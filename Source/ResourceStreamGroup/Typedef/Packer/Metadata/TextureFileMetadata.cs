using System.Text.Json.Serialization;

namespace SexyPackages.ResourceStreamGroup
{
/// <summary> Metadata for a Texture inside a RSG </summary>

public class TextureFileMetadata
{
/// <summary> Gets or Sets the Texture Index </summary>

public uint TextureIndex{ get; set; }

/// <summary> Gets or Sets the Texture Width </summary>

public uint Width{ get; set; }

/// <summary> Gets or Sets the Texture Height </summary>

public uint Height{ get; set; }

// ctor

public TextureFileMetadata()
{
}

// ctor 2

public TextureFileMetadata(uint index, uint width, uint height)
{
TextureIndex = index;

Width = width;
Height = height;
}

public static readonly JsonSerializerContext Context = new RsgTextureContext(JsonSerializer.Options);
}

// Context for serialization

[JsonSerializable(typeof(TextureFileMetadata) ) ]

public partial class RsgTextureContext : JsonSerializerContext
{
}

}