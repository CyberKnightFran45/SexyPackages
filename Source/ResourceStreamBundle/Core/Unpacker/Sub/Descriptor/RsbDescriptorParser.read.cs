using System.IO;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB Descriptor Reader </summary>

internal static partial class RsbDescriptorParser
{
#region ============== DESCRIPTOR READER ==============

// Check if RSB has valid descriptors

private static bool HasDescriptors(uint tableOffset, uint entrySize, uint count)
{
return tableOffset != 0 && entrySize != 0 && count != 0;
}

// Read Pool Descriptors

public static RsbPoolDescriptor[] ReadPools(Stream reader,
                                            uint tableOffset,
											uint entrySize,
                                            uint count,
											Endianness endian)
{

if(!HasDescriptors(tableOffset, entrySize, count) )
return [];

reader.Seek(tableOffset, SeekOrigin.Begin);

uint sectionLen = entrySize * count;
using var buffer = reader.ReadPtr(sectionLen);

RsbPoolDescriptor[] pools = new RsbPoolDescriptor[count];

for(uint i = 0; i < count; i++)
{
var view = buffer.GetView(i * entrySize, (int)entrySize);

pools[i] = RsbPoolDescriptor.Read(view, endian);
}

return pools;
}

// Read Group Descriptors

public static RsbGroupDescriptor[] ReadGroups(Stream reader,
                                              uint tableOffset,
											  uint entrySize,
                                              uint count,
											  Endianness endian)
{

if(!HasDescriptors(tableOffset, entrySize, count) )
return [];

reader.Seek(tableOffset, SeekOrigin.Begin);

uint sectionLen = entrySize * count;
using var buffer = reader.ReadPtr(sectionLen);

RsbGroupDescriptor[] groups = new RsbGroupDescriptor[count];

for(uint i = 0; i < count; i++)
{
var view = buffer.GetView(i * entrySize, (int)entrySize);

groups[i] = RsbGroupDescriptor.Read(view, endian);
}

return groups;
}

// Read Composite Descriptors

public static RsbCompositeDescriptor[] ReadComposites(Stream reader,
                                                      uint tableOffset,
													  uint entrySize, 
                                                      uint count,
													  Endianness endian)
{

if(!HasDescriptors(tableOffset, entrySize, count) )
return [];

reader.Seek(tableOffset, SeekOrigin.Begin);

uint sectionLen = entrySize * count;
using var buffer = reader.ReadPtr(sectionLen);

RsbCompositeDescriptor[] composites = new RsbCompositeDescriptor[count];

for(uint i = 0; i < count; i++)
{
var view = buffer.GetView(i * entrySize, (int)entrySize);

composites[i] = RsbCompositeDescriptor.Read(view, endian);
}

return composites;
}

// Read Composite Descriptors (V3+)

public static RsbCompositeDescriptorV3[] ReadCompositesV3(Stream reader,
                                                          uint tableOffset,
														  uint entrySize,
                                                          uint count,
														  Endianness endian)
{

if(!HasDescriptors(tableOffset, entrySize, count) )
return [];

reader.Seek(tableOffset, SeekOrigin.Begin);

uint sectionLen = entrySize * count;
using var buffer = reader.ReadPtr(sectionLen);

RsbCompositeDescriptorV3[] composites = new RsbCompositeDescriptorV3[count];

for(uint i = 0; i < count; i++)
{
var view = buffer.GetView(i * entrySize, (int)entrySize);

composites[i] = RsbCompositeDescriptorV3.Read(view, endian);
}

return composites;
}

// Read Texture Descriptors

public static RsbTextureDescriptor[] ReadPtxInfo(Stream reader,
                                                 uint tableOffset,
												 uint entrySize,
                                                 uint count,
												 Endianness endian)

{

if(!HasDescriptors(tableOffset, entrySize, count) )
return [];

reader.Seek(tableOffset, SeekOrigin.Begin);

uint sectionLen = entrySize * count;
using var buffer = reader.ReadPtr(sectionLen);

RsbTextureDescriptor[] textures = new RsbTextureDescriptor[count];

for(uint i = 0; i < count; i++)
{
var view = buffer.GetView(i * entrySize, (int)entrySize);

textures[i] = RsbTextureDescriptor.Read(view, endian);
}

return textures;
}

#endregion
}

}
