using static ParallelTables;

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SexyPackages.ResourceStreamGroup;
using System;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Smart Content Extractor: core logic </summary>

public static partial class RsbContentExtractor
{
// Load ResMap from RSG

private static ResourceMap LoadResMapFromRSG(NativeBuffer buffer)
{
var header = RsgUnpacker.ReadInfo(buffer, out var rsgEndian);

var mapOffset = header.ResMapOffset;
var mapSize = header.ResMapLength;

return RsgUnpacker.LoadResMap(buffer, mapOffset, mapSize, rsgEndian);
}

// Try decode file before extraction

private static bool TryDecodeFile(RawBuffer part,
                                  string resName,
                                  uint offset,
                                  uint size,
                                  string outPath,
                                  CodecContext context)
{
using NativeBuffer raw = new(size);

raw.CopyFrom(part, offset, 0, size);

string extension = ResourceCodec.GetDecodedExtension(resName);
string decodedPath = Path.ChangeExtension(outPath, extension);

using var decodedFile = FileManager.OpenWrite(decodedPath);

return ResourceCodec.TryDecode(resName, raw, decodedFile, context);
}

// Extract raw file

private static void ExtractRawFile(RawBuffer part, uint offset, uint size, string outPath)
{
using var outFile = FileManager.OpenWrite(outPath, size);

RawBufferHelper.Dump(part, outFile, offset, size);
}

// Res name resolver

internal delegate string ResNameResolver(string outputDir, string groupName, string resName);

// Build file name

private static string BuildFilePath(string outputDir, string groupName, string resName)
{
string baseDir = Path.Combine(outputDir, groupName);
Directory.CreateDirectory(baseDir);

return Path.Combine(baseDir, resName);
}

// Flat file resolver (ignores group names)

private static string BuildFlatFilePath(string outputDir, string groupName, string resName)
{
string fileName = Path.GetFileName(resName);

return Path.Combine(outputDir, fileName);
}

// Get Name resolver for group

private static ResNameResolver GetNameResolver(int groupFiles)
{
return groupFiles > 1 ? BuildFilePath : BuildFlatFilePath;
}

// Extract single file

private static void ExtractFile(RawBuffer part,
                                string groupName,
                                string resName,
                                uint offset,
                                uint size,
                                string outputDir,
                                CodecContext context,
                                ResNameResolver pathResolver)
{
string outPath = pathResolver(outputDir, groupName, resName);

if(ResourceCodec.HasDecoder(resName) )
{

if(TryDecodeFile(part, resName, offset, size, outPath, context) )
return; // Success

}

ExtractRawFile(part, offset, size, outPath);
}

// Check if group contains resources with matching extensions

private static bool HasMatchingExtension(IEnumerable<string> resNames, HashSet<string> extensions)
{

if(extensions is null)
return true;

foreach(var name in resNames)
{
string ext = Path.GetExtension(name);

if(extensions.Contains(ext) )
return true;

}

return false;
}

// Check if a resource match given ExtensionSet and NameFilter

private static bool ResourceMatch(string resFile,
                                  HashSet<string> extensions,
                                  Func<string, bool> nameFilter)
{
bool nameMatches = nameFilter is null || nameFilter(resFile);

string ext = Path.GetExtension(resFile);
bool extMatches = extensions is null || extensions.Contains(ext);

return nameMatches && extMatches;
}

// Get matching resources

private static CommonResList GetMatchingResources(CommonResEntries resources,
                                                  HashSet<string> extensions,
                                                  Func<string, bool> nameFilter)
{
CommonResList matching = new();

foreach(var entry in resources)
{

if(!ResourceMatch(entry.Key, extensions, nameFilter) )
continue;

matching.Add(entry);
}

return matching;
}

// Extract matching resources

private static CodecSharedInfo ExtractMatchingResources(RawBuffer part0,
                                                        string groupName,
                                                        CommonResList matching,
                                                        string outputDir,
                                                        ResNameResolver pathResolver)
{
CodecSharedInfo localCodecInfo = new();

// Extract resources: single thread

if(matching.Count <= MAX_FILES_SINGLE_THREAD)
{

foreach(var entry in matching)
{
CodecContext context = new();

ExtractFile(part0, 
            groupName,
            entry.Key,
            entry.Value.Offset,
            entry.Value.Size,
            outputDir,
            context,
            pathResolver);

localCodecInfo[entry.Key] = context;
}

}

// Extract resources: multi-thread

else
{

Parallel.ForEach(matching, MultiThreadOptions,

entry =>
{
CodecContext context = new();

ExtractFile(part0, 
            groupName,
            entry.Key,
            entry.Value.Offset,
            entry.Value.Size,
            outputDir,
            context,
            pathResolver);

localCodecInfo[entry.Key] = context;
}

);

}

return localCodecInfo;
}

// Log amount of Reosurces extracted (current RSG)

private static void LogExtractedFiles(string groupName, int files, int extracted, object loggerLock)
{
var fileSummary = files == extracted ? $"{files}" : $"{extracted}/{files}";

lock(loggerLock)
{
TraceLogger.WriteLine($"• {groupName} - Resources extracted: {fileSummary}");
TraceLogger.WriteLine();
}

}

// Processes a single group and extracts matching resources

private static int ProcessGroupResidents(NativeBuffer rsgBuffer,
                                         string groupName,
                                         string outputDir,
                                         HashSet<string> extensions,
                                         CodecInfo codecInfo,
                                         object resultLock,
                                         object loggerLock,
                                         Func<string, bool> nameFilter,
										 ResNameResolver pathResolver,
                                         bool silentLog)
{
var resMap = LoadResMapFromRSG(rsgBuffer);

if(!HasMatchingExtension(resMap.ResidentFiles.Keys, extensions) )
return 0;

int step = 1;
var rsgCtx = RsgUnpacker.InitCtx(rsgBuffer, ref step);

var part1 = rsgCtx.Part1;

if(part1.Size > 0)
part1.Dispose(); // Part1 buffer is not needed

var part0 = rsgCtx.Part0;

if(part0.Size == 0)
return 0;

try
{
var matching = GetMatchingResources(rsgCtx.ResMap.ResidentFiles, extensions, nameFilter);
int filesCount = matching.Count;

if(filesCount == 0)
return 0;

pathResolver ??= GetNameResolver(matching.Count);

var localCodecInfo = ExtractMatchingResources(part0, groupName, matching, outputDir, pathResolver);
int extractedCount = localCodecInfo.Count;

// Update codec info

lock(resultLock)
{

foreach(var kvp in localCodecInfo)
codecInfo.Add(kvp.Key, kvp.Value);

}

if(!silentLog)
LogExtractedFiles(groupName, filesCount, extractedCount, loggerLock);

return extractedCount;
}

finally
{
part0.Dispose();
}

}

// Attempts to extract resources matching a given GroupPredicate, ExtensionSet and NameFilter?

// This overload reports Codec properties

internal static bool ExtractGroupResidents(Stream source,
                                           RsbUnpackerContext ctx,
                                           string outputDir,
                                           string parentPath,
                                           bool useExternalRsgs,
                                           Func<string, bool> groupMatch,
                                           HashSet<string> extensions,
                                           out CodecInfo codecInfo,
                                           out int matchingGroups,
                                           out int totalFiles,
                                           Func<string, bool> nameFilter = null,
										   ResNameResolver pathResolver = null,
                                           bool silentLog = false)
{
object resultLock = new();
object loggerLock = new();

CodecInfo localCodecInfo = new();

int localMatchingGroups = 0;
int localTotalFiles = 0;

bool groupFilter(int index, in RsbGroupDescriptor desc, string groupName) => groupMatch(groupName);

RsbUtils.ForEachGroup(source, ctx, parentPath, useExternalRsgs,

(i, in desc, groupName, buffer) =>
{

int extractedCount = ProcessGroupResidents(buffer,
                                           groupName,
                                           outputDir,
                                           extensions,
                                           localCodecInfo,
                                           resultLock,
                                           loggerLock,
                                           nameFilter,
										   pathResolver,
                                           silentLog);

if(extractedCount > 0)
{
Interlocked.Increment(ref localMatchingGroups);

Interlocked.Add(ref localTotalFiles, extractedCount);
}

},

useParalellism: true,
groupPredicate: groupFilter);

codecInfo = localCodecInfo;

matchingGroups = localMatchingGroups;
totalFiles = localTotalFiles;

return matchingGroups > 0;
}

// Attempts to extract resources matching a given GroupPredicate, ExtensionSet and NameFilter?

internal static bool ExtractGroupResidents(Stream source,
                                           RsbUnpackerContext ctx,
                                           string outputDir,
                                           string parentPath,
                                           bool useExternalRsgs,
                                           Func<string, bool> groupMatch,
                                           HashSet<string> extensions,
                                           out int matchingGroups,
                                           out int totalFiles,
                                           Func<string, bool> nameFilter = null,
										   ResNameResolver pathResolver = null,
                                           bool silentLog = false)
{

return ExtractGroupResidents(source, 
                             ctx,
							 outputDir,
							 parentPath,
							 useExternalRsgs,
                             groupMatch,
							 extensions,
							 out _,
                             out matchingGroups,
							 out totalFiles,
                             nameFilter,
							 pathResolver,
                             silentLog);
							 
}

// Attempts to extract resources matching an exact GroupName, ExtensionSet and NameFilter?

// This overload reports Codec properties

internal static bool ExtractGroupResidents(Stream source,
                                           RsbUnpackerContext ctx,
                                           string outputDir,
                                           string parentPath,
                                           bool useExternalRsgs,
                                           string groupNameToFind,
                                           HashSet<string> extensions,
                                           out CodecInfo codecInfo,
                                           out int matchingGroups,
                                           out int totalFiles,
                                           Func<string, bool> nameFilter = null,
                                           ResNameResolver pathResolver = null,
                                           bool silentLog = false)
{
var groupMatch = RsbUtils.CreateGroupMatcher(groupNameToFind);

return ExtractGroupResidents(source,
                             ctx,
							 outputDir,
							 parentPath,
							 useExternalRsgs,
                             groupMatch,
							 extensions,
							 out codecInfo,
                             out matchingGroups,
							 out totalFiles,
                             nameFilter,
                             pathResolver,
                             silentLog);

}

// Attempts to extract resources matching an exact GroupName, ExtensionSet and NameFilter?

internal static bool ExtractGroupResidents(Stream source,
                                           RsbUnpackerContext ctx,
                                           string outputDir,
                                           string parentPath,
                                           bool useExternalRsgs,
                                           string groupNameToFind,
                                           HashSet<string> extensions,
                                           out int matchingGroups,
                                           out int totalFiles,
                                           Func<string, bool> nameFilter = null,
										   ResNameResolver pathResolver = null,
                                           bool silentLog = false)
{
var groupMatch = RsbUtils.CreateGroupMatcher(groupNameToFind);

return ExtractGroupResidents(source,
                             ctx,
							 outputDir,
							 parentPath,
							 useExternalRsgs,
                             groupMatch,
							 extensions,
							 out _,
                             out matchingGroups,
							 out totalFiles,
                             nameFilter,
							 pathResolver,
                             silentLog);
							 				 
}

}

}