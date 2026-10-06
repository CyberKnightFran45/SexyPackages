using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace SexyPackages.ResourceStreamGroup
{
/// <summary> Stores info about a Resident File inside a ResGroup. </summary>

[StructLayout(LayoutKind.Explicit, Size = 8)]

public struct RsgResidentInfo
{
/// <summary> File Offset </summary>

[FieldOffset(0)]
public uint Offset;

/// <summary> File Size </summary>

[FieldOffset(4)]
public uint Size;

// ctor 

public RsgResidentInfo(uint offset, uint size)
{
Offset = offset;
Size = size;
}

// Read ResidentInfo

public static RsgResidentInfo Read(NativeBuffer buffer, ref ulong pos, Endianness endian)
{
var rawData = buffer.GetView(pos, 8);
pos += 8;

var info = MemoryMarshal.Read<RsgResidentInfo>(rawData);

if(endian == Endianness.BigEndian)
info.SwapEndian();

return info;
}

// Write ResidentInfo

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
}

}

}