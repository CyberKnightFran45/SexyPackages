using System;
using System.Collections.Generic;
using System.IO;
using BlossomLib.Modules.Compression;

namespace SexyPackages.MarmaladeDZip
{
/** <summary> Unpacks the Content of a DZip file </summary>

<remarks> Extracted from <c>Marmalade SDK - Derbh API</c>, 
          inner DZip chunks are not fully supported </remarks> **/

public static class DzUnpacker
{
// Read header

private static DzInfo ReadInfo(Stream reader)
{
TraceLogger.WriteActionStart("Reading header...");

uint flags = reader.ReadUInt32(Endianness.BigEndian);

if(flags != DzConstants.MAGIC)
throw new Exception($"Invalid identifier: 0x{flags:X8}, expected: 0x{DzConstants.MAGIC:X8}");

var info = DzInfo.Read(reader);

TraceLogger.WriteActionEnd();

return info;
}

// Read Names as C-strings

private static string[] ReadNames(Stream reader, int count, string msg, bool addRoot = false)
{
TraceLogger.WriteActionStart(msg);

string[] names = new string[count];
int startIdx = 0;

if(addRoot)
{
names[startIdx] = "";

startIdx++;
}

for(int i = startIdx; i < count; i++)
{
using var nOwner = reader.ReadCString(DzConstants.ENCODING);

names[i] = nOwner.ToString();
}

TraceLogger.WriteActionEnd();

return names;
}

// Read chunks map

private static List<ChunkMapEntry> ReadChkMap(Stream reader, ushort fileCount,
                                              out int maxChunkIdx,
									          out ushort archives,
										      out ushort chunks)
{
maxChunkIdx = -1;

TraceLogger.WriteActionStart("Reading chunks map...");

List<ChunkMapEntry> map = new(fileCount);

for(ushort i = 0; i < fileCount; i++)
{
ushort folderIdx = reader.ReadUInt16(Endianness.LittleEndian);
ushort chunkIdx;

while( (chunkIdx = reader.ReadUInt16(Endianness.LittleEndian) ) != DzConstants.CHUNKS_END)
{
ChunkMapEntry entry = new(folderIdx, i, chunkIdx);
map.Add(entry);

if(chunkIdx > maxChunkIdx)
maxChunkIdx = chunkIdx;

}

}

archives = reader.ReadUInt16(Endianness.LittleEndian);
chunks = reader.ReadUInt16(Endianness.LittleEndian);

TraceLogger.WriteActionEnd();

return map;
}

// Read chunks data

private static ChunkMetadata[] ReadChunkData(Stream reader, int count, out long totalBytes)
{
totalBytes = 0;

TraceLogger.WriteActionStart("Reading chunks data...");

ChunkMetadata[] metadata = new ChunkMetadata[count];

for(int i = 0; i < count; i++)
{
var data = ChunkMetadata.Read(reader);
metadata[i] = data;

totalBytes += data.ChunkSize;
}

TraceLogger.WriteActionEnd();

return metadata;
}

// Read chunk names

private static string[] ReadChunkNames(Stream reader, ushort count)
{
return count > 0 ? ReadNames(reader, count, "Reading chunk names...", true) : [];
}

// Read Chunks

private static void ReadChunks(Stream reader, ushort fileCount,
                               out List<ChunkMapEntry> map,
                               out ChunkMetadata[] metadata,
                               out string[] externalArchives,
                               out long totalBytes)
{
map = ReadChkMap(reader, fileCount, out int maxChunkIdx, out var archivesCount, out var chunksTotal);

TraceLogger.WriteInfo($"Archives: {archivesCount} | Chunks: {chunksTotal}");

int actualCount = Math.Max(maxChunkIdx + 1, chunksTotal);
metadata = ReadChunkData(reader, actualCount, out totalBytes);

externalArchives = ReadChunkNames(reader, archivesCount);
}

// Group chunks by index

private static Dictionary<int, List<int>> GroupEntries(ChunkMetadata[] metadata)
{
Dictionary<int, List<int>> groups = new();

for(int i = 0; i < metadata.Length; i++)
{
int file = metadata[i].FileIndex;

if(!groups.ContainsKey(file) )
groups[file] = new(); // Overwrite info

groups[file].Add(i);
}

return groups;
}

// Compute file sizes (compressed)

private static int[] ComputeSizes(ChunkMetadata[] metadata)
{
int count = metadata.Length;

int[] sizes = new int[count];
int[] order = new int[count];

var entriesByArchive = GroupEntries(metadata);

foreach(var table in entriesByArchive.Values)
{
table.Sort( (a, b) => metadata[a].ChunkOffset.CompareTo(metadata[b].ChunkOffset) );

for(int i = 0; i < table.Count - 1; i++)
{
int cur = table[i];
int next = table[i + 1];

sizes[cur] = metadata[next].ChunkOffset - metadata[cur].ChunkOffset;
}

int last = table[^1];
var m = metadata[last];

if( (m.CompressionFlags & DzFlags.STORE) != 0)
sizes[last] = m.ChunkSize;

else
sizes[last] = -1; // last chunk in archive; read until EOF of its stream

}

return sizes;
}

// Resolve chunk entries

private static ChunkEntry[] ResolveEntries(List<ChunkMapEntry> map, ChunkMetadata[] metadata,
                                           string[] fileNames, string[] dirNames)
{
TraceLogger.WriteActionStart("Resolving chunk entries...");

var computedSizes = ComputeSizes(metadata);
ChunkEntry[] entries = new ChunkEntry[map.Count];

for(int i = 0; i < map.Count; i++)
{
var m = map[i];

var data = metadata[m.ChunkIndex];
int sizeCompressed = computedSizes[m.ChunkIndex];

entries[i] = new(data, fileNames[m.FileIndex], dirNames[m.FolderIndex], sizeCompressed);
}

TraceLogger.WriteActionEnd();

return entries;
}

// Get external stream

private static Stream GetExternalStream(string baseDir, int idx, string[] names,
                                        Dictionary<int, Stream> cache)
{

if(cache.TryGetValue(idx, out var s) )
return s;

string path = Path.Combine(baseDir, names[idx]);
var newStream = FileManager.OpenRead(path);

cache[idx] = newStream;

return newStream;
}

// Process chunk

private static void ProcessChunk(Stream source, string outPath, DzFlags flags, 
                                 int rawSize, int sizeCompressed)
{ 
using var outStream = FileManager.OpenWrite(outPath, rawSize);

if( (flags & DzFlags.DZ) != 0)
FileManager.Process(source, outStream, sizeCompressed);

else if( (flags & DzFlags.STORE) != 0)
FileManager.Process(source, outStream, sizeCompressed);

else if( (flags & DzFlags.ZERO) != 0)
outStream.Fill(sizeCompressed);
    
else if( (flags & DzFlags.GZIP) != 0)  
{
using SubStream gzip = new(source, source.Position, sizeCompressed);

GZipCompressor.DecompressStream(gzip, outStream);
}

else if( (flags & DzFlags.BZIP) != 0)
{
using SubStream bz2 = new(source, source.Position, sizeCompressed);

BZip2Compressor.DecompressStream(bz2, outStream);
}

else if( (flags & DzFlags.LZMA) != 0)
LzmaCompressor.Decompress(source, outStream, sizeCompressed);

else
FileManager.Process(source, outStream, sizeCompressed);

}

// Build output path

private static string BuildOutPath(string outputDir, string dirName, string fileName, int partIndex,
                                   out string relativePath)
{
string targetDir = Path.Combine(outputDir, dirName);
Directory.CreateDirectory(targetDir);

string finalName = fileName;

if(partIndex > 0)
{
string baseName = Path.GetFileNameWithoutExtension(fileName);
string ext = Path.GetExtension(fileName);

finalName = $"{baseName}_multi_{partIndex}{ext}";
}

string fullPath = Path.Combine(targetDir, finalName);
relativePath = Path.GetRelativePath(outputDir, fullPath).Replace('\\', '/');

return fullPath;
}

// Extract res

private static void ExtractRes(Stream source, ChunkEntry entry, string outputDir, int partIndex,
                               Dictionary<string, DzFlags> resMap)
{

string targetPath = BuildOutPath(outputDir, entry.FolderPath, entry.FileName, partIndex,
                                 out var relativePath);

resMap[relativePath] = entry.Metadata.CompressionFlags; // Overwrite duplicates (for multi-part)

source.Seek(entry.Metadata.ChunkOffset, SeekOrigin.Begin);

int size = entry.Metadata.ChunkSize;
var sizeZl = entry.SizeCompressed < 0 ? (int)(source.Length - source.Position) : entry.SizeCompressed;

ProcessChunk(source, targetPath, entry.Metadata.CompressionFlags, size, sizeZl);
}

// Handle multi-part chunks

private static void UpdateMultiPartState(ChunkEntry chk, ref string curFile, ref string curDir,
                                         ref bool inMultiBlock, ref int partIndex)
{
bool isSameFile = chk.FileName == curFile && chk.FolderPath == curDir;
bool isCombuf = (chk.Metadata.CompressionFlags & DzFlags.COMBUF) != 0;

if(!isSameFile)
{
curFile = chk.FileName;
curDir = chk.FolderPath;

partIndex = 0;
inMultiBlock = false;

return;
}

partIndex++;
inMultiBlock = isCombuf;
}

// Extract resources (single thread)

private static Dictionary<string, DzFlags> ExtractFiles(string baseDir, string outputDir, Stream source,
                                                        ChunkEntry[] chunks, string[] externalArchives)
{
TraceLogger.WriteActionStart("Extracting resources...");

Dictionary<string, DzFlags> resMap = new();
Dictionary<int, Stream> streamsCache = new();

try
{
string currentFile = null;
string currentFolder = null;

bool inMultiBlock = false;
int partIndex = 0;

foreach(var chk in chunks)
{
UpdateMultiPartState(chk, ref currentFile, ref currentFolder, ref inMultiBlock, ref partIndex);

int fileIdx = chk.Metadata.FileIndex;

Stream currentSrc;

if(fileIdx > 0)
currentSrc = GetExternalStream(baseDir, fileIdx, externalArchives, streamsCache);

else
currentSrc = source;

ExtractRes(currentSrc, chk, outputDir, partIndex, resMap);
}

}

finally
{
	
foreach(var s in streamsCache.Values)
s.Dispose();

}
	
TraceLogger.WriteActionEnd();

return resMap;
}

// Save DzConfig

private static void SaveConfig(string outputDir, Dictionary<string, DzFlags> cfg)
{
TraceLogger.WriteActionStart("Saving config...");

string infoPath = Path.Combine(outputDir, DzConstants.SRC_CONFIG);
using var cfgStream = FileManager.OpenWrite(infoPath);

JsonSerializer.SerializeObject(cfg, cfgStream);

TraceLogger.WriteActionEnd();
}

// Decompress Stream

public static void Decompress(FileStream source, string outputDir, RAMDiskOptions options = null)
{
TraceLogger.WriteStep(1, "Read Metadata");
var info = ReadInfo(source);

ushort fileCount = info.FileCount;
ushort dirCount = info.DirCount;

byte version = info.Version;
var expectedVer = DzConstants.VERSION;

if(version != expectedVer)
TraceLogger.WriteWarn($"Unknown version: v{version} - Expected: v{expectedVer}");

TraceLogger.WriteInfo($"Resources embedded: {fileCount}");

TraceLogger.WriteStep(2, "Read Paths Table");

var resNames = ReadNames(source, fileCount, "Reading file names...");
var dirNames = ReadNames(source, dirCount, "Reading dir names...", true);

TraceLogger.WriteStep(3, "Read Chunks Info");
ReadChunks(source, fileCount, out var map, out var metadata, out var externalArchives, out var totalBytes);

TraceLogger.WriteStep(4, "Resolve Chunk Entries");
var chunks = ResolveEntries(map, metadata, resNames, dirNames);

TraceLogger.WriteStep(5, "Extract Resources");

RAMDisk.TryRedirect(ref outputDir, fileCount, totalBytes, options);

string baseDir = Path.GetDirectoryName(source.Name);
var resMap = ExtractFiles(baseDir, outputDir, source, chunks, externalArchives);

TraceLogger.WriteStep(6, "Save DZ Config");
SaveConfig(outputDir, resMap);
}

// Unpack internal

private static void UnpackInternal(string inputFile, string outputDir, RAMDiskOptions options)
{
using var dzStream = FileManager.OpenRead(inputFile);

Decompress(dzStream, outputDir, options);
}

// Unpack Dzip

public static void Unpack(string inputFile, string outputDir, RAMDiskOptions options = null)
{

TraceExecutor.Run("DZip Unpacking", 
                  ctx => UnpackInternal(inputFile, outputDir, options),
                  ("InputFile", inputFile),
                  ("OutputDir", outputDir)
);

}

}

}