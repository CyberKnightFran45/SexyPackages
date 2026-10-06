using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB Composite Info (JSON model) </summary>

public class RsbCompositeInfo
{
/// <summary> SubGroups List </summary>

public SubGroupMap SubGroups { get; set; }

// ctor

public RsbCompositeInfo()
{
}

// ctor 2

public unsafe RsbCompositeInfo(in RsbCompositeDescriptor descriptor, string[] groupIDs)
{
SubGroups = new();

uint count = Math.Min(descriptor.ChildCount, 64);

fixed(byte* pChilds = descriptor.Childs)
{
var childPtr = (RsbChildDescriptor*)pChilds;

for(uint i = 0; i < count; i++)
{
var childDesc = childPtr[i];

uint groupIndex = childDesc.GroupIndex;
string groupID = groupIDs[groupIndex];

uint artRes = childDesc.ArtResolution;
RsbSubGroupInfo extraInfo = artRes > 0 ? new(artRes) : null;

SubGroups.Add(groupID, extraInfo);
}

}

}

// ctor 3

public unsafe RsbCompositeInfo(in RsbCompositeDescriptorV3 descriptor, string[] groupIDs)
{
SubGroups = new();

uint count = Math.Min(descriptor.ChildCount, 64);

fixed(byte* pChilds = descriptor.Childs)
{
var childPtr = (RsbChildDescriptorV3*)pChilds;

for(uint i = 0; i < count; i++)
{
var childDesc = childPtr[i];

uint groupIndex = childDesc.GroupIndex;
string groupID = groupIDs[groupIndex];

uint artRes = childDesc.ArtResolution;

uint locFlags = childDesc.Localization;
var locale = locFlags > 0 ? String32.FromInt(locFlags) : null;

bool hasInfo = artRes > 0 || locale != null;
RsbSubGroupInfo extraInfo = hasInfo ? new(artRes, locale) : null;

SubGroups.Add(groupID, extraInfo);
}

}

}

public static readonly JsonSerializerContext Context = new RsbCompositeContext(JsonSerializer.Options);
}

// Context for serialization

[JsonSerializable(typeof(RsbCompositeInfo) ) ]

[JsonSerializable(typeof(RsbSubGroupInfo) ) ]

public partial class RsbCompositeContext : JsonSerializerContext
{
}

}