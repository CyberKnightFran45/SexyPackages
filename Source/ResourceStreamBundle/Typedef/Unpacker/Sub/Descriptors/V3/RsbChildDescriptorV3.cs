using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyPackages.ResourceStreamBundle
{
/** <summary> Descriptor for a RSB SubGroup </summary>

<remarks> Used in V3-V4 of the Algorithm. </remarks> **/

[StructLayout(LayoutKind.Explicit, Size = 16)]

public struct RsbChildDescriptorV3
{
/// <summary> ResGroup index </summary>

[FieldOffset(0)]
public uint GroupIndex;

/// <summary> Art resolution </summary>

[FieldOffset(4)]
public uint ArtResolution;

/// <summary> Language localization </summary>

[FieldOffset(8)]
public uint Localization;

/// <summary> Unknown field </summary>

[FieldOffset(12)]
private readonly uint Reserved;

// ctor

public RsbChildDescriptorV3(uint index, uint artRes, uint locale)
{
GroupIndex = index;

ArtResolution = artRes;
Localization = locale;
}

// Reverse Endianness

public void SwapEndian()
{
GroupIndex = BinaryPrimitives.ReverseEndianness(GroupIndex);

ArtResolution = BinaryPrimitives.ReverseEndianness(ArtResolution);
Localization = BinaryPrimitives.ReverseEndianness(Localization);
}

}

}