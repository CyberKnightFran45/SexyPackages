using System;
using System.IO;

namespace SexyPackages
{
/// <summary> Helper used for Dumping NativeMemory to FileStreams </summary>

public static class RawBufferHelper
{
// Dump ptr to fs

public static void Dump(RawBuffer src, Stream dest, uint offset, uint size)
{
const int MAX_CHUNK = int.MaxValue; // 2 GB

long remaining = size;
ulong currOffset = offset;

while(remaining > 0)
{
var chunkSize = (int)Math.Min(remaining, MAX_CHUNK);

var bytes = src.GetView(currOffset, chunkSize); 
dest.Write(bytes);
    
currOffset += (ulong)chunkSize;
remaining -= chunkSize;
}

}

}

}