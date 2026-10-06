using System;
using System.Collections.Generic;
using System.IO;
using SexyPackages.ResourceStreamBundle;

namespace SexyPackages.ResourceStreamGroup
{
/// <summary> RSG Legacy Packer: check and encode resources </summary>

public static class RsgLegacyPacker
{
// Attempts to Re-encode a single resource

private static void TryReEncodeResource(string declaredName,
                                        string resourcesDir,
										BufferMap overrides,
                                        SeedContext seedCtx,
										UpdateContext updateCtx = null)
{

if(!ResourceCodec.HasEncoder(declaredName) )
return; // No encoder yet

string declaredPath = Path.Combine(resourcesDir, declaredName);

if(File.Exists(declaredPath) )
return; // Already encoded

string decodedExt = ResourceCodec.GetDecodedExtension(declaredName);

if(string.IsNullOrEmpty(decodedExt) )
return; // Unknown extension

string decodedPath = Path.ChangeExtension(declaredPath, decodedExt);

if(!File.Exists(decodedPath) )
return; // Decoded file is missing

using var inFile = FileManager.OpenRead(decodedPath);

CodecContext context = new();
seedCtx(context);

if(!ResourceCodec.TryEncode(declaredName, inFile, out var encoded, context) )
return; // Failed to encode file

updateCtx?.Invoke(context); // Context carries params updated after Encoding

overrides[declaredName] = encoded;
}

// Set RTON context

private static void SeedRtonContext(string declaredName, bool? encryptRtons, CodecContext context)
{
string fileExt = Path.GetExtension(declaredName);
bool isRton = string.Equals(fileExt, ".RTON", StringComparison.OrdinalIgnoreCase);

if(!isRton || !encryptRtons.HasValue)
return;

context.Set(RtonCodec.CTX_KEY_USE_ENCRYPTION, encryptRtons.Value);
}

// Encode declared residents

private static void EncodeResidents(List<string> resFiles, 
                                    string resourcesDir,
                                    BufferMap overrides,
									bool? encryptRtons,
									bool silentLog)
{

if(resFiles is null)
return;

if(!silentLog)
TraceLogger.WriteActionStart("Encoding files...");

foreach(string name in resFiles)
{
	
TryReEncodeResource(name, resourcesDir, overrides,

context => SeedRtonContext(name, encryptRtons, context)

);

}

if(!silentLog)
TraceLogger.WriteActionEnd();

}

// Update PTX info

private static void UpdatePtxInfo(RsbTextureInfo info, CodecContext context)
{
context.TryGet(PtxCodec.CTX_KEY_WIDTH, out uint width);
context.TryGet(PtxCodec.CTX_KEY_HEIGHT, out uint height);
context.TryGet(PtxCodec.CTX_KEY_PITCH, out uint pitch);
context.TryGet(PtxCodec.CTX_KEY_ALPHA_SIZE, out uint aSize);
context.TryGet(PtxCodec.CTX_KEY_SCALE, out uint scale);

info.Width = width;
info.Height = height;
info.Pitch = pitch;
info.AlphaSize = aSize;
info.Scale = scale;
}

// Re-encodes a PTX file and updates its slot inside GroupInfo

private static void ReEncodeTexture(string name,
                                    RsbTextureInfo info,
									string resourcesDir,
									BufferMap overrides,
								    RsbGroupInfo expected,
                                    RsbPlatform platform)
{

TryReEncodeResource(name, resourcesDir, overrides,

context => RsgUtils.SeedPtxContext(expected, name, platform, context, true),
context => UpdatePtxInfo(info, context)

);

}

// Encode declared textures

private static void EncodeTextures(PtxGlobalMap textures,
                                   string resourcesDir,
                                   BufferMap overrides,
                                   RsbGroupInfo expected,
                                   RsbPlatform platform,
                                   bool silentLog)
{

if(textures is null)
return;

if(!silentLog)
TraceLogger.WriteActionStart("Encoding textures...");

foreach(var kvp in textures)
ReEncodeTexture(kvp.Key, kvp.Value, resourcesDir, overrides, expected, platform);

if(!silentLog)
TraceLogger.WriteActionEnd();

}

// Re-encodes every declared resource

private static BufferMap EncodeResources(string sourceDir,
                                         RsbGroupInfo expected,
                                         RsbPlatform platform,
                                         ref int step,
                                         bool? encryptRtons,
                                         bool silentLog)
{
BufferMap overrides = new(StringComparer.OrdinalIgnoreCase);

if(expected is null)
return overrides;

if(!silentLog)
TraceLogger.WriteStep(step, "Encode Resources");

step++;

string resourcesDir = Path.Combine(sourceDir, RsgConstants.SRC_RESOURCES);

EncodeResidents(expected.ResFiles, resourcesDir, overrides, encryptRtons, silentLog);
EncodeTextures(expected.Textures, resourcesDir, overrides, expected, platform, silentLog);

return overrides;
}

// Smart Pack (Core, Stream based)

internal static void PackCore(string sourceDir,
                              Stream target,
                              RsbGroupInfo expected,
                              RsbPlatform platform,
                              ref int step,
                              string groupName = null,
                              FileProgress progress = null,
							  bool? encryptRtons = null,
                              bool silentLog = true)
{
RsgUtils.ValidateResourcesInDir(expected, sourceDir, groupName, ref step, silentLog);

var cfg = RsgPacker.LoadConfig(sourceDir, silentLog, ref step);
bool? shouldEncryptRtons = encryptRtons ?? cfg?.EncryptRtons;

var overrides = EncodeResources(sourceDir, expected, platform, ref step, shouldEncryptRtons, silentLog);

try
{
RsgPacker.CompressCore(sourceDir, target, cfg, ref step, progress, expected, overrides, silentLog);
}

finally
{

foreach(var buffer in overrides.Values)
buffer.Dispose(); // Release encoded files

}

}

/// <summary> Builds a RSG straight into memory, encoding all its resources </summary>

public static NativeBuffer PackToBuffer(string sourceDir,
                                        RsbGroupInfo expected,
                                        RsbPlatform platform,
                                        string groupName = null,
                                        FileProgress progress = null,
                                        bool? encryptRtons = null,
									    bool silentLog = true)
{
int step = 1;

using MemoryStream buffer = new();

PackCore(sourceDir, buffer, expected, platform, ref step, groupName,
         progress, encryptRtons, silentLog);

buffer.Seek(0, SeekOrigin.Begin);

return buffer.ReadPtr();
}

/// <summary> Builds a RSG to a FileStream, encoding all its resources </summary>

internal static void PackToDir(string sourceDir,
                               string targetFile,
                               string groupInfoPath,
                               FileProgress progress = null,
                               bool? encryptRtons = null,
							   bool silentLog = true)
{
int step = 1;

string groupName = Path.GetFileNameWithoutExtension(targetFile);

RsgUtils.LoadGroupInfo(groupInfoPath, groupName, ref step, out var groupInfo, out var platform);

PathHelper.ChangeExtension(ref targetFile, ".rsg");

using var rsgStream = FileManager.OpenWrite(targetFile);

PackCore(sourceDir, rsgStream, groupInfo, platform, ref step, groupName, 
         progress, encryptRtons, silentLog);

}

// Smart Pack internal

private static void PackInternal(string sourceDir,
                                 string targetFile,
								 string groupInfoPath,
                                 FileProgress progress,
								 TraceContext ctx)
{
PackToDir(sourceDir, targetFile, groupInfoPath, progress, silentLog: false);

ctx.OutputSize = FileManager.GetFileSize(targetFile);
ctx.LogOutSize = true;
}

/// <summary> Packs a resource directory back into a RSG, validating it against a known GroupInfo </summary>

public static void Pack(string sourceDir, string targetFile, string groupInfoPath,
                        FileProgress progress = null)
{

TraceExecutor.Run("RSG Legacy Pack",
                  ctx => PackInternal(sourceDir, targetFile, groupInfoPath, progress, ctx),
                  ("SourceDir", sourceDir),
                  ("TargetFile", targetFile),
                  ("GroupInfo", groupInfoPath)
);

}

}

}