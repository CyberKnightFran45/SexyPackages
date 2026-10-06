using System.IO;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RsbContentExtractor: export info </summary>

public static partial class RsbContentExtractor
{
// Metadata only: parses every group's own ResMap

private static GroupMap CollectGroupsInfo(Stream source,
                                          RsbUnpackerContext ctx,
										  string parentPath,
                                          bool useExternalRsgs)
{
GroupMap groupsMap = new();
object groupsMapLock = new();

RsbUtils.ForEachGroup(source, ctx, parentPath, useExternalRsgs, 

(i, in desc, groupName, buffer) =>
{
var info = RsbUtils.BuildGroupInfo(buffer, ctx, desc);

lock(groupsMapLock)
groupsMap.Add(groupName, info);

},

useParalellism: false // Paralellism locks metadata exportation, DON'T use it here!
);

return groupsMap;
}

}

}