using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SexyPackages
{
/// <summary> Encode Paths inside a Binary Trie </summary>

public class TrieWriter
{
// Root element

private readonly TrieNode _root = new('\0');

// Struct offset size (Int24)

private const int STRUCT_OFFSET_SIZE = 3;

/// <summary> Adds a path with its payload. </summary>

public void Add(string path, NativeBuffer payload)
{

if(string.IsNullOrEmpty(path) )
throw new ArgumentException("Path cannot be null or empty", nameof(path) );

var node = _root;

foreach(char c in path)
{

if(!node.TryGetChild(c, out var child) )
{
child = new(c);

node.AddChild(c, child);
}

node = child;
}

if(node.IsTerminal)
throw new InvalidOperationException($"Duplicate path: {path}");

node.IsTerminal = true;
node.Payload = payload; // Take ownership of the buffer
}

// Sort Children

private static void SortChildren(TrieNode node)
{
var nodeChildren = node.Children;

var sorted = nodeChildren.OrderBy(c => c.Key).ToList();
nodeChildren.Clear();

foreach(var s in sorted)
node.AddChild(s.Key, s.Value);

foreach(var child in node.ChildValues)
SortChildren(child); // Sort sub-nodes

}

// Get fragment size

private static int FragmentSize(char c)
{
var singleChar = MemoryMarshal.CreateReadOnlySpan(in c, 1);
int byteCount = Encoding.UTF8.GetByteCount(singleChar);

return byteCount + STRUCT_OFFSET_SIZE;
}

// Compute subtree sizes

private static void ComputeSubtreeSizes(TrieNode node)
{
int size = FragmentSize(node.Value);

if(node.IsTerminal)
size += FragmentSize('\0') + (int)node.PayloadLength;

foreach(var child in node.ChildValues)
{
ComputeSubtreeSizes(child);

size += child.SubtreeSize; // Get sub-nodes size
}

node.SubtreeSize = size;
}

// Ensure buffer capacity

private static void EnsureCapacity(NativeBuffer buffer, ulong neededSize)
{
ulong bufferSize = buffer.Size;

if(bufferSize < neededSize)
{
ulong newSize = Math.Max(bufferSize * 2, neededSize + 64);

buffer.Realloc(newSize);
}

}

// Compute struct offset

private static long ComputeStructOffset(bool hasNextSibling, ulong pos, int subtreeSize)
{

if(!hasNextSibling)
return 0;

return (long)(pos + (ulong)subtreeSize);
}

// Compute terminal offset

private static long ComputeTerminalOffset(bool hasChildrenAfterTerminal, ulong pos, ulong payloadLen)
{

if(!hasChildrenAfterTerminal)
return 0;

var terminalSize = (ulong)FragmentSize('\0');

return (long)(pos + terminalSize + payloadLen);
}

// Write node fragment

private static void WriteFragment(NativeBuffer buffer,
                                  char c,
                                  long structOffset,
                                  ref ulong pos,
                                  Endianness endian)
{
EnsureCapacity(buffer, pos + 4);

int bytesWritten = buffer.SetChar(pos, c, EncodingType.UTF8);
pos += (ulong)bytesWritten;

var offsetDiv4 = (int)(structOffset / 4);
buffer.SetInt24(pos, offsetDiv4, endian);

pos += 3;
}

// Write node

private static void WriteNode(NativeBuffer buffer,
                              TrieNode node,
                              ref ulong pos,
                              bool hasNextSibling,
                              Endianness endian)
{
long structOffset = ComputeStructOffset(hasNextSibling, pos, node.SubtreeSize);

WriteFragment(buffer, node.Value, structOffset, ref pos, endian);

if(node.IsTerminal)
{
bool hasChildrenAfterTerminal = node.HasChildren;

ulong payloadLen = node.PayloadLength;
long terminalOffset = ComputeTerminalOffset(hasChildrenAfterTerminal, pos, payloadLen);

WriteFragment(buffer, '\0', terminalOffset, ref pos, endian);

if(payloadLen > 0)
{
EnsureCapacity(buffer, pos + payloadLen);
buffer.CopyFrom(node.Payload, pos);

pos += payloadLen;
}

}

var children = node.ChildValues.ToList();
int childCount = children.Count;

for(int i = 0; i < childCount; i++)
{
var child = children[i];
bool childHasNext = i < childCount - 1;

WriteNode(buffer, child, ref pos, childHasNext, endian);
}

}

// Release payload buffers

private static void ReleasePayloads(TrieNode node)
{

if(node.Payload != null)
{
node.Payload.Dispose();

node.Payload = null;
}

foreach(var child in node.ChildValues)
ReleasePayloads(child); // Release sub-nodes

}

/// <summary> Writes all paths to a NativeBuffer (preserves insertion order) </summary>

public NativeBuffer WriteToBuffer(Endianness endian)
{
SortChildren(_root);
ComputeSubtreeSizes(_root);

NativeBuffer buffer = new(1024);

try
{
ulong pos = 0;

var children = _root.ChildValues.ToList();
int childCount = children.Count;

for(int i = 0; i < childCount; i++)
{
var child = children[i];
bool hasNext = i < childCount - 1;

WriteNode(buffer, child, ref pos, hasNext, endian);
}

buffer.Realloc(pos);

return buffer;
}

finally
{
ReleasePayloads(_root);
}

}

}

}