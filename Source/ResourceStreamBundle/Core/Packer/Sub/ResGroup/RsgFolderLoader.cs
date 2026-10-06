using static ParallelTables;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SexyPackages.ResourceStreamGroup;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSG Folder loader </summary>

internal static class RsgFolderLoader
{
// Load a RSG already sitting in memory

private static LoadedRsg LoadFromBuffer(string groupId, NativeBuffer buffer)
{
var info = RsgUnpacker.ReadInfo(buffer, out var endian);
var resMap = RsgUnpacker.LoadResMap(buffer, info.ResMapOffset, info.ResMapLength, endian);

return new(groupId, buffer, info, endian, resMap);
}

// Load single RSG (disk based)

private static LoadedRsg LoadSingle(string filePath)
{
string groupId = Path.GetFileNameWithoutExtension(filePath);

using var fileStream = FileManager.OpenRead(filePath);
NativeBuffer buffer = fileStream.ReadPtr();

return LoadFromBuffer(groupId, buffer);
}

// Load all RSGs (sorted from A to Z)

public static List<LoadedRsg> LoadAll(string sourceDir, ReadonlyBufferMap overrides = null)
{
TraceLogger.WriteActionStart("Loading ResGroups...");

Dictionary<string, LoadedRsg> loaded = new(StringComparer.OrdinalIgnoreCase);

string groupsDir = Path.Combine(sourceDir, RsbConstants.SRC_GROUPS);

if(Directory.Exists(groupsDir) )
{
List<string> files = [.. Directory.EnumerateFiles(groupsDir, "*.rsg") ];
files.Sort(StringComparer.OrdinalIgnoreCase);

var results = new LoadedRsg[files.Count];

Parallel.For(0, files.Count, MultiThreadOptions, i =>
{
results[i] = LoadSingle(files[i] );
}

);

foreach(var rsg in results)
loaded[rsg.GroupName] = rsg;

}

if(overrides != null)
{

foreach(var kvp in overrides)
loaded[kvp.Key] = LoadFromBuffer(kvp.Key, kvp.Value);

}

List<LoadedRsg> sorted = [.. loaded.Values];
sorted.Sort( (a, b) => string.Compare(a.GroupName, b.GroupName, StringComparison.OrdinalIgnoreCase) );

TraceLogger.WriteActionEnd();

TraceLogger.WriteInfo($"Loaded: {sorted.Count} RSGs");

return sorted;
}

}

}