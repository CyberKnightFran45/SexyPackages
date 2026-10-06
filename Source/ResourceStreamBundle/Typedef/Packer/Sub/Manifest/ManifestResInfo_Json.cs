using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Manifest Res Info (JSON model) </summary>

public class ManifestResInfo_Json
{
/// <summary> Res Type </summary>

public uint Type{ get; set; }

/// <summary> Path to res file </summary>

public string Path{ get; set; }

/// <summary> Image Properties (optional) </summary>

public ManifestResImageProperty_Json ImageProperties{ get; set; }

/// <summary> Universal Properties (optional) </summary>

public Dictionary<string, string> UniversalProperties{ get; set; }

// ctor

public ManifestResInfo_Json()
{
}

// ctor 2

public ManifestResInfo_Json(uint type, string path, ManifestResImageProperty_Json imgProps,
                            Dictionary<string, string> universalProps)
{
Type = type;
Path = path;

ImageProperties = imgProps;
UniversalProperties = universalProps;
}

public static readonly JsonSerializerContext Context = new ManifestResContext(JsonSerializer.Options);
}

// Context for serialization

[JsonSerializable(typeof(ManifestResInfo_Json) ) ]

[JsonSerializable(typeof(ManifestResImageProperty_Json) ) ]

public partial class ManifestResContext : JsonSerializerContext
{
}

}