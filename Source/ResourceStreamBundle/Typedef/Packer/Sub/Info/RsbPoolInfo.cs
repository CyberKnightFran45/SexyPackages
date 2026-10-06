using System.Text.Json.Serialization;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB Pool Info (JSON model) </summary>

public class RsbPoolInfo
{
/// <summary> Pool Name </summary>

public string PoolName{ get; set; }

/// <summary> Number of instances </summary>

public uint NumInstances{ get; set; }

/// <summary> Pool flags </summary>

public uint Flags{ get; set; }

// ctor

public RsbPoolInfo()
{
}

// ctor 2

public RsbPoolInfo(string name, uint numInstances, uint flags)
{
PoolName = name;

NumInstances = numInstances;
Flags = flags;
}

public static readonly JsonSerializerContext Context = new RsbPoolContext(JsonSerializer.Options);
}

// Context for serialization

[JsonSerializable(typeof(RsbPoolInfo) ) ]

public partial class RsbPoolContext : JsonSerializerContext
{
}

}