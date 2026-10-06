using static ParallelTables;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SexyPackages.ResourceStreamGroup;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RsbContentExtractor: extract images </summary>

public static partial class RsbContentExtractor
{
// Get CodecContext for a PTX file

private static CodecContext GetPtxContext(in RsbTextureDescriptor texDesc, RsbPlatform platform)
{
CodecContext context = new();

context.Set(PtxCodec.CTX_KEY_WIDTH, texDesc.Width);
context.Set(PtxCodec.CTX_KEY_HEIGHT, texDesc.Height);
context.Set(PtxCodec.CTX_KEY_PITCH, texDesc.Pitch);
context.Set(PtxCodec.CTX_KEY_FORMAT, texDesc.Format);
context.Set(PtxCodec.CTX_KEY_ALPHA_SIZE, texDesc.AlphaSize);
context.Set(PtxCodec.CTX_KEY_SCALE, texDesc.Scale);
context.Set(PtxCodec.CTX_KEY_PLATFORM, platform);

return context;
}

// Warn on invalid texture index

private static void WarnOnInvalidPtxIndex(string groupName, string ptxName, uint index, int maxEntries)
{
var indexFlags = $"Index: {index} - Valid range: [0 .. {maxEntries - 1}]";
var msg = $"{groupName}: texture '{ptxName}' is out-of-range | {indexFlags}";

TraceLogger.WriteWarn(msg);
}

// Attempts to extract a texture

private static bool TryExtractTexture(KeyValuePair<string, RsgTextureInfo> entry,
                                      string groupName,
                                      uint ptxBaseIndex,
                                      RsbTextureDescriptor[] globalPtxInfo,
                                      RsbPlatform platform,
                                      RawBuffer part1,
                                      string texturesDir,
                                      Func<string, bool> nameFilter)
{

if(nameFilter is not null && !nameFilter(entry.Key) )
return false;

var rsgTexInfo = entry.Value;

uint globalTexIndex = ptxBaseIndex + rsgTexInfo.TextureIndex;
int maxEntries = globalPtxInfo.Length;

if(globalTexIndex >= maxEntries)
{
WarnOnInvalidPtxIndex(groupName, entry.Key, globalTexIndex, maxEntries);

return false;
}

var texDesc = globalPtxInfo[globalTexIndex];
var context = GetPtxContext(texDesc, platform);

ExtractFile(part1,
            groupName,
            entry.Key,
            rsgTexInfo.Offset,
            rsgTexInfo.Size,
            texturesDir,
            context,
            BuildFlatFilePath);

return true;
}

// Log amount of PTXs extracted (current RSG)

private static void LogExtractedPtxs(string groupName, int ptxs, int extracted, object loggerLock)
{
var ptxSummary = ptxs == extracted ? $"{ptxs}" : $"{extracted}/{ptxs}";

lock(loggerLock)
{
TraceLogger.WriteLine($"• {groupName} - Textures extracted: {ptxSummary}");
TraceLogger.WriteLine();
}

}

// Extract images from RSG (Core logic)

private static int ExtractImagesCore(RawBuffer part1,
                                     PtxMap textures,
                                     string groupName,
                                     uint ptxBaseIndex,
                                     RsbTextureDescriptor[] globalPtxInfo,
                                     RsbPlatform platform,
                                     string texturesDir,
                                     object loggerLock,
                                     Func<string, bool> nameFilter,
                                     bool silentLog)
{
int ptxCount = textures.Count;

if(ptxCount == 0)
return 0;

int extractedCount = 0;

// Single-thread extraction

if(ptxCount <= MAX_FILES_SINGLE_THREAD)
{

foreach(var entry in textures)
{

bool success = TryExtractTexture(entry,
                                 groupName,
                                 ptxBaseIndex,
                                 globalPtxInfo,
                                 platform,
                                 part1,
                                 texturesDir,
                                 nameFilter);

if(success)
extractedCount++;

}

}

// Parallel extraction

else
{

Parallel.ForEach(textures, MultiThreadOptions,

entry =>
{

bool success = TryExtractTexture(entry,
                                 groupName,
                                 ptxBaseIndex,
                                 globalPtxInfo,
                                 platform,
                                 part1,
                                 texturesDir,
                                 nameFilter);

if(success)
Interlocked.Increment(ref extractedCount);

}

);

}

if(!silentLog)
LogExtractedPtxs(groupName, ptxCount, extractedCount, loggerLock);

part1.Dispose();

return extractedCount;
}

// Extract images from a single group

private static int ExtractGroupImages(string groupName,
                                      NativeBuffer rsgBuffer,
                                      uint ptxBaseIndex,
                                      RsbTextureDescriptor[] globalPtxInfo,
                                      RsbPlatform platform,
                                      string texturesDir,
                                      object loggerLock,
                                      Func<string, bool> nameFilter,
                                      bool silentLog)
{
var resMap = LoadResMapFromRSG(rsgBuffer);

int step = 1;
var rsgCtx = RsgUnpacker.InitCtx(rsgBuffer, ref step);

var part0 = rsgCtx.Part0;

if(part0.Size > 0)
part0.Dispose(); // Part0 buffer is not needed

var part1 = rsgCtx.Part1;

if(part1.Size == 0)
return 0;

var ptxFiles = rsgCtx.ResMap.TextureFiles;

return ExtractImagesCore(part1,
                         ptxFiles,
                         groupName,
                         ptxBaseIndex,
                         globalPtxInfo,
                         platform,
                         texturesDir,
                         loggerLock,
                         nameFilter,
                         silentLog);
}

// Extract and decode all .ptx across every group

private static bool ExtractTextures(Stream source,
                                     RsbUnpackerContext ctx,
                                     string outputDir,
                                     string parentPath,
                                     bool useExternalRsgs,
                                     RsbPlatform platform,
                                     Func<string, bool> nameFilter = null,
                                     bool silentLog = false)
{

if(!silentLog)
{
TraceLogger.WriteActionStart("Extracting Textures...");
TraceLogger.WriteLine();
}

string texturesDir = Path.Combine(outputDir, "Textures");
Directory.CreateDirectory(texturesDir);

var tableSet = ctx.Tables;

int matchingGroups = 0;
int totalPtx = 0;

object loggerLock = new();

bool TextureGroupFilter(int index, in RsbGroupDescriptor desc, string groupName)
{

return RsbUtils.HasTextures(source,
                            parentPath,
                            useExternalRsgs,
                            desc,
                            groupName,
                            ctx.BundleInfo.MajorVersion);
}

RsbUtils.ForEachGroup(source,
                      ctx,
                      parentPath,
                      useExternalRsgs,

(i, in desc, groupName, buffer) =>
{
uint baseIndex = RsgExtractor.GetPtxIndex(ctx.BundleInfo.MajorVersion,
                                          desc,
                                          tableSet.Pools);

int ptxCount = ExtractGroupImages(groupName,
                                  buffer,
                                  baseIndex,
                                  tableSet.Textures,
                                  platform,
                                  texturesDir,
                                  loggerLock,
                                  nameFilter,
                                  silentLog);

if(ptxCount > 0)
{
Interlocked.Increment(ref matchingGroups);

Interlocked.Add(ref totalPtx, ptxCount);
}

},

groupPredicate: TextureGroupFilter
);

// Extraction stats

if(!silentLog)
{

if(totalPtx > 0)
TraceLogger.WriteInfo($"Matching groups: {matchingGroups} | Total textures: {totalPtx}");

else
TraceLogger.WriteWarn("No texture resource was found in this RSB.");

TraceLogger.WriteActionEnd();
}

return totalPtx > 0;
}

}

}