using static ParallelTables;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SexyPackages.ResourceStreamGroup;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Extracts ResGroups from RSBs </summary>

internal static class RsgExtractor
{
// RSG Work Item

private readonly record struct RsgWorkItem(int Index, string GroupName, NativeBuffer Buffer);

// RSG Extraction result

private sealed record RsgExtractionResult(string Name,
                                          GroupCompressionFlags Flags,
                                          List<string> Resources,
                                          PtxGlobalMap Textures,
                                          string PoolName);

// Create Work Queue

private static BlockingCollection<RsgWorkItem> CreateRsgWorkQueue(Stream source,
                                                                  RsbGroupDescriptor[] groupInfo,
																  string[] groupIDs)
{
int maxDop = Environment.ProcessorCount;
BlockingCollection<RsgWorkItem> queue = new(maxDop * 4);

Task.Run( 

() =>
{

try
{
ProduceRsgItems(queue, source, groupInfo, groupIDs);
}

finally
{
queue.CompleteAdding();
}

}

);

return queue;
}

// Produce RSG Items (sequencial, mono-thread)

private static void ProduceRsgItems(BlockingCollection<RsgWorkItem> queue,
                                    Stream source,
									RsbGroupDescriptor[] groupInfo,
									string[] groupIDs)
{
int groupCount = groupInfo.Length;

for(int i = 0; i < groupCount; i++)
{
var desc = groupInfo[i];
string groupName = ResolveRsgName(desc, groupIDs[i] );

if(!TryReadRsg(source, desc, groupName, out var buffer) )
continue;

RsgWorkItem worker = new(i, groupName, buffer);

queue.Add(worker);
}

}

// Resolve RSG Name

public static unsafe string ResolveRsgName(in RsbGroupDescriptor desc, string fallbackID)
{

fixed(byte* namePtr = desc.Name)
{
string name = UnsafeStringHelper.ExtractString(namePtr, 128);

return string.IsNullOrEmpty(name) ? fallbackID : name;
}

}

// Validate Rsg CompressionFlags

private static void ValidateRsgFlags(in RsbGroupDescriptor desc, string groupName)
{
var rsgFlags = desc.CompressionFlags;

if(RsgHelper.CheckFlags(rsgFlags) )
return;

var rawFlags = (uint)rsgFlags;

TraceLogger.WriteWarn($"RSG '{groupName}' has an unknown compression flags: 0x{rawFlags:X8} ({rawFlags})");
}

// Parse and extract RSG

public static bool TryReadRsg(Stream source,
                              in RsbGroupDescriptor desc,
							  string groupName,
                              out NativeBuffer buffer)
{
buffer = new();

uint rsgOffset = desc.Offset;
uint rsgSize = desc.Size;

if(rsgOffset == 0 || rsgSize == 0)
{
TraceLogger.WriteWarn($"RSG '{groupName}' has no embedded data.");

return false;
}

ValidateRsgFlags(desc, groupName);

source.Seek(rsgOffset, SeekOrigin.Begin);

buffer = source.ReadPtr(rsgSize);

return true;
}

// Consumer: process RSG queue (parallel)

private static RsgExtractionResult[] ProcessRsgQueue(BlockingCollection<RsgWorkItem> queue,
                                                     string groupsDir,
													 RsbInfo info,
                                                     RsbGroupDescriptor[] groupInfo,
                                                     RsbPoolDescriptor[] poolInfo,
                                                     RsbTextureDescriptor[] ptxInfo)
{
int count = groupInfo.Length;

var results = new RsgExtractionResult[count];

Parallel.ForEach(queue.GetConsumingEnumerable(), MultiThreadOptions,

item =>
{
int index = item.Index;

results[index] = ProcessSingleRsg(item, groupsDir, info, groupInfo[index], poolInfo, ptxInfo);
}

);

return results;
}

// Process single RSG

private static RsgExtractionResult ProcessSingleRsg(in RsgWorkItem item,
                                                    string groupsDir,
                                                    in RsbInfo info,
                                                    in RsbGroupDescriptor desc,
                                                    RsbPoolDescriptor[] poolInfo,
                                                    RsbTextureDescriptor[] ptxInfo)
{
using var buffer = item.Buffer;

ExtractRsg(groupsDir, buffer.GetView(), item.GroupName);

ParseRsgMetadata(buffer,
                 info,
                 desc,
                 poolInfo,
                 ptxInfo,
                 out var resources,
                 out var textures);

string poolName = RsbMetadataJsonBuilder.ResolvePoolName(poolInfo, desc.Index);

return new(item.GroupName, desc.CompressionFlags, resources, textures, poolName);
}

// Extract rsg file (legacy format)

private static void ExtractRsg(string outputDir, ReadOnlySpan<byte> rawBytes, string rsgName)
{
string filePath = Path.Combine(outputDir, rsgName + ".rsg");
using var outFile = FileManager.OpenWrite(filePath, rawBytes.Length);

outFile.Write(rawBytes);
}

// Parse RSG info

public static void ParseRsgMetadata(NativeBuffer buffer,
                                    in RsbInfo rsbInfo,
                                    in RsbGroupDescriptor groupDesc,
                                    RsbPoolDescriptor[] poolInfo,
                                    RsbTextureDescriptor[] globalPtxInfo,
                                    out List<string> resPaths,
                                    out PtxGlobalMap ptxMap)
{
resPaths = null;
ptxMap = null;

var rsgHeader = RsgUnpacker.ReadInfo(buffer, out var rsgEndian);

var mapOffset = rsgHeader.ResMapOffset;
var mapSize = rsgHeader.ResMapLength;

var resMap = RsgUnpacker.LoadResMap(buffer, mapOffset, mapSize, rsgEndian);

if(resMap.FileCount > 0)
resPaths = GetResPaths(resMap.ResidentFiles);

if(resMap.TextureCount > 0)
{
uint ptxBaseIndex = GetPtxIndex(rsbInfo.MajorVersion, groupDesc, poolInfo);

ptxMap = GetPtxEntries(resMap.TextureFiles, ptxBaseIndex, rsbInfo.PtxDescriptorSize, globalPtxInfo);
}

resMap.Clear();
}

// Get & Normalize res paths

private static List<string> GetResPaths(CommonResMap resFiles)
{
List<string> resPaths = new();

foreach(var kvp in resFiles)
{
var path = kvp.Key.Replace('\\', '/');

resPaths.Add(path);
}

return resPaths;
}

// Get ptx base index

public static uint GetPtxIndex(RsbMajorVersion majVer,
                               in RsbGroupDescriptor groupDesc,
							   RsbPoolDescriptor[] poolInfo)
{

if(majVer <= RsbMajorVersion.V1)
return poolInfo[groupDesc.Index].PtxDescriptorStartIndex;

return groupDesc.PtxDescriptorStartIndex;
}

// Get ptx entries

private static PtxGlobalMap GetPtxEntries(PtxMap ptxFiles,
                                          uint ptxBaseIndex,
										  uint descriptorSize,
                                          RsbTextureDescriptor[] globalPtxInfo)
{
PtxGlobalMap ptxMap = new();

foreach(var kvp in ptxFiles)
{
string textureName = kvp.Key.Replace('\\', '/');
var rsgTexInfo = kvp.Value;

uint globalTexIndex = ptxBaseIndex + rsgTexInfo.TextureIndex;

if(globalTexIndex < globalPtxInfo.Length)
{
var texDesc = globalPtxInfo[globalTexIndex];
RsbTextureInfo ptxInfo = new(texDesc, descriptorSize);

ptxMap.Add(textureName, ptxInfo);
}

}

return ptxMap;
}

// Build RSG map

private static GroupMap BuildGroupMap(RsgExtractionResult[] results)
{
GroupMap map = new();

foreach(var r in results)
{

if(r is null)
continue;

RsbGroupInfo groupInfo = new(r.Flags, r.Resources, r.Textures, r.PoolName);

map.Add(r.Name, groupInfo);
}

return map;
}

// Extract Groups in parallel

private static GroupMap ExtractGroupsCore(Stream source,
                                          in RsbInfo info,
                                          RsbGroupDescriptor[] groupInfo,
                                          string[] groupIDs,
                                          RsbPoolDescriptor[] poolInfo,
										  RsbTextureDescriptor[] ptxInfo,
                                          string outputDir)
{
string groupsDir = Path.Combine(outputDir, RsbConstants.SRC_GROUPS);

using var workQueue = CreateRsgWorkQueue(source, groupInfo, groupIDs);
var results = ProcessRsgQueue(workQueue, groupsDir, info, groupInfo, poolInfo, ptxInfo);

return BuildGroupMap(results);
}

// Extract all RSGs

public static GroupMap Extract(Stream source,
                               in RsbInfo info,
							   RsbGroupDescriptor[] groupInfo,
                               string[] groupIDs,
							   RsbPoolDescriptor[] poolInfo,
                               RsbTextureDescriptor[] ptxInfo,
                               string outputDir)
{
TraceLogger.WriteActionStart("Extracting ResGroups...");
var groupsMap = ExtractGroupsCore(source, info, groupInfo, groupIDs, poolInfo, ptxInfo, outputDir);

TraceLogger.WriteActionEnd();

return groupsMap;
}

}

}