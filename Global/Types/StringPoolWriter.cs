using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SexyPackages
{
// Strings pool writer

internal sealed class StringPoolWriter
{
// Buffer

public readonly MemoryStream Stream = new();

// String cache

private readonly Dictionary<string, uint> _cache = new(StringComparer.Ordinal);

// ctor

public StringPoolWriter()
{
Stream.WriteByte(0); // Offset 0 is reserved for null or empty strings
}

// Add string

public uint Add(string val)
{

if(string.IsNullOrEmpty(val) )
return 0;

if(_cache.TryGetValue(val, out var existing) )
return existing;

var offset = (uint)Stream.Position;
byte[] bytes = Encoding.UTF8.GetBytes(val);

Stream.Write(bytes);
Stream.WriteByte(0);

_cache[val] = offset;

return offset;
}

}

}