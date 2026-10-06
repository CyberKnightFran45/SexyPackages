using static SexyPackages.ResourceStreamBundle.RsbUtils;

using System;
using System.IO;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RsbContentExtractor: extract ManifestGroup </summary>

public static partial class RsbContentExtractor
{
// Check if group is a __MANIFESTGROUP__

private static bool IsManifestGroup(string groupName)
{
return groupName.StartsWith("__MANIFESTGROUP", StringComparison.OrdinalIgnoreCase);
}

// Uses the RSB's embedded Manifest when available

// Otherwise, locates every __MANIFESTGROUP* and decodes its RTON/NEWTON resources

private static void ExtractManifestGroup(Stream source,
                                         RsbUnpackerContext ctx,
                                         string outputDir,
                                         string parentPath,
                                         bool useExternalRsgs)
{
TraceLogger.WriteActionStart("Extracting ManifestGroup...");
TraceLogger.WriteLine();

string manifestDir = Path.Combine(outputDir, "ManifestGroup");
var manifest = ctx.Manifest;

bool hasManifest;

if(manifest != null)
{
hasManifest = true;

RsbMetadataSaver.SaveManifestInfo(manifestDir, manifest);
}

else
{

hasManifest = ExtractGroupResidents(source,
                                    ctx,
									manifestDir,
									parentPath,
									useExternalRsgs,
                                    IsManifestGroup,
									MANIFEST_EXTS,
                                    out _,
                                    out _,
                                    pathResolver: BuildFlatFilePath);

}

if(!hasManifest)
TraceLogger.WriteWarn("Missing ManifestInfo. This RSB might be corrupted.");

TraceLogger.WriteActionEnd();
}

}

}