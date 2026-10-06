using System.Collections.Generic;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB Packer Context </summary>

internal sealed class RsbPackerContext
{
/// <summary> RSB Config </summary>

public RsbParams Params{ get; }

/// <summary> List of Loaded RSGs </summary>

public List<LoadedRsg> Rsgs{ get; }

/// <summary> RSB Tables </summary>

public RsbTableSet Tables{ get; }

/// <summary> RSB Manifest (Optional) </summary>

public RsbManifest Manifest{ get; }

// ctor

public RsbPackerContext(RsbParams cfg,
                        List<LoadedRsg> rsgs,
						RsbTableSet tables,
						RsbManifest manifest = null)
{
Params = cfg;
Rsgs = rsgs;

Tables = tables;
Manifest = manifest;
}

}

}