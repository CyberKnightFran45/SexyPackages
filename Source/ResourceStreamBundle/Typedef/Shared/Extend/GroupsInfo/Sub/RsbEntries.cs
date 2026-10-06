using System.Text.Json.Serialization;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Stores info about all Groups and Resources inside a RSB </summary>

public class RsbEntries
{
/// <summary> RSB platform </summary>

public RsbPlatform? Platform{ get; set; }

/// <summary> Groups mapped by name </summary>

public GroupMap Groups{ get; set; }

// ctor

public RsbEntries()
{
Groups = new();
}

// ctor 2

public RsbEntries(GroupMap groups, RsbPlatform? platform = null)
{
Groups = groups;
Platform = platform;
}

// Attempts to get group info by name

public bool TryGetInfo(string groupName, out RsbGroupInfo groupInfo)
{
return Groups.TryGetValue(groupName, out groupInfo);
}

// Get Platform flags

public RsbPlatform GetPlatform() => RsbUtils.GetPtxPlatform(Platform);

public static readonly JsonSerializerContext Context = new RsbEntriesContext(JsonSerializer.Options);
}

// Context for serialization

[JsonSerializable(typeof(RsbEntries) ) ]

[JsonSerializable(typeof(RsbGroupInfo) ) ]

public partial class RsbEntriesContext : JsonSerializerContext
{
}

}