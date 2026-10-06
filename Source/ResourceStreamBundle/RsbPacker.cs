using System.Collections.Generic;
using System.IO;
using SexyCompressors.PopCapZLib;
using SexyCompressors.RSLB;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Packs a directory back into a PopCap ResBundle. </summary>

public static class RsbPacker
{
// RSB Layer Compressor

private delegate void RsbLayerCompressor(Stream input, Stream output, ProgressCallback progressCallback);

// Layer decompressors

private static readonly Dictionary<RsbCompressionFlags, RsbLayerCompressor> LayerCompressors = new()
{

[RsbCompressionFlags.ZLib] = (i, o, c) => SmfCompressor.CompressStream(i, o, default, c),
[RsbCompressionFlags.Lzma] = RslbCompressor.Compress

};

// Init context

internal static RsbPackerContext InitCtx(string sourceDir, ref int step,
										 ReadonlyBufferMap overrides = null)
{
TraceLogger.WriteStep(step, "Import Metadata");

var cfg = RsbMetadataLoader.LoadParams(sourceDir);

string metadataDir = Path.Combine(sourceDir, "Metadata");

RsbManifest manifest = null;

if(cfg.ManifestAsJson)
manifest = RsbMetadataLoader.LoadManifestInfo(metadataDir);

var poolsInfo = RsbMetadataLoader.LoadPoolsInfo(metadataDir);

var compositeMap = RsbMetadataLoader.LoadCompositeInfo(metadataDir);
var groupsInfo = RsbMetadataLoader.LoadGroupsInfo(sourceDir);

step++;

TraceLogger.WriteStep(step, "Load ResGroups");
var rsgs = RsgFolderLoader.LoadAll(sourceDir, overrides);

step++;

TraceLogger.WriteStep(step, "Build RSB Tables");

bool isV3OrGreater = cfg.MajorVersion >= RsbMajorVersion.V3;

TraceLogger.WriteActionStart("Building Tables...");
var tables = RsbGroupTableBuilder.Build(rsgs, poolsInfo, compositeMap, groupsInfo.Groups, isV3OrGreater);

TraceLogger.WriteActionEnd();

step++;

return new(cfg, rsgs, tables, manifest);
}

// Compress (Core)

public static void Compress(string sourceDir, Stream target, out bool addSmfExt,
							ReadonlyBufferMap overrides = null)
{
int step = 1;
var ctx = InitCtx(sourceDir, ref step, overrides);

addSmfExt = ctx.Params.AddSmfExtension ?? false;

var compression = ctx.Params.CompressionFlags;
bool shouldCompress = compression.HasValue && compression.Value != RsbCompressionFlags.None;

if(shouldCompress && LayerCompressors.TryGetValue(compression.Value, out var compressor) )
{
using ChunkedMemoryStream temp = new();
RsbWriter.Write(temp, ctx, ref step);

temp.Seek(0, SeekOrigin.Begin);

compressor(temp, target, null);

return;
}

RsbWriter.Write(target, ctx, ref step);
}

// Pack internal

private static void PackInternal(string sourceDir, string targetFile, TraceContext ctx)
{
PathHelper.ChangeExtension(ref targetFile, ".rsb");

bool addSmfExt;

using(var rsbStream = FileManager.OpenWrite(targetFile) )
{
Compress(sourceDir, rsbStream, out addSmfExt);

ctx.OutputSize = rsbStream.Length;
}

ctx.LogOutSize = true;

if(addSmfExt)
{
string smfPath = targetFile;
PathHelper.AddExtension(ref smfPath, ".smf");

File.Move(targetFile, smfPath); // Rename file
}

}

/// <summary> Packs a directory back into a RSB file. </summary>

public static void Pack(string sourceDir, string targetFile)
{

TraceExecutor.Run("RSB Packing",
                  ctx => PackInternal(sourceDir, targetFile, ctx),
                  ("SourceDir", sourceDir),
                  ("TargetFile", targetFile)
);

}

}

}