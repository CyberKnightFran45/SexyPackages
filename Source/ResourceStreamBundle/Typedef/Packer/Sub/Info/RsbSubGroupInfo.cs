using System.Text.Json.Serialization;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB SubGroup Info (JSON model) </summary>

public class RsbSubGroupInfo
{
/// <summary> Art resolution </summary>

public uint ArtResolution{ get; set; }

/// <summary> Loc name </summary>

public string Localization{ get; set; }

// ctor

public RsbSubGroupInfo()
{
}

// ctor 2

public RsbSubGroupInfo(uint artRes, string locale = null)
{
ArtResolution = artRes;
Localization = locale;
}

public static readonly JsonSerializerContext Context = new RsbChildContext(JsonSerializer.Options);
}

// Context for serialization

[JsonSerializable(typeof(RsbSubGroupInfo) ) ]

public partial class RsbChildContext : JsonSerializerContext
{
}

}