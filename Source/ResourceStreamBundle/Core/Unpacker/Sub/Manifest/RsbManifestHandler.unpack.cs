using System;
using System.Collections.Generic;
using System.IO;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB Manifest Unpacker </summary>

internal static partial class RsbManifestHandler
{
#region ==============  MANIFEST UNPACKER  ==============

// Get string from pool ID

private static string GetStringFromPool(NativeBuffer poolBuffer, uint offset)
{
using var rawID = poolBuffer.GetCString(offset);

return rawID.ToString();
}

// Read CompositeInfo

private static ManifestCompositeInfo ReadCompositeInfo(ReadOnlySpan<byte> span,
                                                       ref int offset,
                                                       Endianness endian)
{
var rawData = span.Slice(offset, 12);

offset += 12;

return ManifestCompositeInfo.Read(rawData, endian);
}

// Parse Composites

private static RsbManifest ParseComposites(NativeBuffer groupBuffer,
                                           NativeBuffer resBuffer, 
                                           NativeBuffer poolBuffer, 
                                           bool isV3OrGreater, 
                                           Endianness endian)
{
RsbManifest compositeGroups = new();

var groupSpan = groupBuffer.GetView();
int groupOffset = 0;

while(groupOffset < groupSpan.Length)
{
var compositeInfo = ReadCompositeInfo(groupSpan, ref groupOffset, endian);
var compositeId = GetStringFromPool(poolBuffer, compositeInfo.IdOffset);

var subGroups = ParseSubGroups(groupSpan,
                               resBuffer,
							   poolBuffer,
							   ref groupOffset,
                               compositeInfo.ChildCount,
							   isV3OrGreater,
							   endian);

ManifestCompositeInfo_Json compositeJson = new(subGroups);

compositeGroups.Add(compositeId, compositeJson);
}

return compositeGroups;
}

// Read ManifestGroup Info

private static ManifestGroupInfo ReadGroupInfo(ReadOnlySpan<byte> span,
                                               ref int offset,
											   Endianness endian)
{
var rawData = span.Slice(offset, 12);

offset += 12;

return ManifestGroupInfo.Read(rawData, endian);
}

// Read ManifestGroup Info (V3)

private static ManifestGroupInfoV3 ReadGroupInfoV3(ReadOnlySpan<byte> span,
                                                   ref int offset,
                                                   Endianness endian)
{
var rawData = span.Slice(offset, 16);

offset += 16;

return ManifestGroupInfoV3.Read(rawData, endian);
}

// Parse SubGroups

private static RsbManifestGroupMap ParseSubGroups(ReadOnlySpan<byte> groupSpan, 
                                                  NativeBuffer resBuffer, 
                                                  NativeBuffer poolBuffer, 
                                                  ref int groupOffset, 
                                                  uint childCount, 
                                                  bool isV3OrGreater, 
                                                  Endianness endian)
{
RsbManifestGroupMap subGroupsDict = new( (int)childCount);

for(uint i = 0; i < childCount; i++)
{
ManifestGroupInfo_Json groupJson = new();

string groupId;
uint resCount;

if(isV3OrGreater)
{
var groupInfoV3 = ReadGroupInfoV3(groupSpan, ref groupOffset, endian);

groupId = GetStringFromPool(poolBuffer, groupInfoV3.IdOffset);
groupJson.Res = groupInfoV3.ArtResolution;

groupJson.Localization = String32.FromInt(groupInfoV3.Localization);
resCount = groupInfoV3.ResCount;
}

else
{
var groupInfo = ReadGroupInfo(groupSpan, ref groupOffset, endian);

groupId = GetStringFromPool(poolBuffer, groupInfo.IdOffset);
groupJson.Res = groupInfo.ArtResolution;

resCount = groupInfo.ResCount;
}

var resOffsets = ReadResOffsets(groupSpan, ref groupOffset, resCount, endian);
groupJson.Resources = ParseResources(resBuffer, poolBuffer, resOffsets, endian);

subGroupsDict.Add(groupId, groupJson);
}

return subGroupsDict;
}

// Read Res Offsets

private static uint[] ReadResOffsets(ReadOnlySpan<byte> span,
                                     ref int offset,
                                     uint resCount,
									 Endianness endian)
{
uint[] resOffsets = new uint[resCount];

for(uint i = 0; i < resCount; i++)
{
var rawBytes = span.Slice(offset, 4);
resOffsets[i] = BinaryHelper.ReadUInt32(rawBytes, endian);

offset += 4;
}

return resOffsets;
}

// Read ResHeader

private static ManifestResHeader ReadResHeader(ReadOnlySpan<byte> span,
                                               int offset,
											   Endianness endian)
{
var rawData = span.Slice(offset, 28);

return ManifestResHeader.Read(rawData, endian);
}

// Read ImageProperties

private static ManifestResImageProperty ReadImgProps(ReadOnlySpan<byte> span,
                                                     int offset,
													 Endianness endian)
{
var rawData = span.Slice(offset, 24);

return ManifestResImageProperty.Read(rawData, endian);
}

// Parse ImageProperties

private static ManifestResImageProperty_Json ParseImageProps(ReadOnlySpan<byte> resSpan,
                                                             NativeBuffer poolBuffer,
                                                             uint imagePropOffset,
                                                             Endianness endian)
{

if(imagePropOffset == 0)
return null;

var imgProp = ReadImgProps(resSpan, (int)imagePropOffset, endian);

uint parentOffset = imgProp.ParentOffset;
var parentName = parentOffset > 0 ? GetStringFromPool(poolBuffer, parentOffset) : null;

return new(imgProp, parentName);
}

// Read ImageProperties

private static ManifestResUniversalProperty ReadUniversalProps(ReadOnlySpan<byte> span,
                                                               ref int offset,
                                                               Endianness endian)
{
var rawData = span.Slice(offset, 12);

offset += 12;

return ManifestResUniversalProperty.Read(rawData, endian);
}

// Parse UniversalProperties

private static Dictionary<string, string> ParseUniversalProps(ReadOnlySpan<byte> resSpan, 
                                                              NativeBuffer poolBuffer, 
                                                              uint startOffset, 
                                                              uint propertyCount,
                                                              Endianness endian)
{

if(propertyCount == 0)
return null;

Dictionary<string, string> universalProps = new( (int)propertyCount);

var propertyOffset = (int)startOffset;

for(uint i = 0; i < propertyCount; i++)
{
var prop = ReadUniversalProps(resSpan, ref propertyOffset, endian);

string key = GetStringFromPool(poolBuffer, prop.KeyOffset);
string val = GetStringFromPool(poolBuffer, prop.ValueOffset);

universalProps.Add(key, val);
}

return universalProps;
}

// Build ResManifest JSON

private static ManifestResInfo_Json BuildResManifest(in ManifestResHeader resHeader,
                                                     NativeBuffer poolBuffer,
                                                     ReadOnlySpan<byte> resSpan,
                                                     Endianness endian)
{
var resPath = GetStringFromPool(poolBuffer, resHeader.PathOffset);
var imgProps = ParseImageProps(resSpan, poolBuffer, resHeader.ImagePropertyOffset, endian);

var universalOffset = resHeader.UniversalPropertyOffset;
var universalPropsCount = resHeader.UniversalPropertyCount;

var universalProps = ParseUniversalProps(resSpan,
                                         poolBuffer,
                                         universalOffset,
                                         universalPropsCount,
                                         endian);

return new(resHeader.Type, resPath, imgProps, universalProps);
}

// Parse resources

private static RsbManifestResMap ParseResources(NativeBuffer resBuffer, 
                                                NativeBuffer poolBuffer, 
                                                uint[] resOffsets,
                                                Endianness endian)
{
RsbManifestResMap resMap = new(resOffsets.Length);

var resSpan = resBuffer.GetView();

foreach(uint offset in resOffsets)
{
var resHeader = ReadResHeader(resSpan, (int)offset, endian);

string resID = GetStringFromPool(poolBuffer, resHeader.IdOffset);
var resourceJson = BuildResManifest(resHeader, poolBuffer, resSpan, endian);

resMap.Add(resID, resourceJson);
}

return resMap;
}

// Read manifest

public static RsbManifest Read(Stream reader, in RsbInfo info, Endianness endian)
{

if(info.ManifestGroupOffset == 0)
return null;

reader.Seek(info.ManifestGroupOffset, SeekOrigin.Begin);

long groupLength = info.ManifestResOffset - info.ManifestGroupOffset;
using var groupBuffer = reader.ReadPtr(groupLength);

reader.Seek(info.ManifestResOffset, SeekOrigin.Begin);

long resLength = info.ManifestPoolOffset - info.ManifestResOffset;
using var resBuffer = reader.ReadPtr(resLength);

reader.Seek(info.ManifestPoolOffset, SeekOrigin.Begin);

long poolLength = info.SectionLength + 108 - info.ManifestPoolOffset;
using var poolBuffer = reader.ReadPtr(poolLength);

bool isV3OrGreater = info.MajorVersion >= RsbMajorVersion.V3;

return ParseComposites(groupBuffer, resBuffer, poolBuffer, isV3OrGreater, endian);
}

#endregion
}

}