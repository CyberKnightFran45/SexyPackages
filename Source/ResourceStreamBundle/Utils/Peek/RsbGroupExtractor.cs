using static SexyPackages.ResourceStreamBundle.RsbUtils;

using System;
using System.IO;
using SexyPackages.ResourceStreamGroup;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RsbGroupExtractor: extract RSG by name </summary>

public static partial class RsbContentExtractor
{
// RAM Disk options

private static readonly RAMDiskOptions DiskOptions = new()
{
Enabled = false
};

// Check if group name matches

private static bool GroupNameMatch(string groupName, string expectedName)
{
return groupName.Equals(expectedName, StringComparison.OrdinalIgnoreCase);
}

// Locate and extract RSG by name

private static void ExtractGroupByName(Stream source,
                                       RsbUnpackerContext ctx,
                                       string outputDir,
                                       string parentPath,
                                       bool useExternalRsgs,
                                       string groupName,
                                       RsbPlatform platform)
{

if(string.IsNullOrWhiteSpace(groupName) )
throw new ArgumentException("Group name cannot be empty.", nameof(groupName) );

TraceLogger.WriteActionStart($"Extracting Group: {groupName} ...");

bool rsgFilter(int i, in RsbGroupDescriptor desc, string currName) => GroupNameMatch(currName, groupName);
bool found = false;

ForEachGroup(source,
             ctx,
             parentPath,
             useExternalRsgs,

(i, in desc, currentGroupName, buffer) =>
{

if(!GroupNameMatch(currentGroupName, groupName) )
return;

var expected = BuildGroupInfo(buffer, ctx, desc);

string groupOutDir = Path.Combine(outputDir, groupName);
Directory.CreateDirectory(groupOutDir);

RsgLegacyUnpacker.Unpack(buffer,
                         groupOutDir,
                         platform,
                         expected,
                         groupName,
                         silentLog: true,
                         options: DiskOptions);

found = true;
},

useParalellism: false, // Single group: paralellism is not needed here
groupPredicate: rsgFilter
);

if(!found)
TraceLogger.WriteWarn($"Group '{groupName}' was not found in this RSB.");

TraceLogger.WriteActionEnd();
}

// Extract Group from RSB Stream

public static void ExtractGroup(Stream source, 
                                string groupName,
								string outputDir,
								RsbPlatform platform,
                                string parentPath = null)
{

void body(Stream stream, RsbCompressionFlags? compressionFlags)
{
int step = 1;
var ctx = InitExtractorContext(stream, ref step, out bool useExternalRsgs);

ExtractGroupByName(stream,
                   ctx,
				   outputDir,
				   parentPath,
                   useExternalRsgs,
                   groupName,
                   platform);

}

RsbUnpacker.UnwrapAndRun(source, body);
}

// Extract RSG (Internal)

private static void ExtractGroupInternal(string inputFile,
                                         string groupName,
                                         string outputDir,
                                         RsbPlatform platform)
{
string parentPath = Path.GetDirectoryName(inputFile);
using var rsbStream = FileManager.OpenRead(inputFile);

ExtractGroup(rsbStream, groupName, outputDir, platform, parentPath);
}

// Extract all resources belonging to a named RSG

public static void ExtractGroup(string inputFile,
                                string outputDir,
                                string groupName,
                                RsbPlatform platform)
{

TraceExecutor.Run("RSB Group Extraction",
                  ctx => ExtractGroupInternal(inputFile, groupName, outputDir, platform),				  
                  ("InputFile", inputFile),
                  ("OutputDir", outputDir),
                  ("GroupName", groupName),
                  ("Platform", platform)

);

}

}

}