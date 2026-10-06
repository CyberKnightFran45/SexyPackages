using System;
using System.Collections.Generic;
using System.IO;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB GroupsInfo cache </summary>

public static class RsbEntriesCache
{
// Entry for GroupsInfo cache

private readonly record struct GroupsInfoCacheEntry(string Path, DateTime LastWriteTime);

// GroupsInfo cache

private static readonly Dictionary<GroupsInfoCacheEntry, RsbEntries> Cached = new();

// Get CacheKey

private static GroupsInfoCacheEntry GetCacheKey(string path)
{
var lastWriteTime = File.GetLastWriteTimeUtc(path);

return new(path, lastWriteTime);
}

// Try Add Entry

public static bool TryAdd(string path, RsbEntries entries)
{
var k = GetCacheKey(path);

return Cached.TryAdd(k, entries);
}

// Try Get Entry

public static bool TryGet(string path, out RsbEntries entries)
{
var k = GetCacheKey(path);

return Cached.TryGetValue(k, out entries);
}

}

}