using SexyPackages.ResourceStreamGroup;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Represents a Loaded RSG </summary>

internal sealed class LoadedRsg
{
/// <summary> Group Name </summary>

public string GroupName{ get; }

/// <summary> RSG Buffer </summary>

public NativeBuffer Buffer{ get; }

/// <summary> Packet Info </summary>

public RsgInfo Info{ get; }

/// <summary> File Endianness </summary>

public Endianness Endian{ get; }

/// <summary> Resources Map </summary>

public ResourceMap ResMap{ get; }

// ctor

public LoadedRsg(string groupId, NativeBuffer buffer, RsgInfo info, 
                 Endianness endian, ResourceMap resMap)
{
GroupName = groupId;
Buffer = buffer;

Info = info;
Endian = endian;

ResMap = resMap;
}

}

}