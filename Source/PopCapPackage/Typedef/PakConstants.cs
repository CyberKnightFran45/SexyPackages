namespace SexyPackages.PopCapPackage
{
/// <summary> Constants used in the PAK Format. </summary>

public static class PakConstants
{
/// <summary> Standar PAK identifier (used in Xbox) </summary>

public const uint MAGIC = 0xBAC04AC0;

/// <summary> PAK identifier for Windows. </summary>

public const uint MAGIC_WINDOWS = 0x4D37BD37;

/// <summary> PAK identifier for AndroidTV (Zip file). </summary>

public const uint MAGIC_ANDROID_TV = 0x04034B50;


/// <remarks> Identifier for XMEM files (not supported, use xbdecompress instead) </remarks> */

public const uint MAGIC_XMEM = 0x0FF512ED;

/// <summary> The Version of a PAK File. </summary>

public const uint VERSION = 0;

/// <summary> The Key used for Encrypting Data. </summary>

public const byte KEY = 0xF7;

/// <summary> Identifier that marks the End of Entries section </summary>

public const byte ENTRIES_END = 0x80;

/// <summary> The Encoding used </summary>

public const EncodingType ENCODING = EncodingType.ANSI;

/// <summary> Config source name </summary>

public const string SRC_CONFIG = "PakInfo.json";

/// <summary> Resources source dir </summary>

public const string SRC_RESOURCES = "Resources";
}

}