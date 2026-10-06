using static SexyPackages.ResourceStreamBundle.RsbUtils;

using System;
using System.IO;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RsbResExtractor: extract a resource by name </summary>

public static partial class RsbContentExtractor
{
// Check if resource name matches

private static bool ResNameMatch(string resFile, string expectedName)
{
expectedName = expectedName.Replace('/', '\\'); // Normalize to binary convention

if(resFile.Equals(expectedName, StringComparison.OrdinalIgnoreCase) )
return true; // Full/relative path match

string resName = Path.GetFileName(resFile);
string expectedFileName = Path.GetFileName(expectedName);

return resName.Equals(expectedFileName, StringComparison.OrdinalIgnoreCase); // Filename match
}

// Check if res file is a texture by its extension

private static bool IsTexture(string resFile)
{
return resFile.EndsWith(".ptx", StringComparison.OrdinalIgnoreCase);
}

// Attempts to extract a file by its name (either from Resident or Texture blob)

private static bool TryExtractRes(Stream source,
                                  RsbUnpackerContext ctx,
                                  string outputDir,
                                  string parentPath,
                                  bool useExternalRsgs,
                                  string resourceName,
                                  RsbPlatform? platform)
{
bool filter(string res) => ResNameMatch(res, resourceName);

// Try extract texture

bool TryTextures()
{

return ExtractTextures(source,
                       ctx,
                       outputDir,
                       parentPath,
                       useExternalRsgs,
                       GetPtxPlatform(platform),
                       nameFilter: filter,
                       silentLog: true);

}

// Try extract resident

bool TryResidents()
{

return ExtractGroupResidents(source,
                             ctx,
                             outputDir,
                             parentPath,
                             useExternalRsgs,
                             MatchAllGroups,
                             null, // Allow all extensions
                             out _,
                             out _,
                             filter,
                             BuildFlatFilePath,
                             true);

}

if(IsTexture(resourceName) )
return TryTextures() || TryResidents();

return TryResidents() || TryTextures();
}

// Locate and extract resource by name

private static void ExtractResourceByName(Stream source,
                                          RsbUnpackerContext ctx,
                                          string outputDir,
                                          string parentPath,
                                          bool useExternalRsgs,
                                          string resName,
                                          RsbPlatform? platform)
{

if(string.IsNullOrWhiteSpace(resName) )
throw new ArgumentException("Resource name cannot be empty.", nameof(resName) );

TraceLogger.WriteActionStart($"Extracting Resource: {resName} ...");

bool found = TryExtractRes(source,
                           ctx,
						   outputDir,
						   parentPath,
						   useExternalRsgs,
						   resName,
						   platform);

if(!found)
TraceLogger.WriteWarn($"Resource '{resName}' was not found in this RSB.");

TraceLogger.WriteActionEnd();
}

// Extract Res from RSB Stream

public static void ExtractResource(Stream source, 
                                   string resName,
								   string outputDir,
                                   string parentPath = null,
								   RsbPlatform? platform = null)
{

void body(Stream stream, RsbCompressionFlags? compressionFlags)
{
int step = 1;
var ctx = InitExtractorContext(stream, ref step, out bool useExternalRsgs);

ExtractResourceByName(stream,
                      ctx,
					  outputDir,
					  parentPath,
                      useExternalRsgs,
                      resName,
                      platform);

}

RsbUnpacker.UnwrapAndRun(source, body);
}

// Extract Res (internal)

private static void ExtractResInternal(string inputFile,
                                       string resName,
                                       string outputDir,
                                       RsbPlatform? platform)
{
string parentPath = Path.GetDirectoryName(inputFile);
using var rsbStream = FileManager.OpenRead(inputFile);

ExtractResource(rsbStream, resName, outputDir, parentPath, platform);
}

// Extract a named resource from a RSB file.

public static void ExtractResource(string inputFile,
                                   string outputDir,
                                   string resName,
                                   RsbPlatform? platform = null)
{

TraceExecutor.Run("RSB Resource Extraction",
                  ctx => ExtractResInternal(inputFile, resName, outputDir, platform),
                  ("InputFile", inputFile),
                  ("OutputDir", outputDir),
                  ("ResName", resName),
                  ("Platform", platform)

);

}

}

}