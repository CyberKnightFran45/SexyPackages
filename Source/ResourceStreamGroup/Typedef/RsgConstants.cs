namespace SexyPackages.ResourceStreamGroup
{
/// <summary> Constants used in the RSG Format. </summary>

public static class RsgConstants
{
/// <summary> RSG identifier. </summary>

public const uint MAGIC = 0x72736770;

/// <summary> RSG identifier (Big Endian) </summary>

public const uint MAGIC_BE = 0x70677372;

/// <summary> Config source name </summary>

public const string SRC_CONFIG = "PacketInfo.json";

/// <summary> Texture source name </summary>

public const string SRC_TEXTURE = "TextureInfo.json";

/// <summary> Resources source dir </summary>

public const string SRC_RESOURCES = "Resources";
}

}