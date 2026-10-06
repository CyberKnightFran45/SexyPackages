using System.Collections.Generic;
using System.Text.Json.Serialization;
using SexyPackages.ResourceStreamGroup;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB Group Info (JSON model) </summary>

public class RsbGroupInfo
{
/// <summary> Compression flags </summary>

public GroupCompressionFlags CompressionFlags{ get; set; }

/** <summary> Name of the pool this group belongs to. </summary>

<remarks> This group gets its own dedicated auto-pool if ID is not set </remarks> **/

public string PoolName{ get; set; }

/// <summary> Common files List (Residents) </summary>

public List<string> ResFiles{ get; set; }

/// <summary> Texture files List (GPU cache) </summary>

public PtxGlobalMap Textures{ get; set; }

// ctor

public RsbGroupInfo()
{
}

// ctor 2

public RsbGroupInfo(GroupCompressionFlags flags,
                    List<string> resFiles,
					PtxGlobalMap textures,
                    string poolName = null)
{
CompressionFlags = flags;
PoolName = poolName;

ResFiles = resFiles;
Textures = textures;
}

public static readonly JsonSerializerContext Context = new RsbGroupContext(JsonSerializer.Options);
}

// Context for serialization

[JsonSerializable(typeof(RsbGroupInfo) ) ]

[JsonSerializable(typeof(RsbTextureInfo) ) ]

public partial class RsbGroupContext : JsonSerializerContext
{
}

}