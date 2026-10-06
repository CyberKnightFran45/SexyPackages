namespace SexyPackages.MarmaladeDZip
{
/// <summary> Constants used in the DZip Format. </summary>

public static class DzConstants
{
/// <summary> DZ identifier </summary>

public const uint MAGIC = 0x4454525A;

/// <summary> The Version of a DZip File. </summary>

public const byte VERSION = 0;

/// <summary> Encoding used </summary>

public const EncodingType ENCODING = EncodingType.ANSI;

/// <summary> Identifier that marks the End of Chunks section </summary>

public const ushort CHUNKS_END = 0xFFFF;

/// <summary> Config source name </summary>

public const string SRC_CONFIG = "DzInfo.json";
}

}