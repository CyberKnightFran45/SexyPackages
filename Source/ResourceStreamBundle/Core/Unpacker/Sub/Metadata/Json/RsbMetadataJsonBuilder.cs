using System;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB Metadata Builder (for JSON) </summary>

internal static class RsbMetadataJsonBuilder
{
// Resolve Pool Name (single)

private static unsafe string ResolvePoolName(in RsbPoolDescriptor desc, uint index)
{

fixed(byte* namePtr = desc.Name)
{
string name = UnsafeStringHelper.ExtractString(namePtr, 128);

return string.IsNullOrEmpty(name) ? $"Pool_{index}" : name;
}

}

// Resolve a pool's name for a given group index

internal static string ResolvePoolName(RsbPoolDescriptor[] poolInfo, uint poolIndex)
{
int poolCount = poolInfo.Length;

if(poolIndex < poolCount)
return ResolvePoolName(poolInfo[poolIndex], poolIndex);

var msg = $"Invalid pool index: '{poolIndex}' @ RsbPoolDescriptors. Expected range: [0 .. {poolCount - 1}]";

throw new ArgumentOutOfRangeException(nameof(poolIndex), poolIndex, msg);
}

// Build PoolInfo

public static RsbPoolInfo[] BuildPoolInfo(RsbPoolDescriptor[] descriptors)
{
int poolCount = descriptors.Length;

RsbPoolInfo[] pools = new RsbPoolInfo[poolCount];

for(uint i = 0; i < poolCount; i++)
{
var desc = descriptors[i];
string poolName = ResolvePoolName(desc, i);

pools[i] = new(poolName, desc.NumInstances, desc.Flags);
}

return pools;
}

// Resolve Composite Name

private static unsafe string ResolveCompositeName(in RsbCompositeDescriptor desc, string fallbackID)
{

fixed(byte* namePtr = desc.Name)
{
string name = UnsafeStringHelper.ExtractString(namePtr, 128);

return string.IsNullOrEmpty(name) ? fallbackID : name;
}

}

// Build CompositeInfo

public static CompositeMap BuildCompositeInfo(string[] compositeIDs,
                                              RsbCompositeDescriptor[] descriptors, 
                                              string[] groupIDs)
{
int compositeCount = descriptors.Length;

CompositeMap compositesMap = new(compositeCount);

for(int i = 0; i < compositeCount; i++)
{
var desc = descriptors[i];

string compositeName = ResolveCompositeName(desc, compositeIDs[i] );
RsbCompositeInfo compositeInfo = new(desc, groupIDs);

compositesMap.Add(compositeName, compositeInfo);
}

return compositesMap;
}

// Resolve Composite Name (V3)

private static unsafe string ResolveCompositeName(in RsbCompositeDescriptorV3 desc, string fallbackID)
{

fixed(byte* namePtr = desc.Name)
{
string name = UnsafeStringHelper.ExtractString(namePtr, 128);

return string.IsNullOrEmpty(name) ? fallbackID : name;
}

}

// Buil CompositeInfo (V3)

public static CompositeMap BuildCompositeInfoV3(string[] compositeIDs,
                                                RsbCompositeDescriptorV3[] descriptors, 
                                                string[] groupIDs)
{
int compositeCount = descriptors.Length;

CompositeMap compositesMap = new(compositeCount);

for(int i = 0; i < compositeCount; i++)
{
var desc = descriptors[i];

string compositeName = ResolveCompositeName(desc, compositeIDs[i] );
RsbCompositeInfo compositeInfo = new(desc, groupIDs);

compositesMap.Add(compositeName, compositeInfo);
}

return compositesMap;
}

}

}