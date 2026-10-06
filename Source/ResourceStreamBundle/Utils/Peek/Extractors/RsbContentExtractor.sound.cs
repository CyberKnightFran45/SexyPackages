using static SexyPackages.ResourceStreamBundle.RsbUtils;

using System.IO;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RsbContentExtractor: extract sounds </summary>

public static partial class RsbContentExtractor
{
// Extracts .bnk/.wav sounds

private static void ExtractSounds(Stream source,
                                  RsbUnpackerContext ctx,
                                  string outputDir,
                                  string parentPath,
                                  bool useExternalRsgs)
{
TraceLogger.WriteActionStart("Extracting Sounds...");
TraceLogger.WriteLine();

string soundDir = Path.Combine(outputDir, "Sounds");

bool hasSound = ExtractGroupResidents(source,
                                      ctx,
									  soundDir,
									  parentPath,
									  useExternalRsgs,
                                      MatchAllGroups,
									  SOUND_EXTS,
									  out int matchingGroups,
									  out int soundFiles);

if(hasSound)
TraceLogger.WriteInfo($"Matching groups: {matchingGroups} | Sound files: {soundFiles}");

else
TraceLogger.WriteWarn("No sound resource was found in this RSB.");

TraceLogger.WriteActionEnd();
}

}

}