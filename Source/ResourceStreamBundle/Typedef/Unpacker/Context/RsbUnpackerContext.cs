namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB Unpacker Context </summary>

internal sealed class RsbUnpackerContext
{
/// <summary> File Endianness </summary>

public Endianness Endian{ get; }

/// <summary> Bundle Info </summary>

public RsbInfo BundleInfo{ get; }

/// <summary> RSB Tables </summary>

public RsbTableSet Tables{ get; }

/// <summary> RSB Manifest (Optional) </summary>

public RsbManifest Manifest{ get; }

// ctor

public RsbUnpackerContext(Endianness endian,
                          RsbInfo info,
						  RsbTableSet tables,
                          RsbManifest manifest = null)
{
Endian = endian;

BundleInfo = info;
Tables = tables;

Manifest = manifest;
}

}

}