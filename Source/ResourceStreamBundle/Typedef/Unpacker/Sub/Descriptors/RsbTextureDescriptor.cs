using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Descriptor for a PTX Image inside a RSB </summary>

[StructLayout(LayoutKind.Explicit, Size = 24)]

public struct RsbTextureDescriptor
{
/// <summary> Texture Width </summary>

[FieldOffset(0)]
public uint Width;

/// <summary> Texture Height </summary>

[FieldOffset(4)]
public uint Height;

/// <summary> Texture Pitch </summary>

[FieldOffset(8)]
public uint Pitch;

/// <summary> PTX Format </summary>

[FieldOffset(12)]
public uint Format;

/// <summary> Amount of bytes used in Alpha Channel  </summary>

[FieldOffset(16)]
public uint AlphaSize;

/// <summary> Image scale  </summary>

[FieldOffset(20)]
public uint Scale;

// ctor

public RsbTextureDescriptor(uint width,
                            uint height,
							uint pitch,
							uint format,
							uint aSize,
							uint scale)
{
Width = width;
Height = height;

Pitch = pitch;
Format = format;

AlphaSize = aSize;
Scale = scale;
}

// Read field

private static uint ReadField(ReadOnlySpan<byte> data, int offset, Endianness endian)
{
var rawField = data.Slice(offset, 4);

return BinaryHelper.ReadUInt32(rawField, endian);
}

// Read uint or default

private static uint ReadField(ReadOnlySpan<byte> data,
                              int descriptorSize,
							  int minSize, 
                              int offset,
							  Endianness endian)
{
return descriptorSize >= minSize ? ReadField(data, offset, endian) : 0;
}

// Read PtxDescriptor

public static RsbTextureDescriptor Read(ReadOnlySpan<byte> rawData, Endianness endian)
{
uint width = ReadField(rawData, 0, endian);
uint height = ReadField(rawData, 4, endian);

uint pitch = ReadField(rawData, 8, endian);
uint format = ReadField(rawData, 12, endian);

int descriptorSize = rawData.Length;

uint aSize = ReadField(rawData, descriptorSize, 20, 16, endian);
uint scale = ReadField(rawData, descriptorSize, 24, 20, endian);

return new(width, height, pitch, format, aSize, scale);
}

// Write field

private static void WriteField(Span<byte> data, int offset, uint v, Endianness endian)
{
var rawField = data.Slice(offset, 4);

BinaryHelper.WriteUInt32(v, rawField, endian);
}

// Write PtxDescriptor

public readonly void Write(Stream writer, uint entrySize, Endianness endian)
{
Span<byte> rawData = stackalloc byte[ (int)entrySize];

WriteField(rawData, 0, Width, endian);
WriteField(rawData, 4, Height, endian);

WriteField(rawData, 8, Pitch, endian);
WriteField(rawData, 12, Format, endian);

if(entrySize >= 20)
WriteField(rawData, 16, AlphaSize, endian);

if(entrySize >= 24)
WriteField(rawData, 20, Scale, endian);

writer.Write(rawData);
}

}

}