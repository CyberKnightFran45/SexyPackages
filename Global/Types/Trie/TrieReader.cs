using System;
using System.Collections.Generic;

namespace SexyPackages
{
/// <summary> Decode Paths inside a Binary Trie </summary>

public static class TrieReader
{
/// <summary> Handles the struct payload found right after a resolved Trie path </summary>

public delegate void BufferEntryHandler(NativeBuffer buffer, ref ulong pos, string path);

// Read Path fragment

private static void ReadFragment(NativeBuffer buffer,
                                 ref ulong pos,
                                 Endianness endian,
                                 out string fragment,
                                 out int structOffset)
{
char c = buffer.GetChar(pos, EncodingType.UTF8, out int bytesRead); // UTF-8 char

fragment = c.ToString();
pos += (ulong)bytesRead;

structOffset = buffer.GetInt24(pos, endian) * 4;
pos += 3;
}

// Append path fragment

private static void AppendFragment(ReadOnlySpan<char> fragment, 
                                   int structOffset,
                                   ref NativeString rawPaths,
                                   ref int curLength,
                                   Dictionary<int, string> pendingOffsets)
{

if(structOffset > 0)
pendingOffsets[structOffset] = rawPaths.Substring(0, curLength);

long bufferSize = rawPaths.Length;
long sizeNeeded = curLength + fragment.Length;

if(sizeNeeded >= bufferSize)
{
long newSize = Math.Max(bufferSize * 2, sizeNeeded + 64);

rawPaths.Realloc(newSize);
}

foreach(char c in fragment)
rawPaths[curLength++] = c;

}

// Resolve Trie Path along with Struct info

public static void Traverse(NativeBuffer buffer,
                            uint offset,
                            uint listSize,
                            Endianness endian,
                            BufferEntryHandler onResourceFound)
{
Dictionary<int, string> pendingOffsets = new();
NativeString rawPaths = new(256);

ulong pos = offset;
ulong end = offset + listSize;

int curLength = 0;

while(pos < end)
{
ReadFragment(buffer, ref pos, endian, out var fragment, out var structOffset);

if(fragment == "\0")
{

if(curLength > 0)
{
string currentPath = rawPaths.Substring(0, curLength);
onResourceFound?.Invoke(buffer, ref pos, currentPath);

foreach(var p in pendingOffsets)
{
int pathOffset = p.Key;
ulong expectedPos = offset + (uint)pathOffset;

if(expectedPos == pos)
{
string oldPath = p.Value;
int oldLen = oldPath.Length;

if(rawPaths.Length < oldLen)
rawPaths.Realloc(oldLen * 2);

oldPath.CopyTo(rawPaths);
curLength = oldLen;

pendingOffsets.Remove(pathOffset);
break;
}

}

}

}

else
AppendFragment(fragment, structOffset, ref rawPaths, ref curLength, pendingOffsets);

}

rawPaths.Dispose();
}

}

}