using static ParallelTables;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using BlossomLib.Modules.Compression;
using SexyPackages.ResourceStreamBundle;

namespace SexyPackages.ResourceStreamGroup
{
/// <summary> Packs the Content of a Directory as a PopCap ResGroup (RSG). </summary>

public static class RsgPacker
{
// RSG file process result

private readonly record struct FileProcessResult(string Name,
                                                 NativeBuffer Data,
												 int Size,
												 TextureFileMetadata PtxInfo,
												 bool OwnsData);

// Resource processor

private delegate FileProcessResult FileProcessor(string filePath, string resourceName);

// Load config

internal static RsgParams LoadConfig(string baseDir, bool silentLog, ref int step)
{

if(!silentLog)
TraceLogger.WriteStep(step, "Load RSG Config");

step++;

if(!silentLog)
TraceLogger.WriteActionStart("Loading config...");

string infoPath = Path.Combine(baseDir, RsgConstants.SRC_CONFIG);

using var cfgStream = FileManager.OpenRead(infoPath);
var cfg = JsonSerializer.DeserializeObject<RsgParams>(cfgStream, RsgParams.Context);

if(!silentLog)
TraceLogger.WriteActionEnd();

return cfg;
}

// Get files (disk scan)

private static void GetFilesFromDisk(string baseDir,
                                     bool silentLog,
                                     out List<string> res,
									 out List<string> textures,
                                     out List<string> resNames,
									 out List<string> ptxNames)
{
res = new();
textures = new();

resNames = new();
ptxNames = new();

if(!silentLog)
TraceLogger.WriteActionStart("Obtaining files...");

string resDir = Path.Combine(baseDir, RsgConstants.SRC_RESOURCES);
var files = Directory.EnumerateFiles(resDir, "*.*", SearchOption.AllDirectories);

foreach(string path in files)
{
string relative = Path.GetRelativePath(resDir, path);
string normalized = relative.Replace('/', '\\');

string resName = normalized.ToUpperInvariant();

bool isPtx = resName.EndsWith(".PTX");

if(isPtx)
{
textures.Add(path);
ptxNames.Add(resName);
}

else
{
res.Add(path);
resNames.Add(resName);
}

}

if(!silentLog)
TraceLogger.WriteActionEnd();

}

// Collect declared files

private static void CollectDeclaredFiles(IEnumerable<string> declaredNames,
                                         string resDir,
										 ReadonlyBufferMap overrides,
                                         List<string> paths,
                                         List<string> collectedNames)
{

if(declaredNames is null)
return;

foreach(string name in declaredNames)
{
string resName = name.ToUpperInvariant();
bool hasOverride = overrides != null && overrides.ContainsKey(resName);

string path = Path.Combine(resDir, name);

if(!hasOverride && !File.Exists(path) )
continue; // Missing resource

paths.Add(hasOverride ? null : path); // Path is ignored when an override exists
collectedNames.Add(resName);
}

}

// Get declared files

private static void GetDeclaredFiles(string baseDir,
                                     RsbGroupInfo expected,
									 ReadonlyBufferMap overrides,
                                     bool silentLog,
                                     out List<string> res,
                                     out List<string> textures,
                                     out List<string> resNames,
                                     out List<string> ptxNames)
{
res = new();
textures = new();

resNames = new();
ptxNames = new();

if(!silentLog)
TraceLogger.WriteActionStart("Obtaining files...");

string resDir = Path.Combine(baseDir, RsgConstants.SRC_RESOURCES);

CollectDeclaredFiles(expected.ResFiles, resDir, overrides, res, resNames);
CollectDeclaredFiles(expected.Textures?.Keys, resDir, overrides, textures, ptxNames);

if(!silentLog)
TraceLogger.WriteActionEnd();

}

// Log amount of resources found in dir

private static void LogResFound(int fileCount, int textureCount)
{
string resSummary; 

if(fileCount > 0 && textureCount > 0)
resSummary = $"{fileCount} files | {textureCount} textures";

else if(fileCount > 0)
resSummary = $"{fileCount} files";

else if(textureCount > 0)
resSummary = $"{textureCount} textures";

else
resSummary = "<nothing>"; 

TraceLogger.WriteInfo($"Found: {resSummary}");
}

// Load Global GPU Metadata

private static PtxExportInfo LoadGpuMetadata(string baseDir, bool silentLog)
{

if(!silentLog)
TraceLogger.WriteActionStart("Loading texture info...");

string fileName = Path.Combine("Metadata", RsgConstants.SRC_TEXTURE);
string gpuInfoPath = Path.Combine(baseDir, fileName);

if(!File.Exists(gpuInfoPath) )
throw new FileNotFoundException($"Missing file: '{fileName}'");

using var stream = FileManager.OpenRead(gpuInfoPath);
var metadata = JsonSerializer.DeserializeObject<PtxExportInfo>(stream);

if(!silentLog)
TraceLogger.WriteActionEnd();

return metadata;
}

// Process files in parallel

private static List<FileProcessResult> ProcessFilesInParallel(List<string> files,
                                                              List<string> names,
                                                              FileProcessor processor,
                                                              FileProgress progress)
{
int count = files.Count;

var results = new FileProcessResult[count];

Parallel.For(0, count, MultiThreadOptions,
	
i =>
{
string file = files[i];
string name = names[i];

results[i] = processor(file, name);

progress?.Invoke(name, i + 1, count);
}
	
);

return results.ToList();
}

// Process resident file

private static FileProcessResult ProcessResidentFile(string filePath, string resourceName,
													 ReadonlyBufferMap overrides)
{

if(overrides != null && overrides.TryGetValue(resourceName, out var overrideBuffer) )
return new(resourceName, overrideBuffer, (int)overrideBuffer.Size, null, false);

using var stream = FileManager.OpenRead(filePath);
var buffer = stream.ReadPtr();

return new(resourceName, buffer, (int)stream.Length, null, true);
}

// Add files

private static void AddFiles(List<string> files,
                             List<string> names,
							 MemoryStream part0,
                             CommonResMap entries,
							 FileProgress progress,
							 ReadonlyBufferMap overrides,
							 bool silentLog)
{

if(!silentLog)
TraceLogger.WriteActionStart("Adding files...");

FileProcessResult addRes(string filePath, string name) => ProcessResidentFile(filePath, name, overrides);

var results = ProcessFilesInParallel(files, names, addRes, progress);

foreach(var r in results)
{
var data = r.Data;

long offset = part0.Position;
part0.Write(data.GetView() );

int fileSize = r.Size;

int padding = RsgHelper.ComputePadding(fileSize, false);
part0.Fill(padding);

RsgResidentInfo info = new( (uint)offset, (uint)fileSize);
entries.Add(r.Name, info);

if(r.OwnsData)
data.Dispose();

}

if(!silentLog)
TraceLogger.WriteActionEnd();

}

// Process texture

private static FileProcessResult ProcessTextureFile(string filePath,
                                                    string resourceName,
                                                    PtxExportInfo infos,
													ReadonlyBufferMap overrides)
{

if(!infos.TryGetValue(resourceName, out var metadata) )
{
TraceLogger.WriteWarn($"Missing config for '{resourceName}', this resource will be Omitted.");

return new(resourceName, null, 0, null, true);
}

if(overrides != null && overrides.TryGetValue(resourceName, out var overrideBuffer) )
return new(resourceName, overrideBuffer, (int)overrideBuffer.Size, metadata, false);

using var stream = FileManager.OpenRead(filePath);
var buffer = stream.ReadPtr();

return new(resourceName, buffer, (int)stream.Length, metadata, true);
}

// Add textures

private static void AddTextures(List<string> files,
                                List<string> names,
                                PtxExportInfo infos,
                                MemoryStream part1,
                                PtxMap entries,
								FileProgress progress,
								ReadonlyBufferMap overrides,
								bool silentLog)
{

if(!silentLog)
TraceLogger.WriteActionStart("Adding textures...");

FileProcessResult addPtx(string filePath, string name)
{
return ProcessTextureFile(filePath, name, infos, overrides);
};

var results = ProcessFilesInParallel(files, names, addPtx, progress);

foreach(var r in results)
{
var data = r.Data;

if(data is null)
continue;

long offset = part1.Position;
part1.Write(data.GetView() );

int fileSize = r.Size;

int padding = RsgHelper.ComputePadding(fileSize, false);
part1.Fill(padding);

var meta = r.PtxInfo;

uint index = meta.TextureIndex;
uint width = meta.Width;
uint height = meta.Height;

RsgTextureInfo textureInfo = new( (uint)offset, (uint)fileSize, index, width, height);

entries.Add(r.Name, textureInfo);

if(r.OwnsData)
data.Dispose();

}

if(!silentLog)
TraceLogger.WriteActionEnd();

}

// Compress Part

private static void CompressPart(MemoryStream part, GroupCompressionFlags flags,
                                 out int rawSize, out int sizeCompressed)
{
rawSize = (int)part.Length;
sizeCompressed = rawSize;

if(!RsgHelper.CheckFlags(flags) )
{
RsgHelper.WarnUnknownFlags(flags);

return;
}

if(flags == GroupCompressionFlags.None)
return;

var level = flags == GroupCompressionFlags.All ? CompressionLevel.SmallestSize : CompressionLevel.Optimal;

using MemoryStream zlibStream = new();

part.Seek(0, SeekOrigin.Begin);
ZLibCompressor.CompressStream(part, zlibStream, level);

part.SetLength(0); // Reset buffer
part.Seek(0, SeekOrigin.Begin);

zlibStream.Seek(0, SeekOrigin.Begin);
FileManager.Process(zlibStream, part);

sizeCompressed = (int)part.Length;

int padding = RsgHelper.ComputePadding(sizeCompressed, true);

part.Fill(padding);
}

// Build res payload

private static NativeBuffer BuildResPayload(in RsgResidentInfo info, Endianness endian)
{
NativeBuffer payload = new(12);

payload.SetUInt32(0, 0, endian);
info.Write(payload.AsSpan(4, 8), endian);

return payload;
}

// Build ptx payload

private static NativeBuffer BuildPtxPayload(in RsgTextureInfo info, Endianness endian)
{
NativeBuffer payload = new(32);

payload.SetUInt32(0, 1, endian);
info.Write(payload.AsSpan(4, 28), endian);

return payload;
}

// Build ResMap

private static void BuildResMap(MemoryStream writer, ResourceMap entries, Endianness endian,
                                bool silentLog)
{
	
if(!silentLog)
TraceLogger.WriteActionStart("Building ResMap...");

var residents = entries.ResidentFiles.OrderBy(r => r.Key, StringComparer.Ordinal).ToList();
var textures = entries.TextureFiles.OrderBy(t => t.Key, StringComparer.Ordinal).ToList();

var resPayloads = new (string key, NativeBuffer payload)[residents.Count];
var ptxPayloads = new (string key, NativeBuffer payload)[textures.Count];

Parallel.For(0, residents.Count, MultiThreadOptions, i =>
{
var (key, info) = residents[i];

resPayloads[i] = (key, BuildResPayload(info, endian) );
}

);

Parallel.For(0, textures.Count, MultiThreadOptions, i =>
{
var (key, info) = textures[i];

ptxPayloads[i] = (key, BuildPtxPayload(info, endian) );
}

);

TrieWriter trieWriter = new();

foreach(var (key, payload) in resPayloads)
trieWriter.Add(key, payload);

foreach(var (key, payload) in ptxPayloads)
trieWriter.Add(key, payload);

using var trieBuffer = trieWriter.WriteToBuffer(endian);
writer.Write(trieBuffer.GetView() );

if(!silentLog)
TraceLogger.WriteActionEnd();

}

// Init RSG context

private static RsgPackerContext InitCtx(string sourceDir,
                                        RsgParams cfg,
                                        FileProgress progress,
                                        ref int step,
                                        bool silentLog,
                                        RsbGroupInfo expected, 
                                        ReadonlyBufferMap overrides)
{

if(!silentLog)
TraceLogger.WriteStep(step, "List Files");

List<string> files, textures, resNames, ptxNames;

if(expected is not null)
{

GetDeclaredFiles(sourceDir, expected, overrides, silentLog, out files, out textures, 
                 out resNames, out ptxNames);

}

else
GetFilesFromDisk(sourceDir, silentLog, out files, out textures, out resNames, out ptxNames);

int fileCount = files.Count;
int textureCount = textures.Count;

if(fileCount == 0 && textureCount == 0)
{
string dirName = PathHelper.ShortPath(sourceDir);
TraceLogger.WriteWarn($"'{dirName}' has no files.");

return null;
}

if(!silentLog)
LogResFound(fileCount, textureCount);

PtxExportInfo gpuInfo = null;
bool hasTextures = textureCount > 0;

if(hasTextures)
{
step++;

if(!silentLog)
TraceLogger.WriteStep(step, "Load Texture Info");

gpuInfo = LoadGpuMetadata(sourceDir, silentLog);
}

step++;

if(!silentLog)
TraceLogger.WriteStep(step, "Add Files");

ResourceMap entries = new(fileCount, textureCount);

MemoryStream part0 = new();
MemoryStream part1 = new();

var compressionFlags = cfg.CompressionFlags;
var endian = cfg.Endian;

int residentSize = 0;
int residentSizeZl = 0;

bool hasResidents = fileCount > 0;

if(hasResidents)
{
AddFiles(files, resNames, part0, entries.ResidentFiles, progress, overrides, silentLog);

CompressPart(part0, compressionFlags, out residentSize, out residentSizeZl);
}

int gpuSize = 0;
int gpuSizeZl = 0;

if(hasTextures)
{
AddTextures(textures, ptxNames, gpuInfo, part1, entries.TextureFiles, progress, overrides, silentLog);

CompressPart(part1, compressionFlags, out gpuSize, out gpuSizeZl);
}

MemoryStream resMap = new();
BuildResMap(resMap, entries, endian, silentLog);

int resMapPos = 92;
var resMapLen = (int)resMap.Length;

int entriesBlock = resMapPos + resMapLen;
int mapPadding = RsgHelper.ComputePadding(entriesBlock, true);

resMap.Fill(mapPadding);

var sectionLen = (uint)(entriesBlock + mapPadding);

uint residentPos = sectionLen;
var gpuPos = (uint)(residentPos + part0.Length);

RsgInfo info = new()
{
MajorVersion = cfg.MajorVersion,
MinorVersion = cfg.MinorVersion,
CompressionFlags = compressionFlags,
SectionLength = sectionLen,
ResidentDataOffset = residentPos,
ResidentDataSizeCompressed = (uint)residentSizeZl,
ResidentDataSize = (uint)residentSize,
GPUDataOffset = gpuPos,
GPUDataSizeCompressed = (uint)gpuSizeZl,
GPUDataSize = (uint)gpuSize,
ResMapLength = (uint)resMapLen,
ResMapOffset = (uint)resMapPos
};

return new(endian, info, resMap, part0, part1);
}

// Write header

private static void WriteHeader(Stream writer, in RsgInfo info, Endianness endian, bool silentLog)
{

if(!silentLog)
TraceLogger.WriteActionStart("Writing header...");

writer.WriteUInt32(RsgConstants.MAGIC, endian);
info.Write(writer, endian);

if(!silentLog)
TraceLogger.WriteActionEnd();

}

// Write ResMap

private static void WriteMap(Stream writer, MemoryStream resMap, bool silentLog)
{

if(!silentLog)
TraceLogger.WriteActionStart("Writing ResMap...");

resMap.Seek(0, SeekOrigin.Begin);

FileManager.Process(resMap, writer);

if(!silentLog)
TraceLogger.WriteActionEnd();

}

// Write Part0

private static void WritePart0(Stream writer, MemoryStream part0, bool silentLog)
{

if(!silentLog)
TraceLogger.WriteActionStart("Writing Part0...");

part0.Seek(0, SeekOrigin.Begin);

FileManager.Process(part0, writer);

if(!silentLog)
TraceLogger.WriteActionEnd();

}

// Write Part1

private static void WritePart1(Stream writer, MemoryStream part1, bool silentLog)
{

if(!silentLog)
TraceLogger.WriteActionStart("Writing Part1...");

part1.Seek(0, SeekOrigin.Begin);

FileManager.Process(part1, writer);

if(!silentLog)
TraceLogger.WriteActionEnd();

}

// Write ctx

private static void WriteCtx(RsgPackerContext ctx, Stream target, ref int step, bool silentLog)
{
step++;

if(!silentLog)
TraceLogger.WriteStep(step, "Write Metadata");

var endian = ctx.Endian;

WriteHeader(target, ctx.Info, endian, silentLog);

step++;

if(!silentLog)
TraceLogger.WriteStep(step, "Write Resource Map");

var resMap = ctx.ResMap;
WriteMap(target, resMap, silentLog);

resMap.Dispose();

var part0 = ctx.Part0;

if(part0.Length > 0)
{
step++;

if(!silentLog)
TraceLogger.WriteStep(step, "Write Part0");

WritePart0(target, part0, silentLog);

part0.Dispose();
}

var part1 = ctx.Part1;

if(part1.Length > 0)
{
step++;

if(!silentLog)
TraceLogger.WriteStep(step, "Write Part1");

WritePart1(target, part1, silentLog);

part1.Dispose();
}

}

// Compress RSG (Core)

internal static void CompressCore(string sourceDir, 
                                  Stream target,
								  RsgParams cfg,
								  ref int step,
                                  FileProgress progress,
                                  RsbGroupInfo expected,
							      ReadonlyBufferMap overrides,
							      bool silentLog)
{
var ctx = InitCtx(sourceDir, cfg, progress, ref step, silentLog, expected, overrides);

if(ctx == null)
return;

WriteCtx(ctx, target, ref step, silentLog);
}

// Compress RSG Dir to Stream

public static void Compress(string sourceDir, 
                            Stream target,
                            FileProgress progress = null,
                            RsbGroupInfo expected = null,
							ReadonlyBufferMap overrides = null,
							bool silentLog = true)
{
int step = 1;
var cfg = LoadConfig(sourceDir, silentLog, ref step);

CompressCore(sourceDir, target, cfg, ref step, progress, expected, overrides, silentLog);
}

// Pack internal

private static void PackInternal(string sourceDir, string targetFile, FileProgress progress,
                                 TraceContext ctx)
{
PathHelper.ChangeExtension(ref targetFile, ".rsg");

using var rsgStream = FileManager.OpenWrite(targetFile);
Compress(sourceDir, rsgStream, progress, silentLog: false);

ctx.OutputSize = rsgStream.Length;
ctx.LogOutSize = true;
}

// Pack RSG folder as FileStream

public static void Pack(string sourceDir, string targetFile, FileProgress progress = null)
{

TraceExecutor.Run("RSG Build", 
                  ctx => PackInternal(sourceDir, targetFile, progress, ctx),
                  ("SourceDir", sourceDir),
                  ("TargetFile", targetFile)
);

}

}

}