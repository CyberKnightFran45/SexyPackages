using System.IO;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB Descriptor Writer </summary>

internal static partial class RsbDescriptorParser
{
#region ============== DESCRIPTOR WRITER ==============

// Write Pool Descriptors

public static void WritePools(Stream writer, RsbPoolDescriptor[] pools, Endianness endian)
{

foreach(var p in pools)
p.Write(writer, endian);

}

// Write Group Descriptors

public static void WriteGroups(Stream writer, RsbGroupDescriptor[] groups,
                               uint entrySize, Endianness endian)
{

foreach(var g in groups)
g.Write(writer, entrySize, endian);

}

// Write Composite Descriptors

public static void WriteComposites(Stream writer, RsbCompositeDescriptor[] composites, Endianness endian)
{

foreach(var c in composites)
c.Write(writer, endian);

}

// Write Composite Descriptors (V3+)

public static void WriteCompositesV3(Stream writer, RsbCompositeDescriptorV3[] composites, Endianness endian)
{

foreach(var c3 in composites)
c3.Write(writer, endian);

}

// Write Texture Descriptors

public static void WritePtxInfo(Stream writer, RsbTextureDescriptor[] textures,
                                uint entrySize, Endianness endian)
{

foreach(var ptx in textures)
ptx.Write(writer, entrySize, endian);

}

#endregion
}

}