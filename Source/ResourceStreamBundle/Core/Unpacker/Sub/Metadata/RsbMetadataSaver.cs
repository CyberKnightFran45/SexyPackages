using System.IO;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Save RSB metadata </summary>

internal static class RsbMetadataSaver
{
// Save manifest info

public static void SaveManifestInfo(string outputDir, RsbManifest manifest)
{

if(manifest is null)
return;

TraceLogger.WriteActionStart("Saving Manifest Info...");

string manifestPath = Path.Combine(outputDir, RsbConstants.SRC_MANIFEST);
using var manifestStream = FileManager.OpenWrite(manifestPath);

JsonSerializer.SerializeObject(manifest, manifestStream);

TraceLogger.WriteActionEnd();
}

// Save pool info

public static void SavePoolInfo(string outputDir, RsbPoolDescriptor[] poolInfo)
{
TraceLogger.WriteActionStart("Saving Pool Info...");

var jsonPool = RsbMetadataJsonBuilder.BuildPoolInfo(poolInfo);

string path = Path.Combine(outputDir, RsbConstants.SRC_POOLS);
using var stream = FileManager.OpenWrite(path);

JsonSerializer.SerializeObject(jsonPool, stream);

TraceLogger.WriteActionEnd();
}

// Build composite info

private static CompositeMap BuildCompositeMap(RsbCompositeData composite,
                                              string[] compositeIDs,
                                              string[] groupIDs)
{

if(composite.IsV3)
return RsbMetadataJsonBuilder.BuildCompositeInfoV3(compositeIDs, composite.InfoV3, groupIDs);

return RsbMetadataJsonBuilder.BuildCompositeInfo(compositeIDs, composite.Info, groupIDs);
}

// Save composite info

public static void SaveCompositeInfo(string outputDir, 
                                     string[] compositeIDs,
									 string[] groupIDs,
                                     RsbCompositeData composite)
{
TraceLogger.WriteActionStart("Saving Composite Info...");

var compositesMap = BuildCompositeMap(composite, compositeIDs, groupIDs);

string path = Path.Combine(outputDir, RsbConstants.SRC_COMPOSITE);
using var stream = FileManager.OpenWrite(path);

JsonSerializer.SerializeObject(compositesMap, stream);

TraceLogger.WriteActionEnd();
}

// Save Groups Info

public static void SaveGroupsInfo(string outputDir, GroupMap groupInfo, RsbPlatform? platform)
{

if(groupInfo is null)
return;

TraceLogger.WriteActionStart("Saving Groups Info...");

RsbEntries entries = new(groupInfo, platform);
string path = Path.Combine(outputDir, RsbConstants.SRC_GROUPS_INFO);

using var stream = FileManager.OpenWrite(path);
JsonSerializer.SerializeObject(entries, stream, RsbEntries.Context);

RsbEntriesCache.TryAdd(path, entries);

TraceLogger.WriteActionEnd();
}

// Save rsb config

public static void SaveConfig(string outputDir, RsbParams cfg)
{
TraceLogger.WriteActionStart("Saving RSB Config...");

string infoPath = Path.Combine(outputDir, RsbConstants.SRC_CONFIG);

using var cfgStream = FileManager.OpenWrite(infoPath);
JsonSerializer.SerializeObject(cfg, cfgStream, RsbParams.Context);

TraceLogger.WriteActionEnd();
}

}

}