using System.IO;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Load external ResGroups </summary>

internal static class ExternalRsgLoader
{
// Resolve external rsg path

public static string ResolvePath(string parentPath, string rsgName)
{
string fileName = rsgName + ".rsg";
string rsgPath = parentPath is null ? fileName : Path.Combine(parentPath, fileName);

if(File.Exists(rsgPath) )
return rsgPath;

string smfPath = rsgPath + ".smf";

if(File.Exists(smfPath) )
return smfPath;

return null;
}

// Load single group

public static NativeBuffer LoadGroup(string parentPath, string groupName)
{
string rsgPath = ResolvePath(parentPath, groupName);

if(rsgPath is null)
{
TraceLogger.WriteWarn($"Missing external RSG: '{groupName}'");

return null;
}

using var externalRsg = FileManager.OpenRead(rsgPath);

return externalRsg.ReadPtr();
}

// Load all RSGs (Core)

private static GroupMap LoadGroupsCore(string parentPath,
                                       in RsbInfo info,
                                       RsbGroupDescriptor[] groupInfo,
                                       string[] groupIDs,
                                       RsbPoolDescriptor[] poolInfo,
                                       RsbTextureDescriptor[] ptxInfo)
{
GroupMap groupsMap = new();

for(int i = 0; i < groupInfo.Length; i++)
{
var desc = groupInfo[i];

string groupName = RsgExtractor.ResolveRsgName(desc, groupIDs[i] );
using var rsgBuffer = LoadGroup(parentPath, groupName);

RsgExtractor.ParseRsgMetadata(rsgBuffer,
                              info,
                              desc,
                              poolInfo,
                              ptxInfo,
                              out var resFiles,
                              out var textures);

string poolName = RsbMetadataJsonBuilder.ResolvePoolName(poolInfo, desc.Index);
RsbGroupInfo groupEntry = new(desc.CompressionFlags, resFiles, textures, poolName);

groupsMap.Add(groupName, groupEntry);
}

return groupsMap;
}

// Load all RSGs

public static GroupMap Load(string parentPath,
                            in RsbInfo info,
							RsbGroupDescriptor[] groupInfo,
                            string[] groupIDs,
							RsbPoolDescriptor[] poolInfo,
                            RsbTextureDescriptor[] ptxInfo)
{
TraceLogger.WriteActionStart("Loading ResGroups...");
var groupsMap = LoadGroupsCore(parentPath, info, groupInfo, groupIDs, poolInfo, ptxInfo);

TraceLogger.WriteActionEnd();

return groupsMap;
}

}

}