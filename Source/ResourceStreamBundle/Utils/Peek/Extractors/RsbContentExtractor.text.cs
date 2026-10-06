using static SexyPackages.ResourceStreamBundle.RsbUtils;

using System.IO;
using System;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RsbContentExtractor: extract text </summary>

public static partial class RsbContentExtractor
{
// Check if group contains text files

private static bool IsTextGroup(string groupName) => TEXT_GROUPS.Contains(groupName);

// Check if group is PACKAGES (LawnStrings are stored there in modern versions of PvZ2)

private static bool IsPackagesMain(string groupName)
{
return string.Equals(groupName, "PACKAGES", StringComparison.OrdinalIgnoreCase);
}

// Check if res is LawnStrings file

private static bool IsLawnStrings(string resFile)
{
string resName = Path.GetFileNameWithoutExtension(resFile);

return resName.StartsWith("LawnStrings", StringComparison.OrdinalIgnoreCase);
}

// Locate and extract text files

private static void ExtractText(Stream source,
                                RsbUnpackerContext ctx,
                                string outputDir,
                                string parentPath,
                                bool useExternalRsgs)
{
TraceLogger.WriteActionStart("Extracting Text...");
TraceLogger.WriteLine();

string textDir = Path.Combine(outputDir, "Text");

int totalGroups = 0;
int txtFiles = 0;

// First check if LawnStrings is in 'PACKAGES' group (International Version)

bool hasTextInPackages = ExtractGroupResidents(source,
                                               ctx,
									           textDir,
									           parentPath,
									           useExternalRsgs,
									           IsPackagesMain,
									           PACKAGE_EXTS,
									           out int _,
									           out int txtFilesPackages,
									           IsLawnStrings);

if(hasTextInPackages)
{
totalGroups++;

txtFiles += txtFilesPackages;
}

// Check if text is present elsewhere

bool hasTextElsewhere = ExtractGroupResidents(source,
                                              ctx,
									          textDir,
									          parentPath,
									          useExternalRsgs,
									          IsTextGroup,
									          TXT_EXTS,
									          out int matchingGroups,
									          out int otherTxts);

if(hasTextElsewhere)
{
totalGroups += matchingGroups;

txtFiles += otherTxts;
}


if(hasTextInPackages || hasTextElsewhere)
TraceLogger.WriteInfo($"Matching groups: {totalGroups} | Text files: {txtFiles}");

else
TraceLogger.WriteWarn("No text ResGroup was found in this RSB.");

TraceLogger.WriteActionEnd();
}

}

}