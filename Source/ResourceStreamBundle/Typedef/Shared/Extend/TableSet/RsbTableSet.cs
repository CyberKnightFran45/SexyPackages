using System.Collections.Generic;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB Table Set </summary>

internal sealed class RsbTableSet
{
/// <summary> GlobalMap Entries </summary>

public List<(string Path, uint GroupIndex)> GlobalMapEntries{ get; private set; }

/// <summary> Group IDs </summary>

public string[] GroupIDs{ get; private set; }

/// <summary> Composite IDs </summary>

public string[] CompositeIDs{ get; private set; }

/// <summary> Pool descriptors </summary>

public RsbPoolDescriptor[] Pools{ get; private set; }

/// <summary> Composite Info </summary>

public RsbCompositeData Composites{ get; private set; }

/// <summary> Group descriptors </summary>

public RsbGroupDescriptor[] Groups{ get; private set; }

/// <summary> Ptx descriptors </summary>

public RsbTextureDescriptor[] Textures{ get; private set; }

// ctor

public RsbTableSet(List<(string Path, uint GroupIndex)> globalEntries)
{
GlobalMapEntries = globalEntries;
}

// Set Shell IDs

public void SetShellIDs(string[] groupIDs, string[] compositeIDs)
{
GroupIDs = groupIDs;
CompositeIDs = compositeIDs;
}

// Set metadata

public void SetMetadata(RsbPoolDescriptor[] pools,
                        RsbCompositeData composites,
                        RsbGroupDescriptor[] groups,
						RsbTextureDescriptor[] textures)
{
Pools = pools;
Composites = composites;

Groups = groups;
Textures = textures;
}

}

}