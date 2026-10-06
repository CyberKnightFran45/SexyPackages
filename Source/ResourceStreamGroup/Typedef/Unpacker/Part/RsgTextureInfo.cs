using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyPackages.ResourceStreamGroup
{
/// <summary> Stores info about a GPU Texture inside a ResGroup. </summary>

[StructLayout(LayoutKind.Explicit, Size = 28)]

public unsafe struct RsgTextureInfo
{
/// <summary> Offset to Texture file </summary>

[FieldOffset(0)]
public uint Offset;

/// <summary> Size in bytes </summary>

[FieldOffset(4)]
public uint Size;

/// <summary> Texture Index inside GPU Pool </summary>

[FieldOffset(8)]
public uint TextureIndex;

/// <summary> Some padding </summary>

[FieldOffset(12)]
private fixed int Padding[2];

/// <summary> Texture Width </summary>

[FieldOffset(20)]
public uint Width;

/// <summary> Texture Height </summary>

[FieldOffset(24)]
public uint Height;

// ctor

public RsgTextureInfo(uint offset, uint size, uint index, uint width, uint height)
{
Offset = offset;

Size = size;
TextureIndex = index;

Width = width;
Height = height;
}

// Read GPUInfo

public static RsgTextureInfo Read(NativeBuffer buffer, ref ulong pos, Endianness endian)
{
var rawData = buffer.GetView(pos, 28);
pos += 28;

var info = MemoryMarshal.Read<RsgTextureInfo>(rawData);

if(endian == Endianness.BigEndian)
info.SwapEndian();

return info;
}

// Write GPUInfo

public void Write(Span<byte> rawData, Endianness endian)
{

if(endian == Endianness.BigEndian)
SwapEndian();

MemoryMarshal.Write(rawData, this);
}

// Reverse Endianness

private void SwapEndian()
{
Offset = BinaryPrimitives.ReverseEndianness(Offset);

Size = BinaryPrimitives.ReverseEndianness(Size);
TextureIndex = BinaryPrimitives.ReverseEndianness(TextureIndex);

Width = BinaryPrimitives.ReverseEndianness(Width);
Height = BinaryPrimitives.ReverseEndianness(Height);
}

}

}