using System.Text.Json.Serialization;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB Texture Info (JSON model) </summary>

public class RsbTextureInfo
{
/// <summary> Texture Width </summary>

public uint Width{ get; set; }

/// <summary> Texture Height </summary>

public uint Height{ get; set; }

/// <summary> Texture Pitch </summary>

public uint Pitch{ get; set; }

/// <summary> PTX Format </summary>

public uint Format{ get; set; }

/// <summary> Amount of bytes used in Alpha Channel (extend1) </summary>

public uint? AlphaSize{ get; set; }

/// <summary> Image scale (extend2) </summary>

public uint? Scale{ get; set; }

// ctor

public RsbTextureInfo()
{
}

// ctor 2

public RsbTextureInfo(in RsbTextureDescriptor descriptor, uint descriptorSize)
{
Width = descriptor.Width;
Height = descriptor.Height;

Pitch = descriptor.Pitch;
Format = descriptor.Format;

AlphaSize = GetUIntOrDefault(descriptorSize, 20, descriptor.AlphaSize);
Scale = GetUIntOrDefault(descriptorSize, 24, descriptor.Scale);
}

// Get value or null

private static uint? GetUIntOrDefault(uint descriptorSize, uint minSize, uint val)
{
return descriptorSize >= minSize && val > 0 ? val : null;
}

public static readonly JsonSerializerContext Context = new RsbTextureContext(JsonSerializer.Options);
}

// Context for serialization

[JsonSerializable(typeof(RsbTextureInfo) ) ]

public partial class RsbTextureContext : JsonSerializerContext
{
}

}