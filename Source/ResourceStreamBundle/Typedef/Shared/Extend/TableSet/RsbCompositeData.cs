namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB Composite Container </summary>

internal sealed class RsbCompositeData
{
/// <summary> Composite Info (V1-V2) </summary>

public RsbCompositeDescriptor[] Info{ get; }

/// <summary> Composite Info (V3+) </summary>

public RsbCompositeDescriptorV3[] InfoV3{ get; }

/// <summary> Wheter RSB uses V3 descriptors or not </summary>
        
public bool IsV3 => InfoV3 != null;

/// <summary> Gets the number of Composite entries </summary>
        
public int Count => IsV3 ? InfoV3.Length : Info.Length;

// ctor

public RsbCompositeData(RsbCompositeDescriptor[] info, RsbCompositeDescriptorV3[] infoV3)
{
Info = info;
InfoV3 = infoV3;
}

}
	
}