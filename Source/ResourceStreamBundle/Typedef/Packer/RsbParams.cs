using System.Text.Json.Serialization;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Defines some Params for Packing a RSB Stream </summary>

public class RsbParams
{
/// <summary> File Endianness </summary>

public Endianness Endian{ get; set; }

/// <summary> Rsb Version (Major) </summary>

[JsonConverter(typeof(JsonNumberEnumConverter<RsbMajorVersion>) ) ]

public RsbMajorVersion MajorVersion{ get; set; }

/// <summary> Rsb Version (Minor) </summary>

[JsonConverter(typeof(JsonNumberEnumConverter<RsbMinorVersion>) ) ]

public RsbMinorVersion MinorVersion{ get; set; }

/// <summary> Determines if RSB Manifest should be threated as a JSON instead of a group </summary>

public bool ManifestAsJson{ get; set; }

/// <summary> Wheter to use Global file offsets or not </summary>

public bool UseGlobalOffsets{ get; set; }

/// <summary> Wheter to use external ResGroups (RSG files) </summary>

public bool UseExternalRsgs{ get; set; }

/// <summary> Ptx descriptor type </summary>

public TextureDescriptorType PtxDescriptorType{ get; set; }

/// <summary> Compression flags </summary>

public RsbCompressionFlags? CompressionFlags{ get; set; }

/// <summary> Wheter to add .smf extension to the compressed file name </summary>

public bool? AddSmfExtension{ get; set; }

/// <summary> Whether to encrypt rtons under PACKAGES or LUA_PACKAGES </summary>

public bool? EncryptPackages{ get; set; }

// ctor

public RsbParams()
{
}

// ctor 2

public RsbParams(Endianness endian, bool useExternalRsgs, in RsbInfo info)
{
Endian = endian;

MajorVersion = info.MajorVersion;
MinorVersion = info.MinorVersion;

ManifestAsJson = info.ManifestGroupOffset > 0;
UseGlobalOffsets = info.GlobalMapSize > 0;

UseExternalRsgs = useExternalRsgs;
PtxDescriptorType = (TextureDescriptorType)info.PtxDescriptorSize;
}

// Set extra info

public void SetExtraInfo(RsbCompressionFlags? flags, bool? addSmfExt, bool? encryptPackages)
{
CompressionFlags = flags;

AddSmfExtension = addSmfExt;
EncryptPackages = encryptPackages;
}

public static readonly JsonSerializerContext Context = new RsbParamsContext(JsonSerializer.Options);
}

// Context for serialization

[JsonSerializable(typeof(RsbParams) ) ]

public partial class RsbParamsContext : JsonSerializerContext
{
}

}