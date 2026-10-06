using System.Text.Json.Serialization;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Manifest Composite Info (JSON model) </summary>

public class ManifestCompositeInfo_Json
{
/// <summary> SubGroups Map </summary>

public RsbManifestGroupMap SubGroups{ get; set; } = new();

// ctor

public ManifestCompositeInfo_Json()
{
}

// ctor 2

public ManifestCompositeInfo_Json(RsbManifestGroupMap subGroups)
{
SubGroups = subGroups;
}

public static readonly JsonSerializerContext Context = new ManifestCompositeContext(JsonSerializer.Options);
}

// Context for serialization

[JsonSerializable(typeof(ManifestCompositeInfo_Json) ) ]

[JsonSerializable(typeof(ManifestGroupInfo_Json) ) ]

[JsonSerializable(typeof(ManifestResInfo_Json) ) ]
[JsonSerializable(typeof(ManifestResImageProperty_Json) ) ]

public partial class ManifestCompositeContext : JsonSerializerContext
{
}

}