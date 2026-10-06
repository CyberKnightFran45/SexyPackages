using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SexyPackages.ResourceStreamBundle;

namespace SexyPackages.ResourceStreamGroup
{
/// <summary> RSG helper methods </summary>

internal static class RsgUtils
{
// Normalize resource name for comparison (JSON uses slash, RSG/RSB table uses backslash)

private static string Normalize(string name) => name.Replace('\\', '/');

// Find group entry and retrieve its info

private static void GetRsgParams(string groupName,
                                 RsbEntries entries, 
                                 out RsbGroupInfo info,
                                 out RsbPlatform platform)
{
entries.TryGetInfo(groupName, out info);

platform = entries.GetPlatform();
}

// Load GroupInfo

internal static void LoadGroupInfo(string groupsInfoPath,
                                   string groupName,
                                   ref int step,
								   out RsbGroupInfo info,
								   out RsbPlatform platform)
{
TraceLogger.WriteStep(step, "Load RSG Info");

var rsbEntries = RsbMetadataLoader.LoadGroupsInfoCore(groupsInfoPath);
GetRsgParams(groupName, rsbEntries, out info, out platform);

step++;
}

// Collect file names from group info (Residents + Textures)

private static HashSet<string> CollectExpectedNames(RsbGroupInfo expected)
{
HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);

if(expected?.ResFiles != null)
{

foreach(var name in expected.ResFiles)
names.Add(Normalize(name) );

}

if(expected?.Textures != null)
{

foreach(var kvp in expected.Textures)
names.Add(Normalize(kvp.Key) );

}

return names;
}

// Checks if a collection of names is declared, either in a RSG ResourceMap or in disk (after extraction)

private static void CheckNamesCore(IEnumerable<string> names,
                                   Func<string, bool> existsCheck,
                                   Action<string> onMissing,
                                   bool silentLog)
{

if(names is null)
return;

int namesCount = 0;
int missingNames = 0;

foreach(string name in names)
{
namesCount++;

if(!existsCheck(name) )
{
missingNames++;

if(missingNames == 1)
TraceLogger.WriteLine(); // Add space on first ocurrence

onMissing(name);
}

}

if(silentLog)
return;

if(missingNames == 0)
TraceLogger.WriteInfo("Resource validation completed: all resources were found.");

else
TraceLogger.WriteInfo($"Missing resources: {missingNames} of {namesCount}");

}

// Get RSG label

private static string GetRsgLabel(string groupName)
{
return string.IsNullOrEmpty(groupName) ? "RSG" : $"RSG - {groupName}";
}

// Warn on missing resource

private static void WarnFileNotDeclared(string label, string fileName, bool isPtx)
{
string fileType = isPtx ? "texture" : "resource";
string normalized = Normalize(fileName);

string msg = $"{label}: {fileType} '{normalized}' exists in the group but is never declared.";

TraceLogger.WriteWarn(msg);
}

// Validate list of names against a collection of expected names

private static void ValidateNamesCore(IEnumerable<string> actual,
                                      HashSet<string> expected,
                                      string label,
                                      bool isPtx, 
                                      bool silentLog)
{
bool existsCheck(string name) => expected.Contains(Normalize(name) );
void onMissing(string name) => WarnFileNotDeclared(label, name, isPtx);

CheckNamesCore(actual, existsCheck, onMissing, silentLog);
}

// Check if list has elements or not

private static bool IsNullOrEmpty(IEnumerable<string> list) => list is null || !list.Any();

// Validate files inside a RSG

private static void ValidateFilesInRSG(IEnumerable<string> fileNames,
                                       HashSet<string> expectedNames,
                                       string label,
									   bool isPtx,
									   string msg,
                                       bool silentLog)
{

if(IsNullOrEmpty(fileNames) )
return;

if(!silentLog)
TraceLogger.WriteActionStart(msg);

ValidateNamesCore(fileNames, expectedNames, label, isPtx, silentLog);

if(!silentLog)
TraceLogger.WriteActionEnd();

}

// Validate residents inside a RSG

private static void ValidateResInRSG(IEnumerable<string> resNames,
                                     HashSet<string> expectedNames,
                                     string label,
                                     bool silentLog)
{
ValidateFilesInRSG(resNames, expectedNames, label, false, "Validating files...", silentLog);
}

// Validate textures inside a RSG

private static void ValidateTexturesInRSG(IEnumerable<string> ptxNames,
                                          HashSet<string> expectedNames,
                                          string label,
                                          bool silentLog)
{
ValidateFilesInRSG(ptxNames, expectedNames, label, true, "Validating textures...", silentLog);
}

// Validate actual ResourceMap against the expected GroupInfo

internal static void ValidateResourcesInRSG(RsbGroupInfo expected,
                                            ResourceMap actual,
									        string groupName,
									        ref int step,
									        bool silentLog)
{

if(expected is null)
return;

step++;

if(!silentLog)
TraceLogger.WriteStep(step, "Validate Resources");

var expectedNames = CollectExpectedNames(expected);
string label = GetRsgLabel(groupName);

ValidateResInRSG(actual.ResidentFiles.Keys, expectedNames, label, silentLog);
ValidateTexturesInRSG(actual.TextureFiles.Keys, expectedNames, label, silentLog);
}

// Check if a resource exists on disk

private static bool ResourceExistsOnDisk(string name, string resourcesDir, HashSet<string> declared)
{
declared.Add(Normalize(name) );

string rawPath = Path.Combine(resourcesDir, name);
bool resExists = File.Exists(rawPath);

if(!resExists)
{
string decodedExt = ResourceCodec.GetDecodedExtension(name);

if(!string.IsNullOrEmpty(decodedExt) )
{
string decodedName = Path.ChangeExtension(name, decodedExt);
string decodedPath = Path.Combine(resourcesDir, decodedName);

resExists = File.Exists(decodedPath);

if(resExists)
declared.Add(Normalize(decodedName) );

}

}

return resExists;
}

// Warn on missing resource

private static void WarnMissingRes(string label, string name)
{
string normalized = Normalize(name);

TraceLogger.WriteWarn($"{label}: resource '{normalized}' is missing.");
}

// Check if a list of declared files exist on disk

private static void CheckExistingFiles(IEnumerable<string> fileNames,
                                       string resourcesDir,
                                       string label,
                                       HashSet<string> declared,
                                       bool silentLog)
{
bool existsCheck(string name) => ResourceExistsOnDisk(name, resourcesDir, declared);
void onMissing(string name) => WarnMissingRes(label, name);

CheckNamesCore(fileNames, existsCheck, onMissing, silentLog);		  
}

// Check files in dir agains expected RSG names

private static void CheckFilesInDir(IEnumerable<string> fileNames,
                                    string resourcesDir,
                                    string label,
									HashSet<string> declared,
									string msg,
                                    bool silentLog)
{

if(IsNullOrEmpty(fileNames) )
return;

if(!silentLog)
TraceLogger.WriteActionStart(msg);

CheckExistingFiles(fileNames, resourcesDir, label, declared, silentLog);

if(!silentLog)
TraceLogger.WriteActionEnd();

}

// Check existing residents inside a folder

private static void CheckExistingRes(IEnumerable<string> resNames,
                                     string resourcesDir,
                                     string label,
									 HashSet<string> declared,
                                     bool silentLog)
{
string msg = "Checking existing res...";

CheckFilesInDir(resNames, resourcesDir, label, declared, msg, silentLog);
}

// Check existing textures inside a folder

private static void CheckExistingTextures(IEnumerable<string> ptxNames,
                                          string resourcesDir,
                                          string label,
										  HashSet<string> declared,
                                          bool silentLog)
{
string msg = "Checking existing textures...";

CheckFilesInDir(ptxNames, resourcesDir, label, declared, msg, silentLog);
}

// Check declared resources (Core)

private static void CheckDeclaredCore(string resourcesDir,
                                      HashSet<string> declared,
                                      string label,
                                      bool silentLog)
{
var files = Directory.EnumerateFiles(resourcesDir, "*.*", SearchOption.AllDirectories);

int notDeclaredFiles = 0;

foreach(string path in files)
{
string relative = Path.GetRelativePath(resourcesDir, path);
string normalized = Normalize(relative);

if(!declared.Contains(normalized) )
{
notDeclaredFiles++;

if(notDeclaredFiles == 1)
TraceLogger.WriteLine(); // Add space on first ocurrence

TraceLogger.WriteWarn($"{label}: file '{normalized}' was not declared in providen JSON.");
}

}

if(!silentLog && notDeclaredFiles == 0)
TraceLogger.WriteInfo("ResourceMap check: all resources are declared in GroupDescriptor.");

else if(!silentLog)
TraceLogger.WriteInfo($"Undeclared resources found: {notDeclaredFiles}");

}

// Validate existing files in disk against a list of declared file names

private static void CheckDeclaredResources(string resourcesDir,
                                           HashSet<string> declared,
                                           string label,
                                           bool silentLog)
{

if(!silentLog)
TraceLogger.WriteActionStart("Validating declared files...");

CheckDeclaredCore(resourcesDir, declared, label, silentLog);

if(!silentLog)
TraceLogger.WriteActionEnd();

}

// Validate resource directory against the expected GroupInfo

internal static void ValidateResourcesInDir(RsbGroupInfo expected,
                                            string sourceDir,
								            string groupName,
                                            ref int step,
								            bool silentLog)
{

if(expected is null)
return;

if(!silentLog)
TraceLogger.WriteStep(step, "Validate Resources");

step++;

string resourcesDir = Path.Combine(sourceDir, RsgConstants.SRC_RESOURCES);
string label = GetRsgLabel(groupName);

HashSet<string> declared = new(StringComparer.OrdinalIgnoreCase); // Stores normalized names

CheckExistingRes(expected.ResFiles, resourcesDir, label, declared, silentLog);
CheckExistingTextures(expected.Textures?.Keys, resourcesDir, label, declared, silentLog);

if(!Directory.Exists(resourcesDir) )
return;

CheckDeclaredResources(resourcesDir, declared, label, silentLog);
}

// Seed a CodecContext with texture metadata from RSB

internal static void SeedPtxContext(RsbGroupInfo expected,
                                   string resName,
								   RsbPlatform platform,
                                   CodecContext context,
								   bool forEncoding)
{
var textures = expected?.Textures;

if(textures is null)
return;

string normalized = Normalize(resName);

if(!textures.TryGetValue(normalized, out var texInfo) )
return;

context.Set(PtxCodec.CTX_KEY_PLATFORM, platform);
context.Set(PtxCodec.CTX_KEY_FORMAT, texInfo.Format);

if(forEncoding)
return; // Only platform and format are needed when encoding

context.Set(PtxCodec.CTX_KEY_WIDTH, texInfo.Width);
context.Set(PtxCodec.CTX_KEY_HEIGHT, texInfo.Height);
context.Set(PtxCodec.CTX_KEY_PITCH, texInfo.Pitch);

if(texInfo.AlphaSize.HasValue)
context.Set(PtxCodec.CTX_KEY_ALPHA_SIZE, texInfo.AlphaSize.Value);

if(texInfo.Scale.HasValue)
context.Set(PtxCodec.CTX_KEY_SCALE, texInfo.Scale.Value);

}

}

}