using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SexyCompressors.PopCapZLib;
using SexyCompressors.RSLB;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Extracts content from a PopCap ResBundle. </summary>

public static class RsbUnpacker
{
// RSB Layer decompressor

private delegate void RsbLayerDecompressor(Stream input, Stream output, ProgressCallback progressCallback);

// RSB unpacker body

internal delegate void RsbUnpackerBody(Stream target, RsbCompressionFlags? flags);

// Layer decompressors

private static readonly Dictionary<uint, RsbLayerDecompressor> LayerDecompressors = new()
{

[RsbConstants.MAGIC_ZLIB] = SmfCompressor.DecompressStream,
[RsbConstants.MAGIC_LZMA] = RslbCompressor.Decompress

};

// Get RSB compression flags

private static RsbCompressionFlags? GetCompressionFlags(uint magic)
{

return magic switch
{
RsbConstants.MAGIC_ZLIB => RsbCompressionFlags.ZLib,
RsbConstants.MAGIC_LZMA => RsbCompressionFlags.Lzma,
_ => null
};

}

// Check if Extraction can be made to RAM Disk

private static void RedirectGlobalOutput(ref string outputDir,
                                         RsbGroupDescriptor[] groupInfo,
                                         RAMDiskOptions options)
{
const long JSON_MAX_SIZE = SizeT.ONE_MEGABYTE * 4;
const int METADATA_FILES = 5;

int fileCount = groupInfo.Length + METADATA_FILES;

long rsgBytes = groupInfo.Sum(e => e.Size);
long totalBytes = rsgBytes + JSON_MAX_SIZE;

RAMDisk.TryRedirect(ref outputDir, fileCount, totalBytes, options);
}

// Build RSB config

internal static RsbParams BuildConfig(Endianness endian,
                                      bool useExternalRsgs,
                                      in RsbInfo rsbInfo,
                                      RsbCompressionFlags? compressionFlags,
                                      bool? addSmfExt,
                                      bool? encryptPackages)
{
RsbParams cfg = new(endian, useExternalRsgs, rsbInfo);
cfg.SetExtraInfo(compressionFlags, addSmfExt, encryptPackages);

return cfg;
}

// Decompress (Core logic)

private static void DecompressCore(Stream source,
                                   string outputDir,
                                   RsbPlatform platform,
                                   string parentPath,
                                   RsbCompressionFlags? compressionFlags,
                                   bool? addSmfExt,
                                   RAMDiskOptions options)
{
int step = 1;
var ctx = RsbReader.InitCtx(source, ref step);

var rsbInfo = ctx.BundleInfo;
var rsbTables = ctx.Tables;

var poolInfo = rsbTables.Pools;
var compositeIDs = rsbTables.CompositeIDs;

var groupIDs = rsbTables.GroupIDs;
var groupInfo = rsbTables.Groups;

var ptxInfo = rsbTables.Textures;

RedirectGlobalOutput(ref outputDir, groupInfo, options); // Redirect to RAM Disk if posible

step++;

bool useExternalRsgs = source.Length == rsbInfo.SectionLength;
GroupMap groupsMap;

if(useExternalRsgs)
{
TraceLogger.WriteStep(step, "Load External Groups");

groupsMap = ExternalRsgLoader.Load(parentPath, rsbInfo, groupInfo, groupIDs, poolInfo, ptxInfo);
}

else
{
TraceLogger.WriteStep(step, "Extract ResGroups");

groupsMap = RsgExtractor.Extract(source, rsbInfo, groupInfo, groupIDs, poolInfo, ptxInfo, outputDir);
}

step++;

TraceLogger.WriteStep(step, "Export Metadata");

string metadataDir = Path.Combine(outputDir, "Metadata");
Directory.CreateDirectory(metadataDir);

RsbMetadataSaver.SaveManifestInfo(metadataDir, ctx.Manifest);
RsbMetadataSaver.SavePoolInfo(metadataDir, poolInfo);

RsbMetadataSaver.SaveCompositeInfo(metadataDir, compositeIDs, groupIDs, rsbTables.Composites);
RsbMetadataSaver.SaveGroupsInfo(metadataDir, groupsMap, platform);

step++;

TraceLogger.WriteStep(step, "Save RSB Config");

var cfg = BuildConfig(ctx.Endian, useExternalRsgs, rsbInfo, compressionFlags, addSmfExt, null);

RsbMetadataSaver.SaveConfig(outputDir, cfg);
}

// Unwrap SMF/LZMA outer layer (if present) and run body logic

internal static void UnwrapAndRun(Stream source, RsbUnpackerBody body)
{
uint flags = source.ReadUInt32();
var compressionFlags = GetCompressionFlags(flags);

source.Seek(0, SeekOrigin.Begin); // Peek

if(LayerDecompressors.TryGetValue(flags, out var decompressor) )
{
using ChunkedMemoryStream temp = new();
decompressor(source, temp, null);

temp.Seek(0, SeekOrigin.Begin);

body(temp, compressionFlags);

return;
}

body(source, compressionFlags);
}

// Decompress RSB Stream

public static void Decompress(Stream source,
                              string outputDir,
                              RsbPlatform platform,
                              string parentPath = null,
                              bool? addSmfExt = null,
                              RAMDiskOptions options = null)
{

void body(Stream stream, RsbCompressionFlags? compressionFlags)
{
DecompressCore(stream, outputDir, platform, parentPath, compressionFlags, addSmfExt, options);
}

UnwrapAndRun(source, body);
}

// Check if file is smf

internal static bool? IsSmf(string path)
{
string fileExt = Path.GetExtension(path);

if(string.Equals(fileExt, ".smf", StringComparison.OrdinalIgnoreCase) )
return true;

return null;
}

// Unpack internal

private static void UnpackInternal(string inputFile, 
                                   string outputDir,
                                   RsbPlatform platform,
                                   RAMDiskOptions options)
{
var hasSmfExt = IsSmf(inputFile);
string parentPath = Path.GetDirectoryName(inputFile);

using var rsbStream = FileManager.OpenRead(inputFile);

Decompress(rsbStream, outputDir, platform, parentPath, hasSmfExt, options);
}

/// <summary> Decompress the Content from a RSB Stream. </summary>

public static void Unpack(string inputFile, 
                          string outputDir,
                          RsbPlatform platform,
                          RAMDiskOptions options = null)
{

TraceExecutor.Run("RSB Unpacking",
                  ctx => UnpackInternal(inputFile, outputDir, platform, options),
                  ("InputFile", inputFile),
                  ("OutputDir", outputDir),
                  ("Platform", platform)

);

}

}

}