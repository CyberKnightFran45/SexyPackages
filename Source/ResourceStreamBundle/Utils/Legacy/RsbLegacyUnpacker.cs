using System.IO;
using SexyPackages.ResourceStreamGroup;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB Legacy Unpacker: init project </summary>

public static class RsbLegacyUnpacker
{
// Detects if packages should be encrypted

private static bool ShouldEncryptPackages(string groupName, bool? encryptPackages, bool? encryptRtons)
{
return encryptPackages == null && RsbUtils.IsPackages(groupName) && encryptRtons.HasValue;
}

// Extract and decode all resources from a single RSG

private static void ExtractAndDecodeGroup(in RsbGroupDescriptor desc,
                                          string groupName,
									      NativeBuffer buffer,
                                          RsbUnpackerContext ctx,
                                          RsbPlatform platform,
									      GroupMap groupsMap,
									      string groupsDir,
                                          RAMDiskOptions options,
                                          ref bool? encryptPackages)
{
var expected = RsbUtils.BuildGroupInfo(buffer, ctx, desc);

groupsMap.Add(groupName, expected);

string groupOutDir = Path.Combine(groupsDir, groupName);

var encryptRtons = RsgLegacyUnpacker.Unpack(buffer, groupOutDir, platform, expected, 
                                            groupName, true, options);

bool shouldEncryptPackages = ShouldEncryptPackages(groupName, encryptPackages, encryptRtons);

if(shouldEncryptPackages)
encryptPackages = encryptRtons;

}

// Full decomposition: extract and decode all resources

private static GroupMap ExtractResources(Stream source,
                                         RsbUnpackerContext ctx,
										 string outputDir,
                                         string parentPath,
										 bool useExternalRsgs,
                                         RsbPlatform platform,
                                         RAMDiskOptions options,
                                         out bool? encryptPackages)
{
TraceLogger.WriteActionStart("Extracting Resources...");

string groupsDir = Path.Combine(outputDir, RsbConstants.SRC_GROUPS_RESOURCES);

GroupMap groupsMap = new();
bool? shouldEncryptPackages = null;

RsbUtils.ForEachGroup(source, ctx, parentPath, useExternalRsgs, 

(i, in desc, groupName, buffer) =>
{

ExtractAndDecodeGroup(desc, groupName, buffer, ctx, platform, groupsMap, groupsDir, options,
                      ref shouldEncryptPackages);
					  
}

);

encryptPackages = shouldEncryptPackages;

TraceLogger.WriteActionEnd();

return groupsMap;
}

// Extract content

private static void ExtractContent(Stream stream,
                                   string outputDir,
								   string parentPath,
								   bool? addSmfExt,
								   RsbPlatform platform,
								   RAMDiskOptions options,
								   RsbCompressionFlags? compressionFlags)
{
int step = 1; 

var ctx = RsbReader.InitCtx(stream, ref step); 

var rsbInfo = ctx.BundleInfo;
bool useExternalRsgs = stream.Length == rsbInfo.SectionLength;

var groupsMap = ExtractResources(stream, ctx, outputDir, parentPath, useExternalRsgs,
                                 platform, options, out var encryptPackages); 


step++; 

TraceLogger.WriteStep( step, "Save Metadata"); 

RsbUtils.SaveMetadata(outputDir, ctx, groupsMap, useExternalRsgs, compressionFlags,
                      addSmfExt, encryptPackages, platform);

}


/// <summary> Unpacks an RSB from an already opened stream </summary>

internal static void Unpack(Stream source,
                            string outputDir,
                            RsbPlatform platform,
                            string parentPath = null,
                            bool? addSmfExt = null,
                            RAMDiskOptions options = null)
{

void body(Stream stream, RsbCompressionFlags? compressionFlags)
{
ExtractContent(stream, outputDir, parentPath, addSmfExt, platform, options, compressionFlags);
}

RsbUnpacker.UnwrapAndRun(source, body);
}

// Unpack internal

private static void UnpackInternal(string inputFile, string outputDir, RsbPlatform platform,
                                   RAMDiskOptions options)
{
var hasSmfExt = RsbUnpacker.IsSmf(inputFile);
string parentPath = Path.GetDirectoryName(inputFile);

using var rsbStream = FileManager.OpenRead(inputFile);

Unpack(rsbStream, outputDir, platform, parentPath, hasSmfExt, options);
}

/// <summary> Unpacks an RSB file using the legacy full-decomposition format </summary>

public static void Unpack(string inputFile, string outputDir, RsbPlatform platform,
                          RAMDiskOptions options = null)
{

TraceExecutor.Run("RSB Legacy Unpack",
                  ctx => UnpackInternal(inputFile, outputDir, platform, options),
                  ("InputFile", inputFile),
                  ("OutputDir", outputDir),
                  ("Platform", platform)

);

}

}

}