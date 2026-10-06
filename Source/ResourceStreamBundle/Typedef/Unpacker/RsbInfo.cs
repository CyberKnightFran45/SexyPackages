using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Stores info related to a PopCap ResBundle. </summary>

[StructLayout(LayoutKind.Explicit, Size = 108)]

public unsafe struct RsbInfo
{
/// <summary> Major Version </summary>

[FieldOffset(0)]
public RsbMajorVersion MajorVersion;

/// <summary> Minor Version </summary>

[FieldOffset(4)]
public RsbMinorVersion MinorVersion;

/** <summary> Amount of bytes Metadata Section ocupies </summary>

<remarks> Includes: <c>Header</c> + <c>ManifestInfo?</c> + <c>TrieMap</c> + <c>Descriptors</c> </remarks> **/

[FieldOffset(8)]
public uint SectionLength;

/// <summary> Size of GlobalMap, a section where Files are indexed <c>(-1 if not present)</c> </summary>

[FieldOffset(12)]
public int GlobalMapSize;

/// <summary> Offset to GlobalMap </summary>

[FieldOffset(16)]
public uint GlobalMapOffset;

/// <summary> Some padding </summary>

[FieldOffset(20)]
private fixed uint Padding[2];

/// <summary> Size of GroupMap, a section where Groups are indexed. </summary>

[FieldOffset(28)]
public uint GroupMapSize;

/// <summary> Offset to GroupMap </summary>

[FieldOffset(32)]
public uint GroupMapOffset;

/// <summary> Amount of Groups embedded </summary>

[FieldOffset(36)]
public uint GroupCount;

/// <summary> Offset to GroupDescriptor </summary>

[FieldOffset(40)]
public uint GroupDescriptorOffset;

/// <summary> GroupDescriptor Size </summary>

[FieldOffset(44)]
public uint GroupDescriptorSize;

/// <summary> Amount of Composite Groups embedded </summary>

[FieldOffset(48)]
public uint CompositeCount;

/// <summary> Offset to CompositeDescriptor </summary>

[FieldOffset(52)]
public uint CompositeDescriptorOffset;

/// <summary> CompositeDescriptor Size </summary>

[FieldOffset(56)]
public uint CompositeDescriptorSize;

/// <summary> Size of CompositeMap, a section where Composites are indexed. </summary>

[FieldOffset(60)]
public uint CompositeMapSize;

/// <summary> Offset to CompositeMap </summary>

[FieldOffset(64)]
public uint CompositeMapOffset;

/// <summary> Amount of Pools embedded </summary>

[FieldOffset(68)]
public uint PoolCount;

/// <summary> Offset to PoolDescriptor </summary>

[FieldOffset(72)]
public uint PoolDescriptorOffset;

/// <summary> PoolDescriptor Size </summary>

[FieldOffset(76)]
public uint PoolDescriptorSize;

/// <summary> Amount of Textures embedded </summary>

[FieldOffset(80)]
public uint TextureCount;

/// <summary> Offset to TextureDescriptor </summary>

[FieldOffset(84)]
public uint PtxDescriptorOffset;

/// <summary> TextureDescriptor Size </summary>

[FieldOffset(88)]
public uint PtxDescriptorSize;

/// <summary> Offset to Manifest Group </summary>

/// <remarks> <c>0</c> if not used, meaning that manifest is stored in 
/// <b>__MANIFESTGROUP__</b> </remarks>

[FieldOffset(92)]
public uint ManifestGroupOffset;

/// <summary> Offset to Resource Manifest </summary>

[FieldOffset(96)]
public uint ManifestResOffset;

/// <summary> Offset to StringPool Manifest </summary>

[FieldOffset(100)]
public uint ManifestPoolOffset;

/// <summary> Header size excluding Manifest <c>(used in V4)</c> </summary>

[FieldOffset(104)]
public uint HeaderSizeNoManifest;

// ctor

public RsbInfo()
{
}

// Read RsbInfo

public static RsbInfo Read(Stream reader, Endianness endian)
{
Span<byte> rawData = stackalloc byte[108];
reader.ReadExactly(rawData);

var info = MemoryMarshal.Read<RsbInfo>(rawData);

if(endian == Endianness.BigEndian)
info.SwapEndian();

return info;
}

// Write RsbInfo

public void Write(Stream writer, Endianness endian)
{
Span<byte> rawData = stackalloc byte[108];

if(endian == Endianness.BigEndian)
SwapEndian();

MemoryMarshal.Write(rawData, this);

writer.Write(rawData);
}

// Reverse Endianness

private void SwapEndian()
{
MajorVersion = (RsbMajorVersion)BinaryPrimitives.ReverseEndianness( (uint)MajorVersion);
MinorVersion = (RsbMinorVersion)BinaryPrimitives.ReverseEndianness( (uint)MinorVersion);

SectionLength = BinaryPrimitives.ReverseEndianness(SectionLength);

GlobalMapSize = BinaryPrimitives.ReverseEndianness(GlobalMapSize);
GlobalMapOffset = BinaryPrimitives.ReverseEndianness(GlobalMapOffset);

GroupMapSize = BinaryPrimitives.ReverseEndianness(GroupMapSize);
GroupMapOffset = BinaryPrimitives.ReverseEndianness(GroupMapOffset);

GroupCount = BinaryPrimitives.ReverseEndianness(GroupCount);
GroupDescriptorOffset = BinaryPrimitives.ReverseEndianness(GroupDescriptorOffset);
GroupDescriptorSize = BinaryPrimitives.ReverseEndianness(GroupDescriptorSize);

CompositeCount = BinaryPrimitives.ReverseEndianness(CompositeCount);
CompositeDescriptorOffset = BinaryPrimitives.ReverseEndianness(CompositeDescriptorOffset);
CompositeDescriptorSize = BinaryPrimitives.ReverseEndianness(CompositeDescriptorSize);

CompositeMapSize = BinaryPrimitives.ReverseEndianness(CompositeMapSize);
CompositeMapOffset = BinaryPrimitives.ReverseEndianness(CompositeMapOffset);

PoolCount = BinaryPrimitives.ReverseEndianness(PoolCount);
PoolDescriptorOffset = BinaryPrimitives.ReverseEndianness(PoolDescriptorOffset);
PoolDescriptorSize = BinaryPrimitives.ReverseEndianness(PoolDescriptorSize);

TextureCount = BinaryPrimitives.ReverseEndianness(TextureCount);
PtxDescriptorOffset = BinaryPrimitives.ReverseEndianness(PtxDescriptorOffset);
PtxDescriptorSize = BinaryPrimitives.ReverseEndianness(PtxDescriptorSize);

ManifestGroupOffset = BinaryPrimitives.ReverseEndianness(ManifestGroupOffset);
ManifestResOffset = BinaryPrimitives.ReverseEndianness(ManifestResOffset);
ManifestPoolOffset = BinaryPrimitives.ReverseEndianness(ManifestPoolOffset);

HeaderSizeNoManifest = BinaryPrimitives.ReverseEndianness(HeaderSizeNoManifest);
}

}

}