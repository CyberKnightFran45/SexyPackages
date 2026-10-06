using static ParallelTables;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using BlossomLib.Modules.Compression;

namespace SexyPackages.PopCapPackage
{
/// <summary> Unpacks the Content of a PopCap Package (PAK). </summary>

public static class PakExtractor
{
// Read info

private static PakPlatform ReadInfo(Stream reader)
{
TraceLogger.WriteActionStart("Reading header...");

var magic = reader.ReadUInt32();

PakPlatform platform = magic switch
{
PakConstants.MAGIC => PakPlatform.Xbox360,
PakConstants.MAGIC_WINDOWS => PakPlatform.Windows,
PakConstants.MAGIC_ANDROID_TV => PakPlatform.AndroidTV,
PakConstants.MAGIC_XMEM => PakPlatform.XMEM,
_ => (PakPlatform)(-1)
};

TraceLogger.WriteActionEnd();

return platform;
}

// Read PAK Entries

private static Dictionary<string, ResInfo> ReadEntries(Stream reader, out long totalBytes)
{
totalBytes = 0;

TraceLogger.WriteActionStart("Reading entries...");
Dictionary<string, ResInfo> entries = new();

while(true)
{
byte contentType = reader.ReadUInt8();

if( (contentType & PakConstants.ENTRIES_END) != 0)
break;

using var pOwner = reader.ReadStringByLen8(PakConstants.ENCODING);
string resName = pOwner.ToString();

var info = ResInfo.Read(reader);
entries.Add(resName, info);

totalBytes += info.Size;
}

TraceLogger.WriteActionEnd();

TraceLogger.WriteInfo($"Resources embedded: {entries.Count}");

return entries;
}

// Read PAK Entries (ZLib variant)

private static Dictionary<string, ResInfoZlib> ReadEntriesZl(Stream reader, out long totalBytes)
{
Dictionary<string, ResInfoZlib> entries = new();
totalBytes = 0;

TraceLogger.WriteActionStart("Reading entries...");

while(true)
{
byte contentType = reader.ReadUInt8();

if( (contentType & PakConstants.ENTRIES_END) != 0)
break;

using var pOwner = reader.ReadStringByLen8(PakConstants.ENCODING);
string resName = pOwner.ToString();

var info = ResInfoZlib.Read(reader);
entries.Add(resName, info);

totalBytes += info.RawSize;
}

TraceLogger.WriteActionEnd();

TraceLogger.WriteInfo($"Resources embedded: {entries.Count}");

return entries;
}

// Get relative offset

private static long GetOffset(NativeBuffer buffer,
                              bool checkAlignment,
                              long dataStart,
                              bool isPtx,
                              long baseOffset,
                              uint size)
{
long blobPos = dataStart + baseOffset;
bool alignData = checkAlignment && PakHelper.IsAligned(blobPos, isPtx);

long offset = 0;

if(alignData)
{
ushort alignSize = buffer.GetUInt16(baseOffset);

offset += 2;
offset += alignSize;
}

offset += size;

return offset;
}

// Precalculate offsets

private static Dictionary<string, long> ComputeOffsets(long dataStart,
                                                       NativeBuffer buffer,
                                                       bool checkAlignment,
                                                       Dictionary<string, ResInfo> entries)
{
Dictionary<string, long> map = new(entries.Count);
long offset = 0;

foreach(var e in entries)
{
var name = e.Key;
map.Add(name, offset);

bool isPtx = name.EndsWith(".ptx", StringComparison.OrdinalIgnoreCase);
var info = e.Value;

offset += GetOffset(buffer, checkAlignment, dataStart, isPtx, offset, info.Size);
}

return map;
}

// Precalculate offsets (ZLib variant)

private static Dictionary<string, long> ComputeOffsetsZl(long dataStart,
                                                         NativeBuffer buffer,
                                                         bool checkAlignment,
                                                         Dictionary<string, ResInfoZlib> entries)
{
Dictionary<string, long>  map = new(entries.Count);
long offset = 0;

foreach(var e in entries)
{
var name = e.Key;
map.Add(name, offset);

bool isPtx = name.EndsWith(".ptx", StringComparison.OrdinalIgnoreCase);
var info = e.Value;

offset += GetOffset(buffer, checkAlignment, dataStart, isPtx, offset, info.SizeCompressed);
}

return map;
}

// Decompress ZLib Stream

private static void DecompressStream(Stream target, ReadOnlySpan<byte> data)
{
using MemoryStream compressed = new();

compressed.Write(data);
compressed.Seek(0, SeekOrigin.Begin);

using ZLibStream decompressor = new(compressed, CompressionMode.Decompress);

ZLibCompressor.DecompressStream(decompressor, target); 
}

// Build res path

private static string BuildResPath(string baseDir, string resName)
{
var resourcesDir = Path.Combine(baseDir, PakConstants.SRC_RESOURCES);
Directory.CreateDirectory(resourcesDir);

return Path.Combine(resourcesDir, resName);
}

// Write Res Stream

private static void WriteRes(NativeBuffer buffer,
                             long offset,
                             string path,
                             int sizeCompressed,
                             int rawSize,
                             long creationTime)
{
using var resStream = FileManager.OpenWrite(path, rawSize);

if(sizeCompressed > 0)
{
var zlData = buffer.GetView(offset, sizeCompressed);

DecompressStream(resStream, zlData);
}

else
{
var rawData = buffer.GetView(offset, rawSize);

resStream.Write(rawData);
}

var timeUtc = DateTime.FromFileTimeUtc(creationTime);

File.SetLastWriteTimeUtc(path, timeUtc);
}

// Extract res

private static void ExtractRes(NativeBuffer buffer, long offset, string path, in ResInfo info)
{
var size = (int)Math.Min(info.Size, int.MaxValue);

WriteRes(buffer, offset, path, 0, size, info.CreationTime);
}

// Extract resources (single thread)

private static void ExtractFiles(string outputDir,
                                 NativeBuffer buffer,
                                 Dictionary<string, ResInfo> entries,
                                 Dictionary<string, long> offsetMap)
{

foreach(var entry in entries)
{
string resName = entry.Key;
string resPath = BuildResPath(outputDir, resName);

ExtractRes(buffer, offsetMap[resName], resPath, entry.Value);
}

}

// Extract resources (multi-thread)

private static void ExtractFilesInParallel(string outputDir,
                                           NativeBuffer buffer,
                                           Dictionary<string, ResInfo> entries,
                                           Dictionary<string, long> offsetMap)
{

Parallel.ForEach(entries, entry =>
{
string resName = entry.Key;
string resPath = BuildResPath(outputDir, resName);

ExtractRes(buffer, offsetMap[resName], resPath, entry.Value);
}

); 

}

// Extract resources (Core)

private static void ExtractFilesCore(string outputDir,
                                     long dataStart,
                                     NativeBuffer buffer,
                                     bool checkAlignment,
                                     Dictionary<string, ResInfo> entries)
{
TraceLogger.WriteActionStart("Extracting resources...");

var offsetMap = ComputeOffsets(dataStart, buffer, checkAlignment, entries);

if(entries.Count <= MAX_FILES_SINGLE_THREAD)
ExtractFiles(outputDir, buffer, entries, offsetMap);

else
ExtractFilesInParallel(outputDir, buffer, entries, offsetMap);

TraceLogger.WriteActionEnd();
}

// Extract res (ZLib variant)

private static void ExtractResZl(NativeBuffer buffer, long offset, string path, in ResInfoZlib info)
{
var sizeCompressed = (int)Math.Min(info.SizeCompressed, int.MaxValue);
var rawSize = (int)Math.Min(info.RawSize, int.MaxValue);

WriteRes(buffer, offset, path, sizeCompressed, rawSize, info.CreationTime);
}

// Extract Zlib resources (single thread)

private static void ExtractZlFiles(string outputDir,
                                   NativeBuffer buffer,
                                   Dictionary<string, ResInfoZlib> entries,
                                   Dictionary<string, long> offsetMap)
{

foreach(var entry in entries)
{
string resName = entry.Key;
string resPath = BuildResPath(outputDir, resName);

ExtractResZl(buffer, offsetMap[resName], resPath, entry.Value);
}

}

// Extract ZLib resources (multi-thread)

private static void ExtractZlFilesInParallel(string outputDir,
                                             NativeBuffer buffer,
                                             Dictionary<string, ResInfoZlib> entries,
                                             Dictionary<string, long> offsetMap)
{

Parallel.ForEach(entries, entry =>
{
string resName = entry.Key;
string resPath = BuildResPath(outputDir, resName);

ExtractResZl(buffer, offsetMap[resName], resPath, entry.Value);
}

); 

}

// Extract ZLib resources (Core)

private static void ExtractZlFilesCore(string outputDir,
                                       long dataStart,
                                       NativeBuffer buffer,
                                       bool checkAlignment,
                                       Dictionary<string, ResInfoZlib> entries)
{
TraceLogger.WriteActionStart("Extracting resources...");

var offsetMap = ComputeOffsetsZl(dataStart, buffer, checkAlignment, entries);

if(entries.Count <= MAX_FILES_SINGLE_THREAD)
ExtractZlFiles(outputDir, buffer, entries, offsetMap);

else
ExtractZlFilesInParallel(outputDir, buffer, entries, offsetMap);

TraceLogger.WriteActionEnd();
}

// Save PakConfig

private static void SaveConfig(string outputDir, PakPlatform platform, bool useZlib)
{
TraceLogger.WriteActionStart("Saving config...");

PakConfig cfg = new(platform, useZlib);
string infoPath = Path.Combine(outputDir, "PakInfo.json");

using var cfgStream = FileManager.OpenWrite(infoPath);
JsonSerializer.SerializeObject(cfg, cfgStream, PakConfig.Context);

TraceLogger.WriteActionEnd();
}

// Extract content

private static void ExtractContent(Stream source,
                                   bool alignData,
                                   bool useZlib,
                                   RAMDiskOptions options,
                                   ref string outputDir,
                                   ref int step)
{
uint version = source.ReadUInt32();
var expectedVer = PakConstants.VERSION;

if(version != expectedVer)
TraceLogger.WriteWarn($"Unknown version: v{version} - Expected: v{expectedVer}");

step++;

TraceLogger.WriteStep(step, "Read Resource Entries");

Dictionary<string, ResInfo> entries;
Dictionary<string, ResInfoZlib> entriesZl;

int fileCount;
long totalBytes;

if(useZlib)
{
entries = null;

entriesZl = ReadEntriesZl(source, out totalBytes);
fileCount = entriesZl.Count;
}

else
{
entries = ReadEntries(source, out totalBytes);
fileCount = entries.Count;

entriesZl = null;
}

step++;

TraceLogger.WriteStep(step, "Extract Resources");

long dataStart = source.Position;
using var resBlob = source.ReadPtr();

RAMDisk.TryRedirect(ref outputDir, fileCount, totalBytes, options);

if(entriesZl is not null)
ExtractZlFilesCore(outputDir, dataStart, resBlob, alignData, entriesZl);

else
ExtractFilesCore(outputDir, dataStart, resBlob, alignData, entries);

}

// Extract and decrypt content

private static void ExtractAndDecryptContent(Stream source, 
                                             bool alignData,
                                             bool useZlib,
                                             RAMDiskOptions options,
                                             ref string outputDir,
                                             ref int step)
{
using XorStream decryptor = new(source, PakConstants.KEY);

ExtractContent(decryptor, alignData, useZlib, options, ref outputDir, ref step);
}

// Decompress Stream

public static void Decompress(Stream source, string outputDir, RAMDiskOptions options)
{
int step = 1;

TraceLogger.WriteStep(step, "Read Metadata");

var platform = ReadInfo(source);
bool useZlib = false;

switch(platform)
{
case PakPlatform.XMEM:
TraceLogger.WriteWarn("XMEM is not supported, use xbdecompress instead");
return;

case PakPlatform.AndroidTV:
ZipCompressor.DecompressStream(source, outputDir);
break;

case PakPlatform.Xbox360:
useZlib = true;

ExtractContent(source, true, useZlib, options, ref outputDir, ref step);
break;

case PakPlatform.Windows:
ExtractAndDecryptContent(source, false, useZlib, options, ref outputDir, ref step);
break;

default:
throw new Exception("Invalid Package identifier");
}

step++;

TraceLogger.WriteStep(step, "Save PAK Config");
SaveConfig(outputDir, platform, useZlib);
}

// Unpack internal

private static void UnpackInternal(string inputFile, string outputDir, RAMDiskOptions options)
{
using var pakStream = FileManager.OpenRead(inputFile);

Decompress(pakStream, outputDir, options);
}

// Unpack Stream as Dir

public static void Unpack(string inputFile, string outputDir, RAMDiskOptions options = null)
{

TraceExecutor.Run("PAK Extraction", 
                  ctx => UnpackInternal(inputFile, outputDir, options),
                  ("InputFile", inputFile),
                  ("OutputDir", outputDir)
);

}

}

}