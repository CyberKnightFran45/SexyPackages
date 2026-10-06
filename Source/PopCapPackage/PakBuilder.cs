using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using BlossomLib.Modules.Compression;

namespace SexyPackages.PopCapPackage
{
/// <summary> Packs the Content of a Directory as a PopCap Package (PAK). </summary>

public static class PakBuilder
{
// Sanitize Path

private static void SanitizePath(ref string target, PakPlatform platform)
{
char separator = platform == PakPlatform.Windows ? '\\' : '/';

using NativeString pathBuilder = new(target.Length);
var span = pathBuilder.AsSpan();

for(int i = 0; i < target.Length; i++)
{
char c = target[i];

span[i] = (c == '\\' || c == '/') ? separator : c;
}

ReadOnlySpan<char> sanitized = span;

if(sanitized.Length >= 2 && sanitized[0] == '.' && (sanitized[1] == '\\' || sanitized[1] == '/') )
sanitized = sanitized[2..];

target = new(sanitized);
}

// Load config

private static PakConfig LoadConfig(string baseDir)
{
TraceLogger.WriteActionStart("Loading config...");

string infoPath = Path.Combine(baseDir, PakConstants.SRC_CONFIG);

using var cfgStream = FileManager.OpenRead(infoPath);
var cfg = JsonSerializer.DeserializeObject<PakConfig>(cfgStream, PakConfig.Context);

TraceLogger.WriteActionEnd();

return cfg;
}

// Get files

private static List<string> GetFiles(string baseDir, PakPlatform platform, out List<string> resNames)
{
resNames = new();

TraceLogger.WriteActionStart("Obtaining files...");

string resDir = Path.Combine(baseDir, PakConstants.SRC_RESOURCES);
var files = Directory.EnumerateFiles(resDir, "*.*", SearchOption.AllDirectories);

List<string> res = new();

foreach(string path in files)
{
string resName = Path.GetRelativePath(resDir, path);
SanitizePath(ref resName, platform);

res.Add(path);
resNames.Add(resName);
}

TraceLogger.WriteActionEnd();

return res;
}

// Write Header

private static void WriteHeader(Stream writer)
{
TraceLogger.WriteActionStart("Writing header...");

writer.WriteUInt32(PakConstants.MAGIC);
writer.WriteUInt32(PakConstants.VERSION);

TraceLogger.WriteActionEnd();
}

// Add Res to Buffer

private static void AddRes(string path,
                           ReadOnlySpan<char> name,
                           bool useZlib,
                           CompressionLevel level,
                           ChunkedMemoryStream parent, 
                           MemoryStream entries)
{
using var resStream = FileManager.OpenRead(path);

entries.WriteByte(0x00);
entries.WriteStringByLen8(name, PakConstants.ENCODING);

var rawSize = (uint)resStream.Length;
entries.WriteUInt32(rawSize);

if(useZlib)
{
long start = parent.Position;
ZLibCompressor.CompressStream(resStream, parent, level);

var sizeCompressed = (uint)(parent.Position - start);
entries.WriteUInt32(sizeCompressed);
}

else
FileManager.Process(resStream, parent);

long fileTime = DateTime.Now.ToFileTimeUtc();
entries.WriteInt64(fileTime);
}

// Add files

private static void AddFiles(List<string> files,
                             List<string> names,
                             bool alignData,
                             bool useZlib,
                             CompressionLevel compressionLvl,
                             ChunkedMemoryStream parent,
                             MemoryStream entries,
                             FileProgress progress)
{
TraceLogger.WriteActionStart("Adding files...");

int fileCount = files.Count;

for(int i = 0; i < fileCount; i++)
{
string file = files[i];
string name = names[i];

progress?.Invoke(name, i + 1, fileCount);

if(alignData)
{
bool isPtx = name.EndsWith(".ptx", StringComparison.OrdinalIgnoreCase);

PakHelper.AlignStream(parent, isPtx);
}

AddRes(file, name, useZlib, compressionLvl, parent, entries);
}

entries.WriteByte(PakConstants.ENTRIES_END);

TraceLogger.WriteActionEnd();
}

// Write PAK Entries

private static void WriteEntries(Stream writer, MemoryStream entries)
{
TraceLogger.WriteActionStart("Writing entries...");

entries.Seek(0, SeekOrigin.Begin);
FileManager.Process(entries, writer);

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

// Pack content

private static void PackContent(string sourceDir,
                                Stream target,
                                PakPlatform platform,
                                bool useZlib,
                                CompressionLevel compressionLvl,
                                FileProgress progress)
{
TraceLogger.WriteStep(2, "List Files");

var files = GetFiles(sourceDir, platform, out var resNames);
int fileCount = files.Count;

if(fileCount == 0)
{
string dirName = PathHelper.ShortPath(sourceDir);
TraceLogger.WriteWarn($"'{dirName}' has no files.");

return;
}

TraceLogger.WriteInfo($"Found: {fileCount} files");

TraceLogger.WriteStep(3, "Write Metadata");
WriteHeader(target);

TraceLogger.WriteStep(4, "Add Files");

using ChunkedMemoryStream resBlob = new();
using MemoryStream entries = new();

bool alignData = platform == PakPlatform.Xbox360;

AddFiles(files, resNames, alignData, useZlib, compressionLvl, resBlob, entries, progress);

TraceLogger.WriteStep(5, "Write Resource Entries");
WriteEntries(target, entries);

TraceLogger.WriteStep(6, "Pack Resources");
WriteRes(target, resBlob);
}

// Pack and encrypt content

private static void PackAndEncryptContent(string sourceDir,
                                          Stream target,
                                          PakPlatform platform,
                                          bool useZlib,
                                          CompressionLevel compressionLvl,
                                          FileProgress progress)
{
using XorStream encryptor = new(target, PakConstants.KEY);

PackContent(sourceDir, encryptor, platform, useZlib, compressionLvl, progress);
}

// Compress Stream

public static void Compress(string sourceDir, Stream target, FileProgress progress = null)
{
TraceLogger.WriteStep(1, "Load PAK Config");

var cfg = LoadConfig(sourceDir);

bool useZlib = cfg.UseZlib;
var compressionLvl = cfg.CompressionLvl ?? default;

var platform = cfg.Platform;

switch(platform)
{
case PakPlatform.XMEM:
TraceLogger.WriteWarn("XMEM is not supported, use xbcompress instead.");
break;

case PakPlatform.AndroidTV:
ZipCompressor.CompressStream(sourceDir, target, default);
break;

case PakPlatform.Windows:
PackAndEncryptContent(sourceDir, target, platform, useZlib, compressionLvl, progress);
break;

default:
PackContent(sourceDir, target, platform, useZlib, compressionLvl, progress);
break;
}

}

// Pack internal

private static void PackInternal(string sourceDir, string targetFile, FileProgress progress,
                                 TraceContext ctx)
{
PathHelper.ChangeExtension(ref targetFile, ".pak");

using var pakStream = FileManager.OpenWrite(targetFile);
Compress(sourceDir, pakStream, progress);

ctx.OutputSize = pakStream.Length;
ctx.LogOutSize = true;
}

// Pack Dir as single stream

public static void Pack(string sourceDir, string targetFile, FileProgress progress = null)
{

TraceExecutor.Run("PAK Build", 
                  ctx => PackInternal(sourceDir, targetFile, progress, ctx),
                  ("SourceDir", sourceDir),
                  ("TargetFile", targetFile)
);

}

}

}