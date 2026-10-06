namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Constants used in the RSB Format. </summary>

internal static class RsbConstants
{
/// <summary> RSB identifier. </summary>

public const uint MAGIC = 0x72736231;

/// <summary> RSB identifier (Big Endian) </summary>

public const uint MAGIC_BE = 0x31627372;

/// <summary> SMF identifier </summary>

public const uint MAGIC_ZLIB = 0xDEADFED4;

/// <summary> RSLB identifier </summary>

public const uint MAGIC_LZMA = 0x424C5352;

/// <summary> Config source name </summary>

public const string SRC_CONFIG = "BundleInfo.json";

/// <summary> Manifest source name </summary>

public const string SRC_MANIFEST = "ManifestInfo.json";

/// <summary> GroupsInfo source file </summary>

public const string SRC_GROUPS_INFO = "GroupsInfo.json";

/// <summary> Pools source name </summary>

public const string SRC_POOLS = "Pools.json";

/// <summary> Composite source name </summary>

public const string SRC_COMPOSITE = "CompositeInfo.json";

/// <summary> Groups source dir </summary>

public const string SRC_GROUPS = "Groups";

/// <summary> GroupsResources source dir </summary>

public const string SRC_GROUPS_RESOURCES = "GroupsResources";
}

}