using System.Text.Json.Serialization;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Manifest Group Info (JSON model) </summary>

public class ManifestGroupInfo_Json
{
/// <summary> Res ID </summary>

public uint Res{ get; set; }

/// <summary> Locale language </summary>

public string Localization{ get; set; }

/// <summary> Resources Map </summary>

public RsbManifestResMap Resources{ get; set; } = new();

// ctor

public ManifestGroupInfo_Json()
{
}

public static readonly JsonSerializerContext Context = new ManifestGroupContext(JsonSerializer.Options);
}

// Context for serialization

[JsonSerializable(typeof(ManifestGroupInfo_Json) ) ]

[JsonSerializable(typeof(ManifestResInfo_Json) ) ]

public partial class ManifestGroupContext : JsonSerializerContext
{
}

}