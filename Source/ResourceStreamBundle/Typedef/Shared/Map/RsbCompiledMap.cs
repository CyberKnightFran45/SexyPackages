using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Handles RSB CompiledMaps </summary>

internal static class RsbCompiledMap
{
// Warn on invalid Trie index

private static string WarnInvalidTrieIndex(string typeName, uint index, uint maxIndex, string id)
{
var rangeFlags = $"Range expected: [0..{maxIndex}]";

return $"Invalid {typeName} index: '{index}'. {rangeFlags} | ID: '{id}'";
}

// Read compiled map (Core logic)

private static string[] ReadCore(NativeBuffer buffer,
                                 uint offset,
                                 uint size,
                                 uint count,
                                 Endianness endian,
                                 string mapTypeName)
{
string[] idList = new string[count];

TrieReader.Traverse(buffer, offset, size, endian,

(buf, ref pos, id) =>
{
uint index = buf.GetUInt32(pos, endian);
pos += 4;

if(index < count)
idList[index] = id;

else
{
var warnMsg = WarnInvalidTrieIndex(mapTypeName, index, count - 1, id);

TraceLogger.WriteWarn(warnMsg);
}

}

);

return idList;
}

// Read compiled map

public static string[] Read(Stream reader,
                            uint offset,
                            uint size,
                            uint count,
                            Endianness endian,
                            string mapTypeName)
{

if(offset == 0 || size == 0 || count == 0)
return [];

reader.Seek(offset, SeekOrigin.Begin);

using var mapBuffer = reader.ReadPtr(size);

return ReadCore(mapBuffer, 0, size, count, endian, mapTypeName);
}

// Write map (pairs)

public static void Write(Stream writer, IEnumerable<(string Key, uint Value)> pairs, Endianness endian)
{

if(!pairs.Any() )
return;

var sorted = pairs.OrderBy(p => p.Key, StringComparer.Ordinal).ToList();

TrieWriter trieWriter = new();

foreach(var (key, val) in sorted)
{
NativeBuffer payload = new(4);
payload.SetUInt32(0, val, endian);

trieWriter.Add(key, payload);
}

using var trieBuffer = trieWriter.WriteToBuffer(endian);

writer.Write(trieBuffer.GetView() );
}

// Write map

public static void Write(Stream writer, string[] ids, Endianness endian)
{

if(ids is null || ids.Length == 0)
return;

var pairs = ids.Select( (id, index) => (id, (uint)index) );

Write(writer, pairs, endian);
}

}

}