using System;
using System.IO;
using SexyPackages.ResourceStreamBundle;

namespace SexyPackages.ResourceStreamGroup
{
/// <summary> RSG Legacy Unpacker: check and decode resources </summary>

public static class RsgLegacyUnpacker
{
// Write raw file

private static void WriteRawFile(RawBuffer buffer, string outPath, uint offset, uint size)
{
using var outFile = FileManager.OpenWrite(outPath, size);

RawBufferHelper.Dump(buffer, outFile, offset, size);
}

// Get decoded res extension

private static string GetDecodedResExt(string resName)
{
bool hasDecoder = ResourceCodec.HasDecoder(resName);

return hasDecoder ? ResourceCodec.GetDecodedExtension(resName) : null;
}

// Write raw resident (returns default RtonEncryption mode)

private static bool? WriteRawResident(RawBuffer part0, string outPath, in RsgResidentInfo info)
{
WriteRawFile(part0, outPath, info.Offset, info.Size);

return null;
}

// Check if RTON should be encrypted

private static bool? ShouldEncrypt(string resName, CodecContext context)
{
string fileExt = Path.GetExtension(resName);

bool isRton = string.Equals(fileExt, ".RTON", StringComparison.OrdinalIgnoreCase);

if(isRton && context.TryGet<bool>(RtonCodec.CTX_KEY_USE_ENCRYPTION, out var wasEncrypted) )
return wasEncrypted;   

return null;
}

// Build res path

private static string BuildResPath(string baseDir, string resName)
{
return RsgUnpacker.BuildResPath(baseDir, resName, true);
}

// Decode a single resident, falling back to a raw copy when there's no decoder (or it fails)

private static bool? ProcessResident(RawBuffer part0,
                                     string baseDir,
									 string resName,
                                     in RsgResidentInfo info)
{
string outPath = BuildResPath(baseDir, resName);

string decodedExt = GetDecodedResExt(resName);

if(string.IsNullOrEmpty(decodedExt) )
return WriteRawResident(part0, outPath, info);

using NativeBuffer raw = new(info.Size);
raw.CopyFrom(part0, info.Offset, 0, info.Size);

CodecContext context = new();

string decodedPath = Path.ChangeExtension(outPath, decodedExt);

using(var output = FileManager.OpenWrite(decodedPath) )
{

if(ResourceCodec.TryDecode(resName, raw, output, context) )
return ShouldEncrypt(resName, context); // Decode success

}

File.Delete(decodedPath); // Delete file if decoder fails

return WriteRawResident(part0, outPath, info);
}

// Write raw texture

private static TextureFileMetadata WriteRawTexture(RawBuffer part1, string outPath, in RsgTextureInfo info)
{
WriteRawFile(part1, outPath, info.Offset, info.Size);

return new(info.TextureIndex, info.Width, info.Height);
}

// Decode a single texture, falling back to a raw copy when there's no decoder (or it fails)

private static TextureFileMetadata ProcessTexture(RawBuffer part1,
                                                  string baseDir,
												  string resName,
                                                  RsbPlatform platform,
                                                  in RsgTextureInfo info,
												  RsbGroupInfo expected)
{
string outPath = BuildResPath(baseDir, resName);
string decodedExt = GetDecodedResExt(resName);

if(string.IsNullOrEmpty(decodedExt) )
return WriteRawTexture(part1, outPath, info);

using NativeBuffer raw = new(info.Size);
raw.CopyFrom(part1, info.Offset, 0, info.Size);

CodecContext context = new();
RsgUtils.SeedPtxContext(expected, resName, platform, context, false);

string decodedPath = Path.ChangeExtension(outPath, decodedExt);

using(var output = FileManager.OpenWrite(decodedPath) )
{

if(ResourceCodec.TryDecode(resName, raw, output, context) )
return new(info.TextureIndex, info.Width, info.Height); // Decode success

}

File.Delete(decodedPath); // Delete file if decoder fails

return WriteRawTexture(part1, outPath, info);
}

// Smart Unpack (Core logic; returns RTON encryption mode)

private static bool? UnpackCore(RsgUnpackerContext ctx,
                                ref int step,
							    string outputDir,
								RsbGroupInfo expected,
                                string groupName,
                                RsbPlatform platform,
						        bool silentLog,
							    RAMDiskOptions options)
{
RsgUtils.ValidateResourcesInRSG(expected, ctx.ResMap, groupName, ref step, silentLog);

bool? encryptRtons = null;

void resProcessor(RawBuffer part0, string baseDir, string resName, in RsgResidentInfo info)
{
bool? result = ProcessResident(part0, baseDir, resName, info);

if(result.HasValue)
encryptRtons = result;

}

TextureFileMetadata ptxProcessor(RawBuffer part1, string baseDir, string resName,
                                 in RsgTextureInfo info)
{
return ProcessTexture(part1, baseDir, resName, platform, info, expected);
}

RsgUnpacker.ExtractCore(ctx, outputDir, ref step, silentLog, options, resProcessor, ptxProcessor);

RsgUnpacker.SaveGeneratedConfig(ctx, outputDir, ref step, encryptRtons, silentLog);

return encryptRtons;
}

/// <summary> Extracts a RSG buffer and decodes all its resources </summary>

public static bool? Unpack(NativeBuffer source,
                           string outputDir,
                           RsbPlatform platform,
                           RsbGroupInfo expected,
                           string groupName = null,
						   bool silentLog = true,
                           RAMDiskOptions options = null)
{
int step = 1;
var ctx = RsgUnpacker.InitCtx(source, ref step, silentLog);

return UnpackCore(ctx, ref step, outputDir, expected, groupName, platform, silentLog, options);
}

/// <summary> Extracts a RSG stream and decodes all its resources </summary>

public static bool? Unpack(Stream source,
                           string outputDir,
                           RsbPlatform platform,
						   RsbGroupInfo expected,
                           string groupName = null,
						   bool silentLog = true,
                           RAMDiskOptions options = null)
{
using var rsgBuffer = source.ReadPtr();

return Unpack(rsgBuffer, outputDir, platform, expected, groupName, silentLog, options);
}

// Smart Unpack (internal)

private static void UnpackInternal(string inputFile, string outputDir, string groupInfoPath,
                                   RAMDiskOptions options)
{
int step = 1;

string groupName = Path.GetFileNameWithoutExtension(inputFile);

RsgUtils.LoadGroupInfo(groupInfoPath, groupName, ref step, out var groupInfo, out var platform);

using var source = FileManager.OpenRead(inputFile);
using var rsgBuffer = source.ReadPtr();

var ctx = RsgUnpacker.InitCtx(rsgBuffer, ref step, false);

UnpackCore(ctx, ref step, outputDir, groupInfo, groupName, platform, false, options);
}

/// <summary> Decompresses a RSG file, validating its contents against a known GroupInfo </summary>

public static void Unpack(string inputFile, string outputDir, string groupInfoPath,
                          RAMDiskOptions options = null)
{

TraceExecutor.Run("RSG Legacy Unpack",
                  ctx => UnpackInternal(inputFile, outputDir, groupInfoPath, options),
                  ("InputFile", inputFile),
                  ("OutputDir", outputDir),
                  ("GroupInfo", groupInfoPath)
);

}

}

}