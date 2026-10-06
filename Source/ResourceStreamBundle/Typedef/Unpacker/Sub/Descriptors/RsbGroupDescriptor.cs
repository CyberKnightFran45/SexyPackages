using System;
using System.IO;
using System.Runtime.InteropServices;
using SexyPackages.ResourceStreamGroup;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Descriptor for a ResGroup inside a RSB </summary>

[StructLayout(LayoutKind.Explicit, Size = 204)]

public unsafe struct RsbGroupDescriptor
{
/// <summary> Group name </summary>

[FieldOffset(0)]
public fixed byte Name[128];

/// <summary> Offset to RSG </summary>

[FieldOffset(128)]
public uint Offset;

/// <summary> RSG size (in bytes) </summary>

[FieldOffset(132)]
public uint Size;

/// <summary> Group index inside pool </summary>

[FieldOffset(136)]
public uint Index;

/// <summary> Compression flags </summary>

[FieldOffset(140)]
public GroupCompressionFlags CompressionFlags;

/// <summary> Header length </summary>

[FieldOffset(144)]
public uint HeaderLength;

/// <summary> Offset to ResidentData </summary>

[FieldOffset(148)]
public uint ResidentDataOffset;

/// <summary> ResidentData Size (after Compression) </summary>

[FieldOffset(152)]
public uint ResidentDataSizeCompressed;

/// <summary> ResidentData Size (before Compression) </summary>

[FieldOffset(156)]
public uint ResidentDataSize;

/// <summary> Amount of bytes ocupied by ResidentData inside Pool </summary>

[FieldOffset(160)]
public uint ResidentPoolSize;

/// <summary> Offset to GPUData </summary>

[FieldOffset(164)]
public uint GPUDataOffset;

/// <summary> GPUData Size (after Compression) </summary>

[FieldOffset(168)]
public uint GPUDataSizeCompressed;

/// <summary> GPUData Size (before Compression) </summary>

[FieldOffset(172)]
public uint GPUDataSize;

/// <summary> Amount of bytes ocupied by GPUData inside Pool <c>(unused, always 0)</c> </summary>

[FieldOffset(176)]
public readonly uint GPUPoolSize;

/// <summary> Some padding </summary>

[FieldOffset(180)]
private fixed uint Padding[4];

/// <summary> Amount of textures embedded <c>(used in V3-V4)</c> </summary>

[FieldOffset(196)]
public uint TextureCount;

/// <summary> TextureDescriptor Start Index <c>(used in V3-V4)</c> </summary>

[FieldOffset(200)]
public uint PtxDescriptorStartIndex;

// Max name length

private const int MAX_NAME_LENGTH = 128;

// Read field

private static uint ReadField(ReadOnlySpan<byte> data, int offset, Endianness endian)
{
var rawField = data.Slice(offset, 4);

return BinaryHelper.ReadUInt32(rawField, endian);
}

// Copy raw name to ptr

private static void CopyName(ReadOnlySpan<byte> rawData, byte* namePtr)
{
var rawName = rawData[ .. MAX_NAME_LENGTH ];
Span<byte> name = new(namePtr, MAX_NAME_LENGTH);

rawName.CopyTo(name);
}

// Read GroupDescriptor

public static RsbGroupDescriptor Read(ReadOnlySpan<byte> rawData, Endianness endian)
{
RsbGroupDescriptor info = new();

CopyName(rawData, info.Name);

info.Offset = ReadField(rawData, 128, endian);
info.Size = ReadField(rawData, 132, endian);
info.Index = ReadField(rawData, 136, endian);
info.CompressionFlags = (GroupCompressionFlags)ReadField(rawData, 140, endian);
info.HeaderLength = ReadField(rawData, 144, endian);

info.ResidentDataOffset = ReadField(rawData, 148, endian);
info.ResidentDataSizeCompressed = ReadField(rawData, 152, endian);
info.ResidentDataSize = ReadField(rawData, 156, endian);
info.ResidentPoolSize = ReadField(rawData, 160, endian);

info.GPUDataOffset = ReadField(rawData, 164, endian);
info.GPUDataSizeCompressed = ReadField(rawData, 168, endian);
info.GPUDataSize = ReadField(rawData, 172, endian);

int entrySize = rawData.Length;

if(entrySize >= 204)
{
info.TextureCount = ReadField(rawData, 196, endian);
info.PtxDescriptorStartIndex = ReadField(rawData, 200, endian);
}

return info;
}

// Write field

private static void WriteField(Span<byte> data, int offset, uint v, Endianness endian)
{
var rawField = data.Slice(offset, 4);

BinaryHelper.WriteUInt32(v, rawField, endian);
}

// Copy name ptr to span

private static void CopyName(byte* namePtr, Span<byte> rawData)
{
Span<byte> name = new(namePtr, MAX_NAME_LENGTH);
var rawName = rawData[ .. MAX_NAME_LENGTH ];

name.CopyTo(rawName);
}

// Write GroupDescriptor

public void Write(Stream writer, uint entrySize, Endianness endian)
{
Span<byte> rawData = stackalloc byte[ (int)entrySize];

fixed(byte* namePtr = Name)
CopyName(namePtr, rawData);

WriteField(rawData, 128, Offset, endian);
WriteField(rawData, 132, Size, endian);
WriteField(rawData, 136, Index, endian);
WriteField(rawData, 140, (uint)CompressionFlags, endian);
WriteField(rawData, 144, HeaderLength, endian);

WriteField(rawData, 148, ResidentDataOffset, endian);
WriteField(rawData, 152, ResidentDataSizeCompressed, endian);
WriteField(rawData, 156, ResidentDataSize, endian);
WriteField(rawData, 160, ResidentPoolSize, endian);

WriteField(rawData, 164, GPUDataOffset, endian);
WriteField(rawData, 168, GPUDataSizeCompressed, endian);
WriteField(rawData, 172, GPUDataSize, endian);

if(entrySize >= 204)
{
WriteField(rawData, 196, TextureCount, endian);
WriteField(rawData, 200, PtxDescriptorStartIndex, endian);
}

writer.Write(rawData);
}

}

}