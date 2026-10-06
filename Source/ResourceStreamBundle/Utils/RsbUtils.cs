using static ParallelTables;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SexyPackages.ResourceStreamGroup;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB helper methods </summary>

internal static class RsbUtils
{
/// <summary> Group-level parallelism threshold requested for large RSBs </summary>

private const int GROUP_PARALLEL_THRESHOLD = 500;

// ResGroup visitor

internal delegate void GroupVisitor(int index,
                                    in RsbGroupDescriptor desc,
                                    string groupName,
                                    NativeBuffer buffer);

// Group predicate

internal delegate bool GroupPredicate(int index,
                                      in RsbGroupDescriptor desc,
                                      string groupName);

// RSB Work Item

private readonly record struct RsbWorkItem(int Index, string GroupName, NativeBuffer Buffer);

// Load embedded RSG

private static NativeBuffer LoadEmbeddedRSG(Stream source, in RsbGroupDescriptor desc, string groupName)
{
RsgExtractor.TryReadRsg(source, desc, groupName, out var rsgBuffer);

return rsgBuffer;
}

// Load external RSG

private static NativeBuffer LoadExternalRSG(string parentPath, string groupName)
{
return ExternalRsgLoader.LoadGroup(parentPath, groupName);
}

// Loads a RSG buffer (either embedded or external)

internal static NativeBuffer LoadGroupBuffer(Stream source,
                                             string parentPath,
                                             bool useExternalRsgs,
                                             in RsbGroupDescriptor desc,
                                             string groupName)
{
if(useExternalRsgs)
return LoadExternalRSG(parentPath, groupName);

return LoadEmbeddedRSG(source, desc, groupName);

}

// Check whether an RSG contains textures (V1-V2)

private static bool HasTexturesInRsg(Stream source, long rsgOffset)
{
source.Seek(rsgOffset, SeekOrigin.Begin);

using var headBuffer = source.ReadPtr(92);
var header = RsgUnpacker.ReadInfo(headBuffer, out var endian);

if(header.ResMapLength == 0)
return false;

uint metadataSize = header.SectionLength;
uint requiredSize = header.ResMapOffset + header.ResMapLength;

if(metadataSize < requiredSize)
metadataSize = requiredSize;

source.Seek(rsgOffset, SeekOrigin.Begin);

using var metadataBuffer = source.ReadPtr(metadataSize);

uint mapOffset = header.ResMapOffset;
uint mapSize = header.ResMapLength;

var resMap = RsgUnpacker.LoadResMap(metadataBuffer, mapOffset, mapSize, endian);

return resMap.TextureCount > 0;
}

// Check if external rsg has textures

private static bool HasTexturesInExternalRsg(string parentPath, string groupName)
{
string rsgPath = ExternalRsgLoader.ResolvePath(parentPath, groupName);

if(rsgPath is null)
{
TraceLogger.WriteWarn($"Missing external RSG: '{groupName}'");

return false;
}

using var externalRsg = FileManager.OpenRead(rsgPath);

return HasTexturesInRsg(externalRsg, 0);
}

// Check if a RSG has textures (either embedded or external)

internal static bool HasTextures(Stream source,
                                 string parentPath,
                                 bool useExternalRsgs,
                                 in RsbGroupDescriptor desc,
                                 string groupName,
                                 RsbMajorVersion majorVersion)
{

if(majorVersion >= RsbMajorVersion.V3)
return desc.TextureCount > 0; // V3-V4

if(useExternalRsgs)
return HasTexturesInExternalRsg(parentPath, groupName);

uint rsgOffset = desc.Offset;

if(rsgOffset == 0 || desc.Size == 0)
return false;

return HasTexturesInRsg(source, rsgOffset);
}

// Produce RSB items (sequential, mono-thread)

private static void ProduceRsbItems(BlockingCollection<RsbWorkItem> queue,
                                    Stream source,
                                    RsbGroupDescriptor[] groups,
                                    string[] groupIDs,
                                    string parentPath,
                                    bool useExternalRsgs,
                                    GroupPredicate groupPredicate)
{
int groupsCount = groups.Length;

for(int i = 0; i < groupsCount; i++)
{
var desc = groups[i];
string groupName = RsgExtractor.ResolveRsgName(desc, groupIDs[i] );

if(groupPredicate is not null && !groupPredicate(i, in desc, groupName) )
continue;

var buffer = LoadGroupBuffer(source, parentPath, useExternalRsgs, desc, groupName);

if(buffer is null)
continue;

RsbWorkItem item = new(i, groupName, buffer);

queue.Add(item);
}

}

// Consumer: process RSB queue (parallel)

private static void ProcessRsbQueue(BlockingCollection<RsbWorkItem> queue,
                                    RsbGroupDescriptor[] groups,
                                    GroupVisitor visitor)
{

Parallel.ForEach(queue.GetConsumingEnumerable(), MultiThreadOptions, item =>
{
using var buffer = item.Buffer;

visitor(item.Index, in groups[item.Index], item.GroupName, buffer);
}

);

}

// Create Work Queue

private static BlockingCollection<RsbWorkItem> CreateRsbWorkQueue(Stream source,
                                                                  RsbGroupDescriptor[] groups,
                                                                  string[] groupIDs,
                                                                  string parentPath,
                                                                  bool useExternalRsgs,
                                                                  GroupPredicate groupPredicate)
{
int maxDop = Environment.ProcessorCount;
BlockingCollection<RsbWorkItem> queue = new(maxDop * 4);

Task.Run( () =>
{

try
{
ProduceRsbItems(queue, source, groups, groupIDs, parentPath, useExternalRsgs, groupPredicate);
}

finally
{
queue.CompleteAdding();
}

}

);

return queue;
}

// Init extractor params

internal static bool InitExtractorParams(RsbTableSet table,
                                         out RsbGroupDescriptor[] groups,
                                         out string[] groupIDs)
{
groups = table.Groups;
groupIDs = table.GroupIDs;

return groups.Length >= GROUP_PARALLEL_THRESHOLD;
}

// Iterate every declared group, loading its buffer

internal static void ForEachGroup(Stream source,
                                  RsbUnpackerContext ctx,
                                  string parentPath,
                                  bool useExternalRsgs,
                                  GroupVisitor visitor,
                                  bool useParalellism = true,
                                  GroupPredicate groupPredicate = null)
{
bool parallelGroups = InitExtractorParams(ctx.Tables, out var groups, out var groupIDs);
int groupsCount = groups.Length;

// Parallel iteration

if(useParalellism && parallelGroups)
{
var queue = CreateRsbWorkQueue(source, groups, groupIDs, parentPath, useExternalRsgs, groupPredicate);

ProcessRsbQueue(queue, groups, visitor);
}

// Single-thread iteration

else
{

for(int i = 0; i < groupsCount; i++)
{
var desc = groups[i];
string groupName = RsgExtractor.ResolveRsgName(desc, groupIDs[i]);

if(groupPredicate is not null && !groupPredicate(i, in desc, groupName))
continue;

using var buffer = LoadGroupBuffer(source, parentPath, useExternalRsgs, desc, groupName);

if(buffer is null)
continue;

visitor(i, in desc, groupName, buffer);
}

}

}

// Text groups

internal static readonly HashSet<string> TEXT_GROUPS = new(StringComparer.OrdinalIgnoreCase)
{
"LawnStrings",
"Credits_Common",
"GameServicesData",
"Lua_Props"
};

// Text extension

internal static readonly HashSet<string> TXT_EXTS = new(StringComparer.OrdinalIgnoreCase)
{
".TXT" 
};

// Package extensions

internal static readonly HashSet<string> PACKAGE_EXTS = new(StringComparer.OrdinalIgnoreCase) 
{
".RTON",
".JSON"
};

// Ini extensions

internal static readonly HashSet<string> INI_EXTS = new(StringComparer.OrdinalIgnoreCase)
{
".INI"
};

// ManifestGroup extensions

internal static readonly HashSet<string> MANIFEST_EXTS = new(StringComparer.OrdinalIgnoreCase)
{
".RTON",
".NEWTON"
};

// Animation extension

internal static readonly HashSet<string> ANIM_EXTS = new(StringComparer.OrdinalIgnoreCase)
{
".PAM"
};

// Sound extensions

internal static readonly HashSet<string> SOUND_EXTS = new(StringComparer.OrdinalIgnoreCase)
{
".BNK",
".WAV"
};

// Check if group is PACKAGES

internal static bool IsPackages(string groupName)
{
bool isPackages = string.Equals(groupName, "PACKAGES", StringComparison.OrdinalIgnoreCase);
bool isLuaPackages = string.Equals(groupName, "LUA_PACKAGES", StringComparison.OrdinalIgnoreCase);

bool isWorldPackages = groupName.StartsWith("WorldPackages_", StringComparison.OrdinalIgnoreCase);

return isPackages || isLuaPackages || isWorldPackages;
}

// Create group matcher

internal static Func<string, bool> CreateGroupMatcher(string groupNameToFind)
{
return gn => string.Equals(gn, groupNameToFind, StringComparison.OrdinalIgnoreCase);
}

// Match all groups

internal static bool MatchAllGroups(string groupName) => true;

// Build info for single group

internal static RsbGroupInfo BuildGroupInfo(NativeBuffer rsgBuffer,
                                            RsbUnpackerContext ctx, 
                                            in RsbGroupDescriptor desc)
{
var tableSet = ctx.Tables;

RsgExtractor.ParseRsgMetadata(rsgBuffer,
                              ctx.BundleInfo,
                              desc,
                              tableSet.Pools,
                              tableSet.Textures,
                              out var resPaths,
                              out var textures);

string poolName = RsbMetadataJsonBuilder.ResolvePoolName(tableSet.Pools, desc.Index);

return new(desc.CompressionFlags, resPaths, textures, poolName);
}

// Save RSB metadata

internal static void SaveMetadata(string outputDir,
                                  RsbUnpackerContext ctx,
                                  GroupMap groupsMap,
                                  bool useExternalRsgs,
                                  RsbCompressionFlags? compressionFlags,
                                  bool? addSmfExt,
                                  bool? encryptPackages,
                                  RsbPlatform? platform)
{
string metadataDir = Path.Combine(outputDir, "Metadata");
Directory.CreateDirectory(metadataDir);

RsbMetadataSaver.SaveManifestInfo(metadataDir, ctx.Manifest);

var tableSet = ctx.Tables;

RsbMetadataSaver.SavePoolInfo(metadataDir, tableSet.Pools);

RsbMetadataSaver.SaveCompositeInfo(metadataDir, tableSet.CompositeIDs, tableSet.GroupIDs,
                                   tableSet.Composites);

RsbMetadataSaver.SaveGroupsInfo(metadataDir, groupsMap, platform);

var cfg = RsbUnpacker.BuildConfig(ctx.Endian, useExternalRsgs, ctx.BundleInfo, compressionFlags,
                                  addSmfExt, encryptPackages);

RsbMetadataSaver.SaveConfig(outputDir, cfg);
}

// Get PTX platform

internal static RsbPlatform GetPtxPlatform(RsbPlatform? platform)
{

if(platform.HasValue)
return platform.Value;

TraceLogger.WriteWarn("RSB Platform not set. 'Android' will be used as the default.");

return RsbPlatform.Android;
}

}

}