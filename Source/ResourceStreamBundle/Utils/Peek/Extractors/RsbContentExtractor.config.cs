using static SexyPackages.ResourceStreamBundle.RsbUtils;

using System;
using System.IO;
using SexyPackages.ResourceStreamGroup;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RsbContentExtractor: extract config </summary>

public static partial class RsbContentExtractor
{
// Detect whether RTONs came out encrypted

private static bool? DetectRtonEncryption(CodecInfo codecInfo)
{

foreach(var kvp in codecInfo)
{
string fileExt = Path.GetExtension(kvp.Key);
bool isRton = string.Equals(fileExt, ".RTON", StringComparison.OrdinalIgnoreCase);

if(!isRton)
continue;

if(kvp.Value.TryGet<bool>(RtonCodec.CTX_KEY_USE_ENCRYPTION, out var wasEncrypted) )
return wasEncrypted;

}

return null;
}

// Locates config groups and decodes all their content

private static bool? ExtractGameConfig(Stream source,
                                       RsbUnpackerContext ctx,
                                       string outputDir,
                                       string parentPath,
                                       bool useExternalRsgs)
{
TraceLogger.WriteActionStart("Extracting Game Config...");
TraceLogger.WriteLine();

string configDir = Path.Combine(outputDir, "Config");

bool? encryptPackages = null;

int totalGroups = 0;
int cfgSources = 0;

// Locate Package groups and decode their RTONs

bool hasPackages = ExtractGroupResidents(source,
                                         ctx,
                                         configDir,
                                         parentPath,
                                         useExternalRsgs,
                                         IsPackages,
                                         PACKAGE_EXTS,
                                         out var packageCodecInfo,
                                         out int packageGroups,
                                         out int totalRtons);

if(hasPackages)
{
encryptPackages = DetectRtonEncryption(packageCodecInfo);

totalGroups += packageGroups;
cfgSources += totalRtons;
}

// Extract .ini files from 'CREDITS_COMMON' (Chinese Version)

bool hasIniFiles = ExtractGroupResidents(source,
                                         ctx,
                                         configDir,
                                         parentPath,
                                         useExternalRsgs,
                                         "CREDITS_COMMON",
                                         INI_EXTS,
                                         out int _,
                                         out int iniSources);

if(hasIniFiles)
{
totalGroups++;

cfgSources += iniSources;
}

// Extraction stats

if(hasPackages || hasIniFiles)
TraceLogger.WriteInfo($"Matching groups: {totalGroups} | Config sources: {cfgSources}");

else
TraceLogger.WriteWarn("No config ResGroup was found in this RSB.");

TraceLogger.WriteActionEnd();

return encryptPackages;
}

}

}