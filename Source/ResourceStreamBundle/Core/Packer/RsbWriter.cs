using System.Collections.Generic;
using System.IO;
using SexyPackages.ResourceStreamGroup;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Write RSB metadata </summary>

internal static class RsbWriter
{
// Write global map core

private static void WriteGlobalMapCore(Stream target, List<(string, uint)> entries, Endianness endian,
                                       out uint offset, out uint size)
{
offset = (uint)target.Position;

using MemoryStream mapStream = new();
RsbCompiledMap.Write(mapStream, entries, endian);

size = (uint)mapStream.Length;

mapStream.Seek(0, SeekOrigin.Begin);

FileManager.Process(mapStream, target);
}

// Write global map

private static void WriteGlobalMap(Stream target, List<(string, uint)> entries, Endianness endian,
                                   out uint offset, out uint size)
{
TraceLogger.WriteActionStart("Writing Global Map...");
WriteGlobalMapCore(target, entries, endian, out offset, out size);

TraceLogger.WriteActionEnd();
}

// Write compiled map

private static void WriteCompiledMap(Stream target, string[] ids, Endianness endian,
                                     out uint offset, out uint size)
{
offset = (uint)target.Position;

using MemoryStream mapStream = new();
RsbCompiledMap.Write(mapStream, ids, endian);

size = (uint)mapStream.Length;

mapStream.Seek(0, SeekOrigin.Begin);

FileManager.Process(mapStream, target);
}

// Write SubGroup IDs

private static void WriteSubGroupIDs(Stream target, string[] ids, Endianness endian,
                                     out uint offset, out uint size)
{
TraceLogger.WriteActionStart("Writing SubGroup IDs...");
WriteCompiledMap(target, ids, endian, out offset, out size);

TraceLogger.WriteActionEnd();
}

// Write Composite IDs

private static void WriteCompositeIDs(Stream target, string[] ids, Endianness endian,
                                      out uint offset, out uint size)
{
TraceLogger.WriteActionStart("Writing Composite IDs...");
WriteCompiledMap(target, ids, endian, out offset, out size);

TraceLogger.WriteActionEnd();
}

// Write Composite info

private static uint WriteCompositeInfo(Stream target, Endianness endian, RsbCompositeData composites)
{
TraceLogger.WriteActionStart("Writing Composite Info...");

uint descriptorSize;

if(composites.IsV3)
{
descriptorSize = 1156;

RsbDescriptorParser.WriteCompositesV3(target, composites.InfoV3, endian);
}

else
{
descriptorSize = 644;

RsbDescriptorParser.WriteComposites(target, composites.Info, endian);
}

TraceLogger.WriteActionEnd();

return descriptorSize;
}

// Write Group descriptors

private static void WriteGroupInfo(Stream target, RsbGroupDescriptor[] groups,
                                   uint descriptorSize, Endianness endian)
{
TraceLogger.WriteActionStart("Writing Group Info...");	
RsbDescriptorParser.WriteGroups(target, groups, descriptorSize, endian);

TraceLogger.WriteActionEnd();
}

// Write Pool descriptors

private static void WritePoolInfo(Stream target, RsbPoolDescriptor[] pools, Endianness endian)
{
TraceLogger.WriteActionStart("Writing Pool Info...");	
RsbDescriptorParser.WritePools(target, pools, endian);

TraceLogger.WriteActionEnd();
}

// Write Ptx descriptors

private static void WritePtxInfo(Stream target, RsbTextureDescriptor[] info, uint descriptorSize,
                                 Endianness endian)
{
TraceLogger.WriteActionStart("Writing Global Texture Descriptors...");	
RsbDescriptorParser.WritePtxInfo(target, info, descriptorSize, endian);

TraceLogger.WriteActionEnd();
}

// Align stream

private static uint Align(Stream target, uint sectionEnd)
{
var padding = RsgHelper.ComputePadding( (int)sectionEnd, false);
uint aligned = sectionEnd + (uint)padding;

target.SetLength(aligned);
target.Seek(aligned, SeekOrigin.Begin);

return aligned;
}

// Pack RSGs (Core)

private static void PackRsgsCore(Stream target, RsbPackerContext ctx)
{
var rsgs = ctx.Rsgs;
var groups = ctx.Tables.Groups;

for(int i = 0; i < rsgs.Count; i++)
{
var rsg = rsgs[i];

var buffer = rsg.Buffer;
var view = buffer.GetView();

groups[i].Offset = (uint)target.Position;
groups[i].Size = (uint)view.Length;

target.Write(view);

buffer.Dispose();
}

}

// Pack RSGs

private static void PackRsgs(Stream target, RsbPackerContext ctx)
{
TraceLogger.WriteActionStart("Adding ResGroups...");
PackRsgsCore(target, ctx);

TraceLogger.WriteActionEnd();
}

// Write Manifest (Core)

private static void WriteManifestCore(Stream target, RsbManifest manifest, RsbMajorVersion majVersion, 
                                      Endianness endian, out uint groupOffset, out uint resOffset,
                                      out uint poolOffset)
{

RsbManifestHandler.Write(manifest, majVersion, endian, out var groupStream,
                         out var resStream, out var poolStream);

groupOffset = (uint)target.Position;

groupStream.Seek(0, SeekOrigin.Begin);
FileManager.Process(groupStream, target);

resOffset = (uint)target.Position;

resStream.Seek(0, SeekOrigin.Begin);
FileManager.Process(resStream, target);

poolOffset = (uint)target.Position;

poolStream.Seek(0, SeekOrigin.Begin);
FileManager.Process(poolStream, target);

groupStream.Dispose();
resStream.Dispose();

poolStream.Dispose();
}

// Write Manifest

private static void WriteManifest(Stream target, RsbPackerContext ctx, Endianness endian,
                                  out uint groupOffset, out uint resOffset,
                                  out uint poolOffset)
{
TraceLogger.WriteActionStart("Writing Manifest Info...");

WriteManifestCore(target, ctx.Manifest, ctx.Params.MajorVersion, endian,
                  out groupOffset, out resOffset, out poolOffset);

TraceLogger.WriteActionEnd();
}

// Write info

private static void WriteInfo(Stream target, in RsbInfo info, Endianness endian)
{
TraceLogger.WriteActionStart("Writing header...");

target.Seek(0, SeekOrigin.Begin);

target.WriteUInt32(RsbConstants.MAGIC, endian);
info.Write(target, endian);

TraceLogger.WriteActionEnd();
}

// Rewrite RSG info with updated sizes and offsets

private static void UpdateGroupInfo(Stream target, RsbPackerContext ctx, uint descriptorOffset,
                                    uint descriptorSize, Endianness endian)
{
TraceLogger.WriteActionStart("Updating RSG slots...");

long endPos = target.Position;
target.Seek(descriptorOffset, SeekOrigin.Begin);

RsbDescriptorParser.WriteGroups(target, ctx.Tables.Groups, descriptorSize, endian);
target.Seek(endPos, SeekOrigin.Begin);

TraceLogger.WriteActionEnd();
}

// Write RSB

public static void Write(Stream target, RsbPackerContext ctx, ref int step)
{
var cfg = ctx.Params;

var endian = cfg.Endian;
var tables = ctx.Tables;

bool isV3OrGreater = cfg.MajorVersion >= RsbMajorVersion.V3;
bool isV4OrGreater = cfg.MajorVersion >= RsbMajorVersion.V4;

bool useExternalRsgs = cfg.UseExternalRsgs;

target.Seek(112, SeekOrigin.Begin); // placeholder: will be filled later

RsbInfo info = new()
{
MajorVersion = cfg.MajorVersion,
MinorVersion = cfg.MinorVersion
};

TraceLogger.WriteStep(step, "Write Metadata");

WriteGlobalMap(target, tables.GlobalMapEntries, endian, out var globalMapOffset, out var globalMapSize);

WriteSubGroupIDs(target, tables.GroupIDs, endian, out var groupMapOffset, out var groupMapSize);

// Write Composite Info

info.CompositeDescriptorOffset = (uint)target.Position;
info.CompositeDescriptorSize = WriteCompositeInfo(target, endian, tables.Composites);

WriteCompositeIDs(target, tables.CompositeIDs, endian, out var compositeMapOffset,
                  out var compositeMapSize);

// Write Group Info

var rsgDescriptorOffset = (uint)target.Position;
uint groupDescriptorSize = isV3OrGreater ? 204u : 196u;

info.GroupDescriptorOffset = rsgDescriptorOffset;
info.GroupDescriptorSize = groupDescriptorSize;

WriteGroupInfo(target, tables.Groups, groupDescriptorSize, endian);

// Write Pool Info

info.PoolDescriptorOffset = (uint)target.Position;
info.PoolDescriptorSize = 152;

WritePoolInfo(target, tables.Pools, endian);

// Write Ptx info

var ptxDescriptorSize = (uint)cfg.PtxDescriptorType;

info.PtxDescriptorOffset = (uint)target.Position;
info.PtxDescriptorSize = ptxDescriptorSize;

WritePtxInfo(target, tables.Textures, ptxDescriptorSize, endian);

info.GlobalMapOffset = globalMapOffset;
info.GlobalMapSize = tables.GlobalMapEntries.Count > 0 ? (int)globalMapSize : -1;

info.GroupMapOffset = groupMapOffset;
info.GroupMapSize = groupMapSize;

info.CompositeMapOffset = compositeMapOffset;
info.CompositeMapSize = compositeMapSize;

var composites = tables.Composites;

info.GroupCount = (uint)tables.Groups.Length;
info.CompositeCount = (uint)composites.Count;
info.PoolCount = (uint)tables.Pools.Length;
info.TextureCount = (uint)tables.Textures.Length;

var headerSectionEnd = (uint)target.Position;

// Write Manifest

uint headerLenNoManifest;

if(ctx.Manifest is not null)
{

if(isV4OrGreater)
headerLenNoManifest = Align(target, headerSectionEnd);

else
{
headerLenNoManifest = headerSectionEnd;

target.SetLength(headerSectionEnd);
target.Seek(headerSectionEnd, SeekOrigin.Begin);
}

step++;

TraceLogger.WriteStep(step, "Write Resources Manifest");

WriteManifest(target, ctx, endian, out var manifestGroupOffset, out var manifestResOffset,
              out var manifestPoolOffset);

info.ManifestGroupOffset = manifestGroupOffset;
info.ManifestResOffset = manifestResOffset;
info.ManifestPoolOffset = manifestPoolOffset;

var alignedAfterManifest = Align(target, (uint)target.Position);

info.SectionLength = alignedAfterManifest;
}

else
{
headerLenNoManifest = Align(target, headerSectionEnd);

info.SectionLength = headerLenNoManifest;
}

info.HeaderSizeNoManifest = headerLenNoManifest;

if(!useExternalRsgs)
{
step++;

TraceLogger.WriteStep(step, "Pack ResGroups");

PackRsgs(target, ctx);
}

step++;

TraceLogger.WriteStep(step, "Finalize RSB Info");

WriteInfo(target, info, endian);

if(!useExternalRsgs)
UpdateGroupInfo(target, ctx, rsgDescriptorOffset, groupDescriptorSize, endian); // update slots

}

}

}