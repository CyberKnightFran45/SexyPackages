using System.IO;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Load RSB metadata </summary>

public static class RsbMetadataLoader
{
// Load RSB params

internal static RsbParams LoadParams(string sourceDir)
{
TraceLogger.WriteActionStart("Loading config...");

string path = Path.Combine(sourceDir, RsbConstants.SRC_CONFIG);
using var stream = FileManager.OpenRead(path);

var cfg = JsonSerializer.DeserializeObject<RsbParams>(stream, RsbParams.Context);

TraceLogger.WriteActionEnd();

return cfg;
}

// Load Pools Info
 
internal static RsbPoolInfo[] LoadPoolsInfo(string sourceDir)
{
string path = Path.Combine(sourceDir, RsbConstants.SRC_POOLS);
 
if(!File.Exists(path) )
return [];

TraceLogger.WriteActionStart("Loading Pool Info...");
 
using var stream = FileManager.OpenRead(path);
var pools = JsonSerializer.DeserializeObject<RsbPoolInfo[]>(stream);
 
TraceLogger.WriteActionEnd();
 
return pools ?? [];
}

// Load Composite Info

internal static CompositeMap LoadCompositeInfo(string sourceDir)
{
TraceLogger.WriteActionStart("Loading Composite Info...");

string path = Path.Combine(sourceDir, RsbConstants.SRC_COMPOSITE);
using var stream = FileManager.OpenRead(path);

var compositeMap = JsonSerializer.DeserializeObject<CompositeMap>(stream, RsbCompositeInfo.Context);

TraceLogger.WriteActionEnd();

return compositeMap;
}

// Load Manifest Info (optional)

internal static RsbManifest LoadManifestInfo(string sourceDir)
{
string path = Path.Combine(sourceDir, RsbConstants.SRC_MANIFEST);

if(!File.Exists(path) )
return null;

TraceLogger.WriteActionStart("Loading Manifest Info...");

using var stream = FileManager.OpenRead(path);
var manifest = JsonSerializer.DeserializeObject<RsbManifest>(stream);

TraceLogger.WriteActionEnd();

return manifest;
}

// Load Groups Info (Core logic)

public static RsbEntries LoadGroupsInfoCore(string infoPath)
{
TraceLogger.WriteActionStart("Loading Groups Info...");

RsbEntries entries;

if(RsbEntriesCache.TryGet(infoPath, out var cached) )
entries = cached;

else
{
using var stream = FileManager.OpenRead(infoPath);
entries = JsonSerializer.DeserializeObject<RsbEntries>(stream, RsbEntries.Context);

RsbEntriesCache.TryAdd(infoPath, entries);
}

TraceLogger.WriteActionEnd();

return entries;
}

// Load Groups Info

internal static RsbEntries LoadGroupsInfo(string sourceDir)
{
string path = Path.Combine(sourceDir, "Metadata", RsbConstants.SRC_GROUPS_INFO);

return LoadGroupsInfoCore(path);
}

}

}