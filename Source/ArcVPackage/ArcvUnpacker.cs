using static ParallelTables;

using System;
using System.IO;
using System.Threading.Tasks;

namespace SexyPackages.ArcVPackage
{
/** <summary> Unpacks the Content of an ARC-V Package. </summary>

<remarks> This format is used in <c>Nintendo DS</c> ROMs (such as PvZ DS). </remarks> **/

public static class ArcvUnpacker
{
// Read info

private static ArcvInfo ReadInfo(Stream reader)
{
TraceLogger.WriteActionStart("Reading header...");

uint flags = reader.ReadUInt32(Endianness.BigEndian);
var expected = ArcvConstants.MAGIC;

if(flags != expected)
throw new Exception($"Invalid identifier: 0x{flags:X8}, expected: 0x{expected:X8}");

var info = ArcvInfo.Read(reader);

TraceLogger.WriteActionEnd();

return info;
}

// Read Entries

private static ResInfo[] ReadEntries(Stream reader, int fileCount, out long totalBytes)
{
totalBytes = 0;

TraceLogger.WriteActionStart("Reading entries...");

ResInfo[] entries = new ResInfo[fileCount];

for(int i = 0; i < fileCount; i++)
{
var entry = ResInfo.Read(reader);
entries[i] = entry;

totalBytes += entry.Size;
}

TraceLogger.WriteActionEnd();

return entries;
}

// Write Res Stream

private static void WriteRes(NativeBuffer buffer, long offset, string path, int size)
{
using var resStream = FileManager.OpenWrite(path, size);
var resData = buffer.AsSpan(offset, size);

resStream.Write(resData);
}

// Get extension from Flags

private static string GetExtension(uint flags)
{

return flags switch
{
0x4E415243 => ".narc",
0x4E4D4152 => ".nmar",
0x4E4D4352 => ".nmcr",
0x4E534352 => ".nscr",
0x4E434552 => ".ncer",
0x4E434752 => ".ncgr",
0x4E434C52 => ".nclr",
0x4E414E52 => ".nanr",
0x4E465452 => ".nftr",
0x53444154 => ".sdat",
_ => ".dat"
};

}

// Build Resouce Path

public static string BuildResPath(string baseDir, uint id, uint flags)
{
var extractionDir = Path.Combine(baseDir, ArcvConstants.ROOT_DIR);
Directory.CreateDirectory(extractionDir);

string fileNumber = id.ToString().PadLeft(10, '0');
string fileExt = GetExtension(flags);

return Path.Combine(extractionDir, fileNumber + fileExt);
}

// Extract res

private static void ExtractRes(string outputDir, NativeBuffer buffer, long dataStart, in ResInfo info)
{
var resOffset = info.Offset - dataStart;
var size = (int)Math.Min(info.Size, int.MaxValue);

uint flags = buffer.GetUInt32(resOffset);
string resPath = BuildResPath(outputDir, info.ID, flags);

WriteRes(buffer, resOffset, resPath, size);
}

// Extract resources (single thread)

private static void ExtractFiles(string outputDir, long dataStart, NativeBuffer buffer,
                                 ResInfo[] entries)
{

foreach(var entry in entries)
ExtractRes(outputDir, buffer, dataStart, entry);

}

// Extract resources (multi-thread)

private static void ExtractFilesInParallel(string outputDir, long dataStart, NativeBuffer buffer,
                                           ResInfo[] entries)
{
Parallel.ForEach(entries, entry => ExtractRes(outputDir, buffer, dataStart, entry) ); 
}

// Extract resources (Core)

private static void ExtractFilesCore(string outputDir, long dataStart, NativeBuffer buffer,
                                     ResInfo[] entries)
{
TraceLogger.WriteActionStart("Extracting resources...");

if(entries.Length <= MAX_FILES_SINGLE_THREAD)
ExtractFiles(outputDir, dataStart, buffer, entries);

else
ExtractFilesInParallel(outputDir, dataStart, buffer, entries);

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

TraceLogger.WriteStep(3, "Extract Resources");

long dataStart = source.Position;
using var resBlob = source.ReadPtr();

RAMDisk.TryRedirect(ref outputDir, fileCount, totalBytes, options);

ExtractFilesCore(outputDir, dataStart, resBlob, entries);
}

// Unpack internal

private static void UnpackInternal(string inputFile, string outputDir, RAMDiskOptions options)
{
using var arcvStream = FileManager.OpenRead(inputFile);

Decompress(arcvStream, outputDir, options);
}

// Unpack Stream as Dir

public static void Unpack(string inputFile, string outputDir, RAMDiskOptions options = null)
{

TraceExecutor.Run("ARC-V Extraction", 
                  ctx => UnpackInternal(inputFile, outputDir, options),
                  ("InputFile", inputFile),
                  ("OutputDir", outputDir)
);

}

}

}