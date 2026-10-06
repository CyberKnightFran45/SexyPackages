using System;
using System.IO;
using SexyPackages.ResourceStreamGroup;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB Legacy Packer: rebuild project </summary>

public static class RsbLegacyPacker
{
// Rebuild each group dir, then pack RSB

private static BufferMap RebuildGroups(string sourceDir,
                                       FileProgress progress,
                                       bool? encryptPackages,
                                       bool useExternalRsgs)
{
BufferMap overrides = new(StringComparer.OrdinalIgnoreCase);

string groupsResName = RsbConstants.SRC_GROUPS_RESOURCES;
string groupsResDir = Path.Combine(sourceDir, groupsResName);

if(!Directory.Exists(groupsResDir) )
{
var msg = $"'{groupsResName}' dir not found. Standar RSB build will be done instead.";
TraceLogger.WriteWarn(msg);

return overrides;
}

TraceLogger.WriteActionStart("Rebuilding ResGroups...");

string groupsDir = Path.Combine(sourceDir, RsbConstants.SRC_GROUPS);
var groupsEntries = RsbMetadataLoader.LoadGroupsInfo(sourceDir);

if(useExternalRsgs)
Directory.CreateDirectory(groupsDir);

var groups = Directory.EnumerateDirectories(groupsResDir);

foreach(string groupDir in groups)
{
string groupName = Path.GetFileName(groupDir);
string infoSrc = Path.Combine("Metadata", RsbConstants.SRC_GROUPS_INFO);

if(!groupsEntries.TryGetInfo(groupName, out var expected) )
{
var msg = $"Missing RSG info: '{groupName}' @ '{infoSrc}'. Rebuild will be omitted for this group.";
TraceLogger.WriteWarn(msg);

continue;
}

bool isPackages = RsbUtils.IsPackages(groupName);
bool? groupEncryption = isPackages ? encryptPackages : null;

if(useExternalRsgs)
{
string targetRsg = Path.Combine(groupsDir, groupName + ".rsg");
string groupsInfoPath = Path.Combine(sourceDir, infoSrc);

RsgLegacyPacker.PackToDir(groupDir, targetRsg, groupsInfoPath, progress, groupEncryption);
}

else
{
var platform = groupsEntries.GetPlatform();

var rsgBuffer = RsgLegacyPacker.PackToBuffer(groupDir, expected, platform, groupName, 
                                             progress, groupEncryption);

overrides.Add(groupName, rsgBuffer);
}

}

TraceLogger.WriteActionEnd();

return overrides;
}

// Pack Core (rebuilds Groups first if needed)

internal static void PackCore(string sourceDir, Stream target, out bool addSmfExt,
                              FileProgress progress = null)
{
var cfg = RsbMetadataLoader.LoadParams(sourceDir);
bool useExternalRsgs = cfg?.UseExternalRsgs ?? false;

var overrides = RebuildGroups(sourceDir, progress, cfg?.EncryptPackages, useExternalRsgs);

RsbPacker.Compress(sourceDir, target, out addSmfExt, overrides);
}

// Pack internal (file-based)

private static void PackInternal(string sourceDir, string targetFile, FileProgress progress,
                                 TraceContext ctx)
{
PathHelper.ChangeExtension(ref targetFile, ".rsb");

bool addSmfExt;

using(var rsbStream = FileManager.OpenWrite(targetFile) )
{
PackCore(sourceDir, rsbStream, out addSmfExt, progress);

ctx.OutputSize = rsbStream.Length;
}

ctx.LogOutSize = true;

if(addSmfExt)
{
string smfPath = targetFile;
PathHelper.AddExtension(ref smfPath, ".smf");

File.Move(targetFile, smfPath); // Rename file
}

}

/// <summary> Rebuilds a RSB from a directory, encoding all resources and building RSGs
///           if 'GroupResources' dir is present. </summary>

public static void Pack(string sourceDir, string targetFile, FileProgress progress = null)
{

TraceExecutor.Run("RSB Legacy Pack",
                  ctx => PackInternal(sourceDir, targetFile, progress, ctx),
                  ("SourceDir", sourceDir),
                  ("TargetFile", targetFile)
);

}

}

}