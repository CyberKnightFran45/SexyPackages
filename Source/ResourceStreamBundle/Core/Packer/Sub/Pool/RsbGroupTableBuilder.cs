using System;
using System.Collections.Generic;
using System.Text;
using SexyPackages.ResourceStreamGroup;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB GroupTable Builder </summary>

internal static unsafe class RsbGroupTableBuilder
{
// Write ID

private static void WriteID(byte* dest, int maxLen, string val)
{
int byteCount = Encoding.UTF8.GetByteCount(val);

if(byteCount >= maxLen)
TraceLogger.WriteWarn($"ID '{val}' exceds length limit and will be truncated. Max length: {maxLen} bytes.");

UnsafeStringHelper.WriteFixedString(dest, maxLen, val);
}

// Build PTX descriptor

private static RsbTextureDescriptor BuildTextureDescriptor(RsbTextureInfo texJson,
                                                           RsgTextureInfo rsgTexInfo)
{
uint width = texJson?.Width ?? rsgTexInfo.Width;
uint height = texJson?.Height ?? rsgTexInfo.Height;

uint pitch = texJson?.Pitch ?? 0;
uint format = texJson?.Format ?? 0;

uint alphaSize = texJson?.AlphaSize ?? 0;
uint scale = texJson?.Scale ?? 0;

return new(width, height, pitch, format, alphaSize, scale);
}

// Build PTX slots

private static RsbTextureDescriptor[] BuildTextureSlots(LoadedRsg rsg, PtxGlobalMap jsonTextures)
{
var textureFiles = rsg.ResMap.TextureFiles;

var slots = new RsbTextureDescriptor[textureFiles.Count];

foreach(var kvp in textureFiles)
{
string name = kvp.Key.Replace('\\', '/');
var rsgTexInfo = kvp.Value;

RsbTextureInfo texJson = null;
jsonTextures?.TryGetValue(name, out texJson);

var localIndex = (int)rsgTexInfo.TextureIndex;

if(localIndex >= 0 && localIndex < slots.Length)
slots[localIndex] = BuildTextureDescriptor(texJson, rsgTexInfo);

}

return slots;
}

// Build Global Texture table

private static RsbTextureDescriptor[] BuildTextureTable(List<LoadedRsg> rsgs,
														GroupMap groupsInfo,
                                                        uint[] ptxIndices,
                                                        uint[] texturesPerGroup)
{
List<RsbTextureDescriptor> textures = new();

uint runningIndex = 0;

for(int i = 0; i < rsgs.Count; i++)
{
var rsg = rsgs[i];
var textureFiles = rsg.ResMap.TextureFiles;

ptxIndices[i] = runningIndex;
texturesPerGroup[i] = (uint)textureFiles.Count;

if(textureFiles.Count == 0)
continue;

groupsInfo.TryGetValue(rsg.GroupName, out var groupJson);

var slots = BuildTextureSlots(rsg, groupJson?.Textures);

textures.AddRange(slots);

runningIndex += (uint)textureFiles.Count;
}

return textures.ToArray();
}

// Resolve Group index (case-insensitive)

private static bool TryResolveGroupIndex(Dictionary<string, int> groupLookup,
                                         string subGroupKey,
                                         out int index)
{
return groupLookup.TryGetValue(subGroupKey.ToUpperInvariant(), out index);
}

// Warn on missing rsgs

private static void WarnMissingSubGroup(string groupName, string parent)
{
var msg = $"Composite '{parent}' references subgroup '{groupName}', but no matching group was found.";

TraceLogger.WriteWarn(msg);
}

// Fill children

private static void FillChildren(RsbChildDescriptor* childPtr, 
                                 SubGroupMap subGroups,
                                 Dictionary<string, int> groupLookup,
                                 string compositeId)
{
int i = 0;

foreach(var (subGroupKey, subInfo) in subGroups)
{

if(i >= 64)
break;

bool hasID = TryResolveGroupIndex(groupLookup, subGroupKey, out int groupIndex);

if(!hasID)
{
WarnMissingSubGroup(subGroupKey, compositeId);

continue;
}

uint artRes = subInfo?.ArtResolution ?? 0;

childPtr[i] = new( (uint)groupIndex, artRes);

i++;
}

}

// Build composite descriptor

private static RsbCompositeDescriptor BuildComposite(string compositeId,
                                                     SubGroupMap subGroups,
                                                     Dictionary<string, int> groupLookup)
{
var childCount = (uint)Math.Min(subGroups.Count, 64);

RsbCompositeDescriptor desc = new(childCount);

WriteID(desc.Name, 128, compositeId);

var childPtr = (RsbChildDescriptor*)desc.Childs;

FillChildren(childPtr, subGroups, groupLookup, compositeId);

return desc;
}

// Fill children (V3+)

private static void FillChildrenV3(RsbChildDescriptorV3* childPtr,
                                   SubGroupMap subGroups,
                                   Dictionary<string, int> groupLookup,
                                   string compositeId)
{
int i = 0;

foreach(var (subGroupKey, subInfo) in subGroups)
{

if(i >= 64)
break;

bool hasID = TryResolveGroupIndex(groupLookup, subGroupKey, out int groupIndex);

if(!hasID)
{
WarnMissingSubGroup(subGroupKey, compositeId);

continue;
}

uint artRes = subInfo?.ArtResolution ?? 0;

var loc = subInfo?.Localization;
uint locFlags = string.IsNullOrWhiteSpace(loc) ? 0 : String32.ToInt(loc);

childPtr[i] = new( (uint)groupIndex, artRes, locFlags);

i++;
}

}

// Build composite descriptor (V3)

private static RsbCompositeDescriptorV3 BuildCompositeV3(string compositeId,
                                                         SubGroupMap subGroups,
                                                         Dictionary<string, int> groupLookup)
{
var childCount = (uint)Math.Min(subGroups.Count, 64);

RsbCompositeDescriptorV3 desc = new(childCount);

WriteID(desc.Name, 128, compositeId);

var childPtr = (RsbChildDescriptorV3*)desc.Childs;

FillChildrenV3(childPtr, subGroups, groupLookup, compositeId);

return desc;
}


// Build Composites (V1 or V3)

private static void BuildComposites(CompositeMap compositeMap,
                                    Dictionary<string, int> groupLookup,
                                    bool isV3OrGreater,
									out RsbCompositeData compositeData,
									out string[] compositeIDs)
{
int count = compositeMap.Count;

compositeIDs = new string[count];

var composites = isV3OrGreater ? null : new RsbCompositeDescriptor[count];
var compositesV3 = isV3OrGreater ? new RsbCompositeDescriptorV3[count] : null;

int i = 0;

foreach(var (compositeId, compositeInfo) in compositeMap)
{
compositeIDs[i] = compositeId.ToUpperInvariant();

var subGroups = compositeInfo.SubGroups ?? new();

if(isV3OrGreater)
compositesV3[i] = BuildCompositeV3(compositeId, subGroups, groupLookup);

else  
composites[i] = BuildComposite(compositeId, subGroups, groupLookup);

i++;
}

compositeData = new(composites, compositesV3);
}

// Build Group Lookup

private static Dictionary<string, int> BuildGroupLookup(List<LoadedRsg> rsgs)
{
int groupCount = rsgs.Count;

Dictionary<string, int> lookup = new(groupCount, StringComparer.Ordinal);

for(int i = 0; i < groupCount; i++)
{
string id = rsgs[i].GroupName.ToUpperInvariant();

lookup.Add(id, i);
}

return lookup;
}

// Build group descriptor

private static RsbGroupDescriptor BuildGroupDescriptor(LoadedRsg rsg,
                                                       uint poolIndex,
                                                       bool isV3OrGreater,
                                                       uint textureCount,
                                                       uint ptxBaseIndex)
{
var info = rsg.Info;

RsbGroupDescriptor grp = new();

WriteID(grp.Name, 128, rsg.GroupName);

grp.Index = poolIndex;
grp.CompressionFlags = info.CompressionFlags;
grp.HeaderLength = info.SectionLength;

grp.ResidentDataOffset = info.ResidentDataOffset;
grp.ResidentDataSizeCompressed = info.ResidentDataSizeCompressed;
grp.ResidentDataSize = info.ResidentDataSize;
grp.ResidentPoolSize = info.ResidentDataSize;

grp.GPUDataOffset = info.GPUDataOffset;
grp.GPUDataSizeCompressed = info.GPUDataSizeCompressed;
grp.GPUDataSize = info.GPUDataSize;

if(isV3OrGreater)
{
grp.TextureCount = textureCount;
grp.PtxDescriptorStartIndex = ptxBaseIndex;
}

return grp;
}

// Resolve the pool key for a group

private static string ResolvePoolKey(RsbGroupInfo groupInfo, string groupName)
{

if(!string.IsNullOrEmpty(groupInfo?.PoolName) )
return groupInfo.PoolName;

return "\0AUTO\0" + groupName; // NUL-prefixed: can never collide with a real PoolName
}

// Build the Pool table

private static RsbPoolDescriptor[] BuildPoolTable(List<LoadedRsg> rsgs,
                                                  GroupMap groupsInfo,
												  PoolMap poolsLookup,
                                                  out int[] groupPoolIndex)
{
int groupCount = rsgs.Count;
groupPoolIndex = new int[groupCount];

Dictionary<string, int> poolIndexByKey = new(StringComparer.OrdinalIgnoreCase);
List<RsbPoolDescriptor> pools = new();

for(int i = 0; i < groupCount; i++)
{
var rsg = rsgs[i];
groupsInfo.TryGetValue(rsg.GroupName, out var groupJson);

string poolKey = ResolvePoolKey(groupJson, rsg.GroupName);

if(!poolIndexByKey.TryGetValue(poolKey, out int poolIdx) )
{
poolIdx = pools.Count;
poolIndexByKey[poolKey] = poolIdx;

string displayName = groupJson?.PoolName ?? rsg.GroupName + "_AutoPool";

poolsLookup.TryGetValue(displayName, out var declared);

uint numInstances = declared?.NumInstances ?? 1u;
uint flags = declared?.Flags ?? 0u;

RsbPoolDescriptor pool = new(0, 0, numInstances, flags, 0, 0);
WriteID(pool.Name, 128, displayName);

pools.Add(pool);
}

groupPoolIndex[i] = poolIdx;
}

return pools.ToArray();
}

// Build pool info lookup (by name)

private static PoolMap BuildPoolInfoLookup(RsbPoolInfo[] poolsInfo)
{
PoolMap lookup = new(StringComparer.OrdinalIgnoreCase);

if(poolsInfo != null)
{

foreach(var p in poolsInfo)
{

if(!string.IsNullOrEmpty(p?.PoolName) )
lookup.Add(p.PoolName, p); 

}

}

return lookup;
}

// Accumulate a group's resident/GPU footprint into the pool it belongs to

private static void AccumulatePoolUsage(ref RsbPoolDescriptor pool,
                                        LoadedRsg rsg,
                                        bool isV3OrGreater,
                                        uint textureCount,
                                        uint ptxBaseIndex)
{
var info = rsg.Info;

uint dataLen = info.SectionLength + info.ResidentDataSize;
uint gpuLen = info.GPUDataSize;

pool.ResidentDataMemorySize = Math.Max(pool.ResidentDataMemorySize, dataLen);
pool.GPUDataMemorySize = Math.Max(pool.GPUDataMemorySize, gpuLen);

if(!isV3OrGreater)
{
pool.TextureCount = textureCount;
pool.PtxDescriptorStartIndex = ptxBaseIndex;
}

}

// Add GlobalMap entries

private static void AddGlobalMapEntries(List<(string, uint)> entries,
                                        RsbGroupInfo groupInfo,
                                        uint groupIndex)
{
// Add res files

if(groupInfo?.ResFiles != null)
{

foreach(string path in groupInfo.ResFiles)
{
string normalized = path.Replace('/', '\\');
var resEntry = (normalized.ToUpperInvariant(), groupIndex);

entries.Add(resEntry);
}

}

// Add ptx files

if(groupInfo?.Textures != null)
{

foreach(string texName in groupInfo.Textures.Keys)
{
string normalized = texName.Replace('/', '\\');
var ptxEntry = (normalized.ToUpperInvariant(), groupIndex);

entries.Add(ptxEntry);
}

}

}

// Build all tables

public static RsbTableSet Build(List<LoadedRsg> rsgs,
                                RsbPoolInfo[] poolsInfo,
                                CompositeMap compositeMap,
                                GroupMap groupsInfo,
                                bool isV3OrGreater)
{
int groupCount = rsgs.Count;

string[] groupIDs = new string[groupCount];

var groups = new RsbGroupDescriptor[groupCount];

var groupLookup = BuildGroupLookup(rsgs);

uint[] ptxIndices = new uint[groupCount];
uint[] texturesPerGroup = new uint[groupCount];

var textures = BuildTextureTable(rsgs, groupsInfo, ptxIndices, texturesPerGroup);

var poolsLookup = BuildPoolInfoLookup(poolsInfo);
var pools = BuildPoolTable(rsgs, groupsInfo, poolsLookup, out int[] groupPoolIndex);

List<(string, uint)> globalMapEntries = new();

for(int i = 0; i < groupCount; i++)
{
var rsg = rsgs[i];

groupIDs[i] = rsg.GroupName.ToUpperInvariant();

uint textureCount = texturesPerGroup[i];
uint ptxBaseIndex = ptxIndices[i];

var poolIndex = (uint)groupPoolIndex[i];

groups[i] = BuildGroupDescriptor(rsg, poolIndex, isV3OrGreater, textureCount, ptxBaseIndex);

AccumulatePoolUsage(ref pools[poolIndex], rsg, isV3OrGreater, textureCount, ptxBaseIndex);

groupsInfo.TryGetValue(rsg.GroupName, out var groupJson);

AddGlobalMapEntries(globalMapEntries, groupJson, (uint)i);
}

BuildComposites(compositeMap, groupLookup, isV3OrGreater, out var composites, out var compositeIDs);

RsbTableSet rsbTable = new(globalMapEntries);

rsbTable.SetShellIDs(groupIDs, compositeIDs);
rsbTable.SetMetadata(pools, composites, groups, textures);

return rsbTable;
}

}

}