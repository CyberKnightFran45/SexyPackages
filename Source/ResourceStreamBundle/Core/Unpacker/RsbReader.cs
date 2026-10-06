using System;
using System.IO;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Read RSB metadata </summary>

internal static class RsbReader
{
// Read info

private static RsbInfo ReadInfo(Stream reader, out Endianness endian)
{
TraceLogger.WriteActionStart("Reading header...");

uint flags = reader.ReadUInt32();

switch(flags)
{
case RsbConstants.MAGIC:
endian = Endianness.LittleEndian;
break;

case RsbConstants.MAGIC_BE:
endian = Endianness.BigEndian;
break;

default:
var expectedFlags = $"0x{RsbConstants.MAGIC:X8} | 0x{RsbConstants.MAGIC_BE:X8} (BigEndian)";

throw new Exception($"Invalid ResBundle identifier: 0x{flags:X8}, expected: {expectedFlags}");
}

var info = RsbInfo.Read(reader, endian);

var majVer = info.MajorVersion;
var minVer = info.MinorVersion;

if(!Enum.IsDefined(majVer) || !Enum.IsDefined(minVer) )
TraceLogger.WriteWarn($"Unknown version: v{(uint)majVer}.{(uint)minVer}");

TraceLogger.WriteActionEnd();

TraceLogger.WriteInfo($"ResGroups: {info.GroupCount}");

return info;
}

// Read Manifest info

private static RsbManifest ReadManifestInfo(Stream reader, in RsbInfo info, Endianness endian)
{
TraceLogger.WriteActionStart("Reading Manifest Info...");
var manifest = RsbManifestHandler.Read(reader, info, endian);

TraceLogger.WriteActionEnd();

return manifest;
}

// Read SubGroup IDs

private static string[] ReadSubGroupIDs(Stream reader, in RsbInfo info, Endianness endian)
{
TraceLogger.WriteActionStart("Reading SubGroup IDs...");

uint mapOffset = info.GroupMapOffset; 
uint mapSize = info.GroupMapSize;
uint groupCount = info.GroupCount;

var groupIDs = RsbCompiledMap.Read(reader, mapOffset, mapSize, groupCount, endian, "SubGroup");

TraceLogger.WriteActionEnd();

return groupIDs;
}

// Read Composite IDs

private static string[] ReadCompositeIDs(Stream reader, in RsbInfo info, Endianness endian)
{
TraceLogger.WriteActionStart("Reading Composite IDs...");

uint mapOffset = info.CompositeMapOffset; 
uint mapSize = info.CompositeMapSize;
uint groupCount = info.CompositeCount;

var compositeIDs = RsbCompiledMap.Read(reader, mapOffset, mapSize, groupCount, endian, "CompositeGroup");

TraceLogger.WriteActionEnd();

return compositeIDs;
}

// Read pool info

private static RsbPoolDescriptor[] ReadPoolInfo(Stream reader, in RsbInfo info, Endianness endian)
{
TraceLogger.WriteActionStart("Reading Pool Info...");

uint descriptorOffset = info.PoolDescriptorOffset; 
uint descriptorSize = info.PoolDescriptorSize;
uint poolCount = info.PoolCount;

var poolInfo = RsbDescriptorParser.ReadPools(reader, descriptorOffset, descriptorSize, poolCount, endian);

TraceLogger.WriteActionEnd();

return poolInfo;
}

// Read Composites (Core logic)

private static RsbCompositeData GetCompositeData(Stream reader, in RsbInfo info, Endianness endian)
{
uint descriptorOffset = info.CompositeDescriptorOffset; 
uint descriptorSize = info.CompositeDescriptorSize;
uint compositeCount = info.CompositeCount;

RsbCompositeDescriptor[] composites = null;
RsbCompositeDescriptorV3[] compositesV3 = null;

if(info.MajorVersion >= RsbMajorVersion.V3)
{

compositesV3 = RsbDescriptorParser.ReadCompositesV3(reader,
                                                    descriptorOffset,
                                                    descriptorSize,
                                                    compositeCount,
                                                    endian);

}

else
{

composites = RsbDescriptorParser.ReadComposites(reader,
                                                descriptorOffset,
                                                descriptorSize,
                                                compositeCount,
                                                endian);


}

return new(composites, compositesV3);
}

// Read Composite info

private static void ReadCompositeInfo(Stream reader,
                                      in RsbInfo info,
                                      Endianness endian,
                                      out RsbCompositeData composites)
{
TraceLogger.WriteActionStart("Reading Composite Info...");
composites = GetCompositeData(reader, info, endian);

TraceLogger.WriteActionEnd();
}

// Read Group info

private static RsbGroupDescriptor[] ReadGroupInfo(Stream reader, in RsbInfo info, Endianness endian)
{
TraceLogger.WriteActionStart("Reading Group Info...");

uint descriptorOffset = info.GroupDescriptorOffset; 
uint descriptorSize = info.GroupDescriptorSize;
uint groupCount = info.GroupCount;

var groupInfo = RsbDescriptorParser.ReadGroups(reader, descriptorOffset, descriptorSize, groupCount, endian);

TraceLogger.WriteActionEnd();

return groupInfo;
}

// Read ptx info

private static RsbTextureDescriptor[] ReadPtxInfo(Stream reader, in RsbInfo info, Endianness endian)
{
TraceLogger.WriteActionStart("Reading Global Texture Descriptors...");

uint descriptorOffset = info.PtxDescriptorOffset;
uint descriptorSize = info.PtxDescriptorSize;
uint ptxCount = info.TextureCount;

var ptxInfo = RsbDescriptorParser.ReadPtxInfo(reader, descriptorOffset, descriptorSize, ptxCount, endian);

TraceLogger.WriteActionEnd();

return ptxInfo;
}

// Init context

public static RsbUnpackerContext InitCtx(Stream source, ref int step)
{
TraceLogger.WriteStep(step, "Read RSB Info");
var info = ReadInfo(source, out var endian);

RsbTableSet rsbTable = new(null);
RsbManifest manifest = null;

if(info.ManifestGroupOffset > 0)
{
step++;

TraceLogger.WriteStep(step, "Read Resouces Manifest");
manifest = ReadManifestInfo(source, info, endian);
}

step++;

TraceLogger.WriteStep(step, "Read Shell IDs");

var groupIDs = ReadSubGroupIDs(source, info, endian);
var compositeIDs = ReadCompositeIDs(source, info, endian);

rsbTable.SetShellIDs(groupIDs, compositeIDs);

step++;

TraceLogger.WriteStep(step, "Read Metadata");

var poolInfo = ReadPoolInfo(source, info, endian);
ReadCompositeInfo(source, info, endian, out var composites);

var groupInfo = ReadGroupInfo(source, info, endian);
var ptxInfo = ReadPtxInfo(source, info, endian);

rsbTable.SetMetadata(poolInfo, composites, groupInfo, ptxInfo);

return new(endian, info, rsbTable, manifest);
}

}

}