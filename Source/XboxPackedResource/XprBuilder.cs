using System;
using System.Collections.Generic;
using System.IO;

namespace SexyPackages.XboxPackedResource
{
/// <summary> Packs the Content of a Directory as a Xbox Package (XPR). </summary>

public static class XprBuilder
{
// Split path into Root and Name

private static void SplitPath(string path, out uint dirFlags, out string resName)
{
string[] parts = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

string rootDir = parts.Length > 1 ? parts[0] : string.Empty;
dirFlags = String32.ToInt(rootDir);

if(parts.Length > 1)
resName = string.Join(Path.DirectorySeparatorChar, parts.AsSpan(1).ToArray() );

else
resName = parts[0];

resName = resName.Replace('\\', '/');
}
	
// Get files

private static List<string> GetFiles(string baseDir, out List<string> names, out List<uint> dirs)
{
names = new();
dirs = new();

TraceLogger.WriteActionStart("Obtaining files...");

string resDir = Path.Combine(baseDir, XprConstants.SRC_RESOURCES);
var files = Directory.EnumerateFiles(resDir, "*.*", SearchOption.AllDirectories);

List<string> res = new();

foreach(string path in files)
{
res.Add(path);

string resPath = Path.GetRelativePath(resDir, path);
SplitPath(resPath, out var root, out var name);

dirs.Add(root);
names.Add(name);
}

TraceLogger.WriteActionEnd();

return res;
}

// Write single entry

private static void WriteEntry(ChunkedMemoryStream writer, uint rootDir, uint fileOffset,
                               uint fileSize, uint pathOffset)
{
writer.WriteUInt32(rootDir, Endianness.BigEndian);
writer.WriteUInt32(fileOffset, Endianness.BigEndian);

writer.WriteUInt32(fileSize, Endianness.BigEndian);
writer.WriteUInt32(pathOffset, Endianness.BigEndian);
}

// Add Res to Buffer

private static void AddRes(string filePath,
                           string name,
                           ChunkedMemoryStream paths,
                           ChunkedMemoryStream parent,
                           out long pathPos,
                           out long dataPos,
                           out uint size)
{
pathPos = paths.Position;
dataPos = parent.Position;

using var resStream = FileManager.OpenRead(filePath);
size = (uint)resStream.Length;

paths.WriteCString(name, XprConstants.ENCODING);
FileManager.Process(resStream, parent);

int alignment = XprConstants.FILE_ALIGMENT;

if(parent.Position % alignment != 0)
parent.Align(alignment);

}

// Add files

private static void AddFiles(List<string> files,
                             List<string> names,
                             List<uint> dirs,
                             ChunkedMemoryStream entries,
							 ChunkedMemoryStream paths,
							 ChunkedMemoryStream parent,
                             FileProgress progress)
{
TraceLogger.WriteActionStart("Adding files...");

int fileCount = files.Count;

int entriesLen = fileCount * 16;
entries.SetLength(entriesLen);

using NativeMemoryOwner<long> pathRelPos = new(fileCount);
using NativeMemoryOwner<long> dataRelPos = new(fileCount);
using NativeMemoryOwner<uint> sizes = new(fileCount);

// Build PathTable and ResBlob

for(int i = 0; i < fileCount; i++)
{
string name = names[i];
progress?.Invoke(name, i + 1, fileCount);

AddRes(files[i], name, paths, parent, out pathRelPos[i], out dataRelPos[i], out sizes[i]);
}

// Calculate offsets

var pathTableOffset = (uint)(16 + entriesLen);
var dataBlobOffset = (uint)(pathTableOffset + paths.Position);

// Write entries

for(int i = 0; i < fileCount; i++)
{
var pathOffset = (uint)(pathTableOffset + pathRelPos[i] );
var fileOffset = (uint)(dataBlobOffset + dataRelPos[i] );

WriteEntry(entries, dirs[i], fileOffset, sizes[i], pathOffset);
}

TraceLogger.WriteActionEnd();
}

// Write Header

private static void WriteHeader(Stream writer, uint totalSize, int fileCount)
{
TraceLogger.WriteActionStart("Writing header...");

writer.WriteUInt32(XprConstants.MAGIC, Endianness.BigEndian);
writer.WriteUInt32(totalSize, Endianness.BigEndian);

writer.WriteUInt32(0); // <GPUDataSize> (not used)
writer.WriteInt32(fileCount, Endianness.BigEndian);

TraceLogger.WriteActionEnd();
}

// Write XPR Entries

private static void WriteEntries(Stream writer, ChunkedMemoryStream entries)
{
TraceLogger.WriteActionStart("Writing entries...");

entries.Seek(0, SeekOrigin.Begin);
FileManager.Process(entries, writer);

TraceLogger.WriteActionEnd();
}

// Write PathTable

private static void WritePaths(Stream writer, ChunkedMemoryStream paths)
{
TraceLogger.WriteActionStart("Writing paths...");

paths.Seek(0, SeekOrigin.Begin);
FileManager.Process(paths, writer);

TraceLogger.WriteActionEnd();
}

// Write Res data

private static void WriteRes(Stream writer, ChunkedMemoryStream data)
{
TraceLogger.WriteActionStart("Packing resources...");

data.Seek(0, SeekOrigin.Begin);
FileManager.Process(data, writer);

TraceLogger.WriteActionEnd();
}

// Align stream

private static void AlignStream(Stream target)
{
TraceLogger.WriteActionStart("Aligning stream...");
target.Align(2048);

TraceLogger.WriteActionEnd();
}

// Compress Stream

public static void Compress(string sourceDir, Stream target, FileProgress progress = null)
{
TraceLogger.WriteStep(1, "List Files");

var files = GetFiles(sourceDir, out var names, out var dirs);
int fileCount = files.Count;

if(fileCount == 0)
{
string dirName = PathHelper.ShortPath(sourceDir);
TraceLogger.WriteWarn($"'{dirName}' has no files.");

return;
}

TraceLogger.WriteInfo($"Found: {fileCount} files");

TraceLogger.WriteStep(2, "Add Files");

using ChunkedMemoryStream entries = new();
using ChunkedMemoryStream pathsTable = new();
using ChunkedMemoryStream resBlob = new();

AddFiles(files, names, dirs, entries, pathsTable, resBlob, progress);

TraceLogger.WriteStep(3, "Write Metadata");

var totalSize = (uint)(entries.Length + pathsTable.Length + resBlob.Length);
WriteHeader(target, totalSize, fileCount);

TraceLogger.WriteStep(4, "Write Resource Entries");
WriteEntries(target, entries);

TraceLogger.WriteStep(5, "Write Paths Table");
WritePaths(target, pathsTable);

TraceLogger.WriteStep(6, "Pack Resources");
WriteRes(target, resBlob);

TraceLogger.WriteStep(7, "Align Stream");
AlignStream(target);
}

// Pack internal

private static void PackInternal(string sourceDir, string targetFile, FileProgress progress,
                                 TraceContext ctx)
{
PathHelper.ChangeExtension(ref targetFile, ".xpr");

using var xprStream = FileManager.OpenWrite(targetFile);
Compress(sourceDir, xprStream, progress);

ctx.OutputSize = xprStream.Length;
ctx.LogOutSize = true;
}

// Pack Dir as single stream

public static void Pack(string sourceDir, string targetFile,
                        FileProgress progress = null)
{

TraceExecutor.Run("XPR Build", 
                  ctx => PackInternal(sourceDir, targetFile, progress, ctx),
                  ("SourceDir", sourceDir),
                  ("TargetFile", targetFile)
);

}

}

}