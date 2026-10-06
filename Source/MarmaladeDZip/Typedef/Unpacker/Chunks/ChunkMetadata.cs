using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyPackages.MarmaladeDZip
{
/// <summary> Raw data for a Dz chunk </summary>

[StructLayout(LayoutKind.Explicit, Size = 16)]

public readonly struct ChunkMetadata
{
/// <summary> Chuck offset inside Blob. </summary>

[FieldOffset(0)]
public readonly int ChunkOffset;

/// <summary> DZip size. </summary>

[FieldOffset(4)]
public readonly int DZ_Size;

/// <summary> Chunk Size (before Compression) </summary>

[FieldOffset(8)]
public readonly int ChunkSize;

/// <summary> Compression flags </summary>

[FieldOffset(12)]
public readonly DzFlags CompressionFlags;

/// <summary> File index. </summary>

[FieldOffset(14)]
public readonly ushort FileIndex;
 
// Read ChunkInfo

public static ChunkMetadata Read(Stream reader)
{
Span<byte> rawData = stackalloc byte[16];
reader.ReadExactly(rawData);

return MemoryMarshal.Read<ChunkMetadata>(rawData);
}

}

}