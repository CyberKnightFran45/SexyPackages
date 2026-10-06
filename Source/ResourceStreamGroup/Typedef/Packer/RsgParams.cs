using System.Text.Json.Serialization;

namespace SexyPackages.ResourceStreamGroup
{
/// <summary> Defines some Params for Packing a RSG Stream </summary>

public class RsgParams
{
/// <summary> File Endianness </summary>

public Endianness Endian{ get; set; }

/// <summary> Rsg Version (Major) </summary>

[JsonConverter(typeof(JsonNumberEnumConverter<RsgMajorVersion>) ) ]

public RsgMajorVersion MajorVersion{ get; set; }

/// <summary> Rsg Version (Minor) </summary>

[JsonConverter(typeof(JsonNumberEnumConverter<RsgMinorVersion>) ) ]

public RsgMinorVersion MinorVersion{ get; set; }

/// <summary> Compression Flags </summary>

public GroupCompressionFlags CompressionFlags{ get; set; }

/// <summary> Whether to encrypt rtons under this group </summary>

public bool? EncryptRtons{ get; set; }

// ctor

public RsgParams()
{
}

// ctor 2

public RsgParams(Endianness endian, in RsgInfo info, bool? encryptRtons = null)
{
Endian = endian;

MajorVersion = info.MajorVersion;
MinorVersion = info.MinorVersion;

CompressionFlags = info.CompressionFlags;
EncryptRtons = encryptRtons;
}

public static readonly JsonSerializerContext Context = new RsgParamsContext(JsonSerializer.Options);
}

// Context for serialization

[JsonSerializable(typeof(RsgParams) ) ]

public partial class RsgParamsContext : JsonSerializerContext
{
}

}