using System.IO;

namespace SexyPackages.ResourceStreamGroup
{
/// <summary> RSG Packer Context </summary>

public sealed class RsgPackerContext
{
/// <summary> File Endianness </summary>

public Endianness Endian{ get; }

/// <summary> RSG Info </summary>

public RsgInfo Info{ get; }

/// <summary> Resource Map (encoded) </summary>

public MemoryStream ResMap{ get; }

/// <summary> Part0 buffer </summary>

public MemoryStream Part0{ get; }

/// <summary> Part1 buffer </summary>

public MemoryStream Part1{ get; }

// ctor

public RsgPackerContext(Endianness endian, RsgInfo info, MemoryStream resMap,
                        MemoryStream part0, MemoryStream part1)
{
Endian = endian;

Info = info;
ResMap = resMap;

Part0 = part0;
Part1 = part1;

ResMap = resMap;
}

}

}