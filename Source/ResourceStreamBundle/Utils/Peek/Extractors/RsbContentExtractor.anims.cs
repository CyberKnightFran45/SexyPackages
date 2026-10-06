using static SexyPackages.ResourceStreamBundle.RsbUtils;

using System.IO;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RsbContentExtractor: extract animations </summary>

public static partial class RsbContentExtractor
{
// Extracts .pam animations

private static void ExtractAnimations(Stream source,
                                      RsbUnpackerContext ctx,
                                      string outputDir,
                                      string parentPath,
                                      bool useExternalRsgs)
{
TraceLogger.WriteActionStart("Extracting Animations...");
TraceLogger.WriteLine();

string animDir = Path.Combine(outputDir, "Animations");

bool hasAnim = ExtractGroupResidents(source,
                                     ctx,
                                     animDir,
                                     parentPath,
                                     useExternalRsgs,
                                     MatchAllGroups,
                                     ANIM_EXTS,
                                     out int matchingGroups,
                                     out int totalAnims);

if(hasAnim)
TraceLogger.WriteInfo($"Matching groups: {matchingGroups} | Anim files: {totalAnims}");

else
TraceLogger.WriteWarn("No anim resource was found in this RSB.");

TraceLogger.WriteActionEnd();
}

}

}