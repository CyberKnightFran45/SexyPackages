using static ParallelTables;

using System;
using System.IO;
using System.Threading.Tasks;

namespace SexyPackages.XboxPackedResource
{
/// <summary> Unpacks the Content of a Xbox Package (XPR). </summary>

public static class XprUnpacker
{
// Read info

private static XprInfo ReadInfo(Stream reader)
{
TraceLogger.WriteActionStart("Reading header...");

uint flags = reader.ReadUInt32(Endianness.BigEndian);

if(flags != XprConstants.MAGIC)
throw new Exception($"Invalid identifier: 0x{flags:X8}, expected: 0x{XprConstants.MAGIC:X8}");

var info = XprInfo.Read(reader);

TraceLogger.WriteActionEnd();

return info;
}

// Read XPR Entries

private static XResInfo[] ReadEntries(Stream reader, int fileCount, out long totalBytes)
{
totalBytes = 0;

TraceLogger.WriteActionStart("Reading entries...");

XResInfo[] entries = new XResInfo[fileCount];

for(int i = 0; i < fileCount; i++)
{
var entry = XResInfo.Read(reader);
entries[i] = entry;

totalBytes += entry.FileSize;
}

TraceLogger.WriteActionEnd();

return entries;
}

// Read Paths as C-string

private static string[] ReadPaths(Stream reader, int fileCount)
{
TraceLogger.WriteActionStart("Reading paths...");

string[] resPaths = new string[fileCount];

for(int i = 0; i < fileCount; i++)
{
using var pOwner = reader.ReadCString(XprConstants.ENCODING);

resPaths[i] = pOwner.ToString();
}

TraceLogger.WriteActionEnd();

return resPaths;
}

// Write Res Stream

private static void WriteRes(NativeBuffer buffer, long offset, string filePath, int fileSize)
{
using var resStream = FileManager.OpenWrite(filePath, fileSize);
var resData = buffer.AsSpan(offset, fileSize);

resStream.Write(resData);
}

// Build Path for Resource

private static string BuildResPath(string baseDir, uint root, string resName)
{
string rootDir = String32.FromInt(root);
string outDir = PathHelper.SafeCombine(baseDir, XprConstants.SRC_RESOURCES, rootDir);

Directory.CreateDirectory(outDir);

return PathHelper.SafeCombine(outDir, resName);
}

// Extract res

private static void ExtractRes(string outputDir, NativeBuffer buffer, long dataStart,
                               string path, in XResInfo info)
{
var resOffset = info.FileOffset - dataStart;
var size = (int)Math.Min(info.FileSize, int.MaxValue);

string resPath = BuildResPath(outputDir, info.RootDir, path);

WriteRes(buffer, resOffset, resPath, size);
}

// Extract resources (single thread)

private static void ExtractFiles(string outputDir, long dataStart, NativeBuffer buffer,
                                 XResInfo[] entries, string[] paths)
{

for(int i = 0; i < entries.Length; i++)
ExtractRes(outputDir, buffer, dataStart, paths[i], entries[i]);

}

// Extract resources (multi-thread)

private static void ExtractFilesInParallel(string outputDir, long dataStart, NativeBuffer buffer,
                                           XResInfo[] entries, string[] paths)
{
int total = entries.Length;
int batchSize = BatchHelper.ComputeBatchSize(total, buffer.Size);

for(int start = 0; start < total; start += batchSize)
{
int end = Math.Min(start + batchSize, total);

Parallel.For(start, end, i =>
{
ExtractRes(outputDir, buffer, dataStart, paths[i], entries[i] );
}

);

}

}

// Extract resources (Core)

private static void ExtractFilesCore(string outputDir, long dataStart, NativeBuffer buffer,
                                     XResInfo[] entries, string[] paths)
{
TraceLogger.WriteActionStart("Extracting resources...");

if(entries.Length <= MAX_FILES_SINGLE_THREAD)
ExtractFiles(outputDir, dataStart, buffer, entries, paths);

else
ExtractFilesInParallel(outputDir, dataStart, buffer, entries, paths);

TraceLogger.WriteActionEnd();
}

// Decompress Stream

public static void Decompress(Stream source, string outputDir, RAMDiskOptions options = null)
{
TraceLogger.WriteStep(1, "Read Metadata");
var info = ReadInfo(source);

var fileCount = (int)info.FileCount;

TraceLogger.WriteInfo($"Resources embedded: {fileCount}");

TraceLogger.WriteStep(2, "Read Resource Entries");
var entries = ReadEntries(source, fileCount, out var totalBytes);

TraceLogger.WriteStep(3, "Read Paths Table");
var pathsTable = ReadPaths(source, fileCount);

TraceLogger.WriteStep(4, "Extract Resources");

long dataStart = source.Position;
using var resBlob = source.ReadPtr();

RAMDisk.TryRedirect(ref outputDir, fileCount, totalBytes, options);

ExtractFilesCore(outputDir, dataStart, resBlob, entries, pathsTable);
}

// Unpack internal

private static void UnpackInternal(string inputFile, string outputDir, RAMDiskOptions options)
{
using var xprStream = FileManager.OpenRead(inputFile);

Decompress(xprStream, outputDir, options);
}

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