using static ParallelTables;

using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SexyPackages.ResourceStreamGroup
{
/// <summary> Extracts content from a PopCap RSG Stream. </summary>

public static class RsgUnpacker
{
// Resident processor used for decoding files

internal delegate void ResidentProcessor(RawBuffer part0,
                                         string outputDir,
                                         string resName,
                                         in RsgResidentInfo info);

// Texture processor used for decoding ptx files

internal delegate TextureFileMetadata TextureProcessor(RawBuffer part1,
                                                       string outputDir,
                                                       string resName,
                                                       in RsgTextureInfo info);
										 
// Evaluate rsg flags

private static void EvaluateFlags(uint flags, out Endianness endian)
{

switch(flags)
{
case RsgConstants.MAGIC:
endian = Endianness.LittleEndian;
break;

case RsgConstants.MAGIC_BE:
endian = Endianness.BigEndian;
break;

default:
var expectedFlags = $"0x{RsgConstants.MAGIC:X8} | 0x{RsgConstants.MAGIC_BE:X8} (BigEndian)";

throw new Exception($"Invalid ResGroup identifier: 0x{flags:X8}, expected: {expectedFlags}");
}

}

// Read RSG info

internal static RsgInfo ReadInfo(NativeBuffer buffer, out Endianness endian, bool silentLog = true)
{

if(!silentLog)
TraceLogger.WriteActionStart("Reading header...");

uint flags = buffer.GetUInt32(0);
EvaluateFlags(flags, out endian);

var rawData = buffer.GetView(4, 88);
var info = RsgInfo.Read(rawData, endian);

if(!silentLog)
{
var majVer = info.MajorVersion;
var minVer = info.MinorVersion;

if(!Enum.IsDefined(majVer) || !Enum.IsDefined(minVer) )
TraceLogger.WriteWarn($"Unknown version: v{(uint)majVer}.{(uint)minVer}");

TraceLogger.WriteActionEnd();
}

return info;
}

// Read entry

private static void ReadEntry(NativeBuffer buffer, ref ulong pos, Endianness endian,
                              ResourceMap resMap, string path)
{
uint rawType = buffer.GetUInt32(pos, endian);
pos += 4;

var type = (ResourceType)rawType;

switch(type)
{
case ResourceType.Common:
var residentInfo = RsgResidentInfo.Read(buffer, ref pos, endian);

resMap.AddResident(path, residentInfo);
break;

case ResourceType.Texture:
var textureInfo = RsgTextureInfo.Read(buffer, ref pos, endian);

resMap.AddTexture(path, textureInfo);
break;

default:
throw new Exception($"Unknown resource: {path} @ {pos} - Flags: 0x{rawType:X8}");
}

}

// Log Resources embedded

private static void LogResCount(int fileCount, int textureCount)
{
string resSummary; 

if(fileCount > 0 && textureCount > 0)
{
int total = fileCount + textureCount;

resSummary = $"{total} ({fileCount} files, {textureCount} textures)";
}

else if(fileCount > 0)
resSummary = $"{fileCount} files";

else if(textureCount > 0)
resSummary = $"{textureCount} textures";

else
resSummary = "<none>"; 

TraceLogger.WriteInfo($"Resources embedded: {resSummary}");
}

// Load ResourceMap (Core)

internal static ResourceMap LoadResMap(NativeBuffer buffer, uint offset, uint listSize, Endianness endian,
                                       bool silentLog = true)
{
ResourceMap resMap = new();

if(!silentLog)
TraceLogger.WriteActionStart("Loading ResMap...");

TrieReader.Traverse(buffer, offset, listSize, endian,

(buf, ref entryPos, path) => ReadEntry(buf, ref entryPos, endian, resMap, path)

);

if(!silentLog)
{
TraceLogger.WriteActionEnd();

LogResCount(resMap.FileCount, resMap.TextureCount);
}

return resMap;
}

// Check if Extraction can be made to RAM Disk

private static void RedirectGlobalOutput(ref string outputDir, ResourceMap map, RAMDiskOptions options)
{
int fileCount = map.FileCount + map.TextureCount;

long residentBytes = map.ResidentFiles.Sum(e => e.Value.Size);
long textureBytes = map.TextureFiles.Sum(e => e.Value.Size);

long totalBytes = residentBytes + textureBytes;

RAMDisk.TryRedirect(ref outputDir, fileCount, totalBytes, options);
}

// Log Part Size

private static void LogPartSize(uint rawSize, uint sizeCompressed, bool compressed)
{
string partSize = SizeT.FormatSize(rawSize);
string partSummary;

if(compressed)
{
string partSizeZl = SizeT.FormatSize(sizeCompressed);

partSummary = $"{partSize} | Compressed: {partSizeZl}";
}

else
partSummary = partSize;

TraceLogger.WriteInfo($"Part Size: {partSummary}");
}

// Decompress a ZLib stream into a fixed-size destination

private static void DecompressInto(Stream compressedSource, Span<byte> destination)
{
using ZLibStream decompressor = new(compressedSource, CompressionMode.Decompress);

int totalRead = 0;

while(totalRead < destination.Length)
{
int read = decompressor.Read(destination[totalRead..] );

if(read == 0)
break;

totalRead += read;
}

}

// Split RSG buffer into Part

private static RawBuffer GetPartCore(NativeBuffer source, uint offset, uint sizeCompressed,
                                     uint rawSize, bool compressed)
{
RawBuffer pOwner = new(rawSize);
var destination = pOwner.AsSpan();

if(!compressed)
{
var view = source.GetView(offset, (int)rawSize);
view.CopyTo(destination);

return pOwner;
}

using MemoryStream compressedStream = new();
RawBufferHelper.Dump(source, compressedStream, offset, sizeCompressed);

compressedStream.Seek(0, SeekOrigin.Begin);

DecompressInto(compressedStream, destination);

return pOwner;
}

// Get Part buffer

private static RawBuffer GetPartX(NativeBuffer source,
                                  uint offset,
                                  uint sizeCompressed,
                                  uint rawSize,
                                  bool compressed,
                                  uint partNum,
								  bool silentLog)
{

if(!silentLog)
TraceLogger.WriteActionStart($"Reading Part{partNum}...");

var part = GetPartCore(source, offset, sizeCompressed, rawSize, compressed);

if(!silentLog)
{
TraceLogger.WriteActionEnd();

LogPartSize(rawSize, sizeCompressed, compressed);
}

return part;
}


// Get Part Buffers

private static void GetParts(NativeBuffer source,
                             in RsgInfo info,
                             GroupCompressionFlags compressionFlags,
                             ref int step,
                             bool silentLog,
                             out RawBuffer part0,
                             out RawBuffer part1)
{
part0 = new();
part1 = new();

uint residentSize = info.ResidentDataSize;

if(residentSize > 0)
{
step++;

if(!silentLog)
TraceLogger.WriteStep(step, "Read Part0");

uint residentOffset = info.ResidentDataOffset;
uint residentSizeZl = info.ResidentDataSizeCompressed;

bool useZlib = (compressionFlags & GroupCompressionFlags.Part0) != 0;

part0 = GetPartX(source, residentOffset, residentSizeZl, residentSize, useZlib, 0, silentLog);
}

uint gpuSize = info.GPUDataSize;

if(gpuSize > 0)
{
step++;

if(!silentLog)
TraceLogger.WriteStep(step, "Read Part1");

uint gpuOffset = info.GPUDataOffset;
uint gpuSizeZl = info.GPUDataSizeCompressed;

bool useZlib = (compressionFlags & GroupCompressionFlags.Part1) != 0;

part1 = GetPartX(source, gpuOffset, gpuSizeZl, gpuSize, useZlib, 1, silentLog);
}

}

// Init context

internal static RsgUnpackerContext InitCtx(NativeBuffer source, ref int step, bool silentLog = true)
{

if(!silentLog)
TraceLogger.WriteStep(step, "Read Metadata");

var info = ReadInfo(source, out var endian, silentLog);

var compressionFlags = info.CompressionFlags;

if(!silentLog)
RsgHelper.LogFlagsIfUnknown(compressionFlags);

step++;

if(!silentLog)
TraceLogger.WriteStep(step, "Load Resource Map");

var resMap = LoadResMap(source, info.ResMapOffset, info.ResMapLength, endian, silentLog);

GetParts(source, info, compressionFlags, ref step, silentLog, out var part0, out var part1);

return new(endian, info, resMap, part0, part1);
}

// Build res path

internal static string BuildResPath(string baseDir, string resName, bool createDir = false)
{
var resourcesDir = Path.Combine(baseDir, RsgConstants.SRC_RESOURCES);

if(createDir)
Directory.CreateDirectory(resourcesDir);

return Path.Combine(resourcesDir, resName);
}

// Pre-create res folders

private static void EnsureResourceDirs(IEnumerable<string> resNames, string resourcesDir)
{
HashSet<string> uniqueDirs = new();

foreach(var name in resNames)
{
string dir = Path.GetDirectoryName(Path.Combine(resourcesDir, name) );

if(!string.IsNullOrEmpty(dir) )
uniqueDirs.Add(dir);

}

foreach(var dir in uniqueDirs)
Directory.CreateDirectory(dir);

}

// Extract Res

private static void ExtractRes(string outDir, RawBuffer buffer, string resName, in RsgResidentInfo info)
{
string resPath = BuildResPath(outDir, resName);
uint fileSize = info.Size;

using var resStream = FileManager.OpenWrite(resPath, fileSize);

RawBufferHelper.Dump(buffer, resStream, info.Offset, fileSize);
}

// Extract residents (Core)

private static void ExtractResidentsCore(CommonResMap entries, RawBuffer buffer, string outDir,
                                         ResidentProcessor processor)
{
processor ??= (part0, outputDir, resName, in info) => ExtractRes(outputDir, part0, resName, info);

if(entries.Count <= MAX_FILES_SINGLE_THREAD)
{

foreach(var entry in entries)
processor(buffer, outDir, entry.Key, entry.Value); // Single thread

}

else
{

Parallel.ForEach(entries, MultiThreadOptions, entry =>
{
processor(buffer, outDir, entry.Key, entry.Value);
}

); // Multi-thread

}

}

// Extract Files from Part0

private static void ExtractResidents(CommonResMap entries, RawBuffer buffer, string outDir,
                                     ResidentProcessor processor, bool silentLog)
{

if(!silentLog)
TraceLogger.WriteActionStart("Extracting files...");

ExtractResidentsCore(entries, buffer, outDir, processor);

if(!silentLog)
TraceLogger.WriteActionEnd();

}

// Extract Ptx

private static TextureFileMetadata ExtractPtx(string outDir, RawBuffer buffer, string name,
                                              in RsgTextureInfo info)
{
string resPath = BuildResPath(outDir, name);
uint fileSize = info.Size;

using var resStream = FileManager.OpenWrite(resPath, fileSize);
RawBufferHelper.Dump(buffer, resStream, info.Offset, fileSize);

return new(info.TextureIndex, info.Width, info.Height);
}

// Extract textures (Core)

private static PtxExportInfo ExtractTexturesCore(PtxMap entries, RawBuffer buffer, string outDir,
                                                 TextureProcessor processor)
{
processor ??= (part1, outputDir, resName, in info) => ExtractPtx(outputDir, buffer, resName, info);

PtxExportInfo gpuFilesDict = new();
object lockObj = new();

void processAction(KeyValuePair<string, RsgTextureInfo> entry)
{
var meta = processor(buffer, outDir, entry.Key, entry.Value);

lock(lockObj)
{
string textureName = entry.Key.Replace('\\', '/');

gpuFilesDict.Add(textureName, meta);
}

}

if(entries.Count <= MAX_FILES_SINGLE_THREAD)
{

foreach(var entry in entries)
processAction(entry); // Single thread
 
}

else
Parallel.ForEach(entries, MultiThreadOptions, processAction); // Multi threading

return gpuFilesDict;
}

// Extract Files from Part1

private static PtxExportInfo ExtractTextures(PtxMap entries, RawBuffer buffer, string outDir,
                                             TextureProcessor processor, bool silentLog)
{

if(!silentLog)
TraceLogger.WriteActionStart("Extracting textures...");

var gpuFilesDict = ExtractTexturesCore(entries, buffer, outDir, processor);

if(!silentLog)
TraceLogger.WriteActionEnd();

return gpuFilesDict;
}

// Save Gpu metadata

private static void SaveGpuMetadata(string outDir, PtxExportInfo ptxInfo, bool silentLog)
{

if(!silentLog)
TraceLogger.WriteActionStart("Saving texture info...");

string infoPath = Path.Combine(outDir, "Metadata", RsgConstants.SRC_TEXTURE);

using var infoStream = FileManager.OpenWrite(infoPath);
JsonSerializer.SerializeObject(ptxInfo, infoStream);

if(!silentLog)
TraceLogger.WriteActionEnd();

}

// Extract Core

internal static void ExtractCore(RsgUnpackerContext ctx,
                                 string outputDir,
								 ref int step,
                                 bool silentLog = true,
                                 RAMDiskOptions options = null,
                                 ResidentProcessor residentProcessor = null,
                                 TextureProcessor textureProcessor = null)
{
var resMap = ctx.ResMap;

RedirectGlobalOutput(ref outputDir, resMap, options); // Redirect to RAM Disk if posible

string resourcesDir = Path.Combine(outputDir, RsgConstants.SRC_RESOURCES);

var allNames = resMap.ResidentFiles.Keys.Concat(resMap.TextureFiles.Keys);
EnsureResourceDirs(allNames, resourcesDir);

var part0 = ctx.Part0;

if(part0.Size > 0)
{
step++;

if(!silentLog)
TraceLogger.WriteStep(step, "Extract Resident Files");

ExtractResidents(resMap.ResidentFiles, part0, outputDir, residentProcessor, silentLog);

part0.Dispose();
}

var part1 = ctx.Part1;

if(part1.Size > 0)
{
step++;

if(!silentLog)
TraceLogger.WriteStep(step, "Extract Texture Files");

var textureInfo = ExtractTextures(resMap.TextureFiles, part1, outputDir, textureProcessor, silentLog);

step++;

if(!silentLog)
TraceLogger.WriteStep(step, "Save Texture Info");

SaveGpuMetadata(outputDir, textureInfo, silentLog);

part1.Dispose();
}

resMap.Clear();
}

// Save RsgConfig

private static void SaveConfig(string outputDir, RsgParams cfg, bool silentLog)
{

if(!silentLog)
TraceLogger.WriteActionStart("Saving config...");

string infoPath = Path.Combine(outputDir, RsgConstants.SRC_CONFIG);

using var cfgStream = FileManager.OpenWrite(infoPath);
JsonSerializer.SerializeObject(cfg, cfgStream, RsgParams.Context);

if(!silentLog)
TraceLogger.WriteActionEnd();

}

// Save the generated RsgParams config

internal static void SaveGeneratedConfig(RsgUnpackerContext ctx, string outputDir, ref int step,
                                         bool? encryptRtons = null,
                                         bool silentLog = true)
{
step++;

if(!silentLog)
TraceLogger.WriteStep(step, "Save RSG Config");

RsgParams cfg = new(ctx.Endian, ctx.Info, encryptRtons);

SaveConfig(outputDir, cfg, silentLog);
}

// Decompress a RSG buffer

public static void Decompress(NativeBuffer source, string outputDir, RAMDiskOptions options = null,
                              bool silentLog = true)
{
int step = 1;

var ctx = InitCtx(source, ref step, silentLog);
ExtractCore(ctx, outputDir, ref step, silentLog, options);

SaveGeneratedConfig(ctx, outputDir, ref step, null, silentLog);
}

// Decompress RSG Stream

public static void Decompress(Stream source, string outputDir, RAMDiskOptions options = null,
                              bool silentLog = true)
{
using var rsgBuffer = source.ReadPtr();

Decompress(rsgBuffer, outputDir, options, silentLog);
}

// Unpack internal

private static void UnpackInternal(string inputFile, string outputDir, RAMDiskOptions options)
{
using var rsgStream = FileManager.OpenRead(inputFile);

Decompress(rsgStream, outputDir, options, false);
}

/// <summary> Decompress the Content from a RSG Stream. </summary>

public static void Unpack(string inputFile, string outputDir, RAMDiskOptions options = null)
{

TraceExecutor.Run("RSG Unpacking", 
                  ctx => UnpackInternal(inputFile, outputDir, options),
                  ("InputFile", inputFile),
                  ("OutputDir", outputDir)
);

}

}

}