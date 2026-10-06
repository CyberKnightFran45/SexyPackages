using System.IO;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB Manifest Builder </summary>

internal static partial class RsbManifestHandler
{
#region ==============  MANIFEST BUILDER  ==============

// Write single resource (header + props)

private static uint WriteResource(Stream resStream, StringPoolWriter pool, string resId,
                                  ManifestResInfo_Json resInfo, Endianness endian)
{
var headerOffset = (uint)resStream.Position;

bool hasImg = resInfo.ImageProperties is not null;

var universalProps = resInfo.UniversalProperties;
bool hasUniversal = universalProps is not null && universalProps.Count > 0;

uint imgOffset = hasImg ? headerOffset + 28 : 0;

uint universalOffset = hasUniversal ? headerOffset + 28 + (uint)(hasImg ? 24 : 0) : 0;
uint universalCount = hasUniversal ? (uint)universalProps.Count : 0;

uint idOffset = pool.Add(resId);
uint pathOffset = pool.Add(resInfo.Path);

ManifestResHeader header = new()
{
Type = (ushort)resInfo.Type,
HeaderSize = 28,
UniversalPropertyOffset = universalOffset,
ImagePropertyOffset = imgOffset,
IdOffset = idOffset,
PathOffset = pathOffset,
UniversalPropertyCount = universalCount
};

header.Write(resStream, endian);

if(hasImg)
{
var ip = resInfo.ImageProperties;
uint parentOffset = pool.Add(ip.ParentName);

ManifestResImageProperty imgProp = new()
{
Type = ip.Type,
AtlasFlags = ip.AtlasFlags,
X = ip.X,
Y = ip.Y,
AtlasX = ip.AtlasX,
AtlasY = ip.AtlasY,
AtlasWidth = ip.AtlasWidth,
AtlasHeight = ip.AtlasHeight,
Rows = ip.Rows,
Cols = ip.Cols,
ParentOffset = parentOffset
};

imgProp.Write(resStream, endian);
}

if(hasUniversal)
{

foreach(var (key, val) in universalProps)
{
uint keyOffset = pool.Add(key);
uint valOffset = pool.Add(val);

ManifestResUniversalProperty prop = new(keyOffset, valOffset);

prop.Write(resStream, endian);
}

}

return headerOffset;
}

// Write SubGroup along with its resources

private static void WriteSubGroup(Stream groupStream, Stream resStream, StringPoolWriter pool,
                                  string groupId, ManifestGroupInfo_Json groupJson,
                                  bool isV3OrGreater, Endianness endian)
{
var resources = groupJson.Resources ?? new();
uint resCount = (uint)resources.Count;

uint[] resOffsets = new uint[resCount];

int i = 0;

foreach(var (resId, resInfo) in resources)
resOffsets[i++] = WriteResource(resStream, pool, resId, resInfo, endian);

uint groupIdOffset = pool.Add(groupId);

if(isV3OrGreater)
{
string locale = groupJson.Localization;
uint locFlags = string.IsNullOrEmpty(locale) ? 0u : String32.ToInt(locale);

ManifestGroupInfoV3 gi = new(groupJson.Res, locFlags, groupIdOffset, resCount);

gi.Write(groupStream, endian);
}

else
{
ManifestGroupInfo gi = new(groupJson.Res, groupIdOffset, resCount);

gi.Write(groupStream, endian);
}

foreach(uint offset in resOffsets)
groupStream.WriteUInt32(offset, endian);

}

// Write Composite along with its sub groups

private static void WriteComposite(Stream groupStream, Stream resStream, StringPoolWriter pool,
                                   string compositeId, ManifestCompositeInfo_Json compositeJson,
                                   bool isV3OrGreater, Endianness endian)
{
var subGroups = compositeJson.SubGroups ?? new();
uint childCount = (uint)subGroups.Count;

uint groupInfoStructSize = isV3OrGreater ? 16u : 12u;
uint groupInfoLength = 0;

foreach(var kvp in subGroups)
{
uint resCount = (uint)(kvp.Value?.Resources?.Count ?? 0);

groupInfoLength += groupInfoStructSize + 4u * resCount;
}

uint compositeIdOffset = pool.Add(compositeId);
ManifestCompositeInfo compositeInfo = new(compositeIdOffset, childCount, groupInfoLength);

compositeInfo.Write(groupStream, endian);

foreach(var (groupId, groupJson) in subGroups)
WriteSubGroup(groupStream, resStream, pool, groupId, groupJson, isV3OrGreater, endian);

}

// Write manifest (Core)

public static void Write(RsbManifest manifest, RsbMajorVersion majorVersion, Endianness endian,
                         out MemoryStream groupStream,
						 out MemoryStream resStream,
                         out MemoryStream poolStream)
{
groupStream = new();
resStream = new();

StringPoolWriter pool = new();
bool isV3OrGreater = majorVersion >= RsbMajorVersion.V3;

if(manifest is not null)
{

foreach(var (compositeId, compositeJson) in manifest)
WriteComposite(groupStream, resStream, pool, compositeId, compositeJson, isV3OrGreater, endian);

}

poolStream = pool.Stream;
}

#endregion
}

}