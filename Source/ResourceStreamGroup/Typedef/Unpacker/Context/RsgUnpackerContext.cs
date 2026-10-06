namespace SexyPackages.ResourceStreamGroup
{
/// <summary> RSG Unpacker Context </summary>

public sealed class RsgUnpackerContext
{
/// <summary> File Endianness </summary>

public Endianness Endian{ get; }

/// <summary> RSG Info </summary>

public RsgInfo Info{ get; }

/// <summary> Resource Map </summary>

public ResourceMap ResMap{ get; }

/// <summary> Part0 buffer </summary>

public RawBuffer Part0{ get; }

/// <summary> Part1 buffer </summary>

public RawBuffer Part1{ get; }

// ctor

public RsgUnpackerContext(Endianness endian, RsgInfo info, ResourceMap resMap,
                          RawBuffer part0, RawBuffer part1)
{
Endian = endian;

Info = info;
ResMap = resMap;

Part0 = part0;
Part1 = part1;
}

}

}