using System;
using System.IO;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB Extractor: selective resources extraction </summary>

public static partial class RsbContentExtractor
{
// RSB Extraction Task

private readonly record struct RsbExtractionTask(RsbResType Flag, 
                                                 string Description,
                                                 Action Execute,
                                                 bool SilentLog = false);

// Gets the RSB Extraction tasks associated to a given file

private static RsbExtractionTask[] GetExtractionTasks(Stream source,
                                                      RsbUnpackerContext ctx, 
                                                      string outputDir, 
                                                      string parentPath, 
                                                      bool useExternalRsgs,
													  RsbPlatform? platform,
													  RsbExtractionState state)
{
// Define Workloads

RsbExtractionTask getGroupsInfo = new(RsbResType.Info, "Collect Groups Info", 

() => state.GroupsMap = CollectGroupsInfo(source, ctx, parentPath, useExternalRsgs),
true

);

RsbExtractionTask extractTextures = new(RsbResType.Textures, "Extract Textures", 

() => 
{
var ptxPlatform = RsbUtils.GetPtxPlatform(platform);

ExtractTextures(source, ctx, outputDir, parentPath, useExternalRsgs, ptxPlatform);
}

);

RsbExtractionTask extractText = new(RsbResType.Text,  "Extract Text", 

() => ExtractText(source, ctx, outputDir, parentPath, useExternalRsgs)

);

RsbExtractionTask extractCfg = new(RsbResType.GameConfig, "Extract Game Config", 

() => state.EncryptPackages = ExtractGameConfig(source, ctx, outputDir, parentPath, useExternalRsgs)

);

RsbExtractionTask extractManifestGroup = new(RsbResType.ManifestGroup,  "Extract ManifestGroup", 

() => ExtractManifestGroup(source, ctx, outputDir, parentPath, useExternalRsgs)

);

RsbExtractionTask extractAnimations = new(RsbResType.Animations, "Extract Animations", 

() => ExtractAnimations(source, ctx, outputDir, parentPath, useExternalRsgs)

);

RsbExtractionTask extractSounds = new(RsbResType.Sounds,  "Extract Sounds",

() => ExtractSounds(source, ctx, outputDir, parentPath, useExternalRsgs)

);

// Populate tasks into Array

return
[
        getGroupsInfo,
        extractTextures,
        extractText,
        extractCfg,
        extractManifestGroup,
        extractAnimations,
        extractSounds
];

}

// Initialize the shared extractor context and RSB storage mode.

private static RsbUnpackerContext InitExtractorContext(Stream source,
                                                       ref int step,
                                                       out bool useExternalRsgs)
{
var ctx = RsbReader.InitCtx(source, ref step);
useExternalRsgs = source.Length == ctx.BundleInfo.SectionLength;

return ctx;
}

// Dispatch specialized extractors

private static void ExtractDispatch(Stream source,
                                    string outputDir,
                                    RsbResType type,
                                    string parentPath,
                                    RsbCompressionFlags? compressionFlags,
                                    bool? addSmfExt,
									RsbPlatform? platform)
{
int step = 1;

var ctx = InitExtractorContext(source, ref step, out bool useExternalRsgs);

RsbExtractionState state = new();

var tasks = GetExtractionTasks(source, ctx, outputDir, parentPath, useExternalRsgs, platform, state);

foreach(var t in tasks)
{

if(type.HasFlag(t.Flag) ) // Filter content by flags
{

if(!t.SilentLog)
{
step++;

TraceLogger.WriteStep(step, t.Description);
}

t.Execute();
}

}

var groupsMap = state.GroupsMap;
var encryptPackages = state.EncryptPackages;

bool persistMetadata = groupsMap != null || encryptPackages.HasValue;

if(persistMetadata)
{
step++;

TraceLogger.WriteStep(step, "Save Metadata");

RsbUtils.SaveMetadata(outputDir, ctx, groupsMap, useExternalRsgs, compressionFlags,
                      addSmfExt, encryptPackages, platform);

}

}

// Extract Content from RSB Stream

public static void ExtractContent(Stream source,
                                  string outputDir,
								  RsbResType type,
                                  RsbPlatform? platform = null,
                                  string parentPath = null,
                                  bool? addSmfExt = null)
{

void body(Stream stream, RsbCompressionFlags? compressionFlags)
{
ExtractDispatch(stream, outputDir, type, parentPath, compressionFlags, addSmfExt, platform);
}

RsbUnpacker.UnwrapAndRun(source, body);
}

// Extract internal

private static void ExtractInternal(string inputFile,
                                    string outputDir,
									RsbResType type,
									RsbPlatform? platform)
{
var hasSmfExt = RsbUnpacker.IsSmf(inputFile);
string parentPath = Path.GetDirectoryName(inputFile);

using var rsbStream = FileManager.OpenRead(inputFile);

ExtractContent(rsbStream, outputDir, type, platform, parentPath, hasSmfExt);
}

/// <summary> Extracts selected content from a RSB </summary>

public static void Extract(string inputFile,
                           string outputDir,
						   RsbResType type,
						   RsbPlatform? platform = null)
{

TraceExecutor.Run("RSB Content Peek",
                  ctx => ExtractInternal(inputFile, outputDir, type, platform),
                  ("InputFile", inputFile),
                  ("OutputDir", outputDir),
                  ("TargetResType", type),
				  ("Platform", platform)
				  
);

}

}

}