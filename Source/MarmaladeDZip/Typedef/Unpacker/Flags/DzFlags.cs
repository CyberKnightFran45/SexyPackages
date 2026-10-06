using System;

namespace SexyPackages.MarmaladeDZip
{
/// <summary> Special flags for DZip Compression </summary>

[Flags]

public enum DzFlags : ushort
{
/// <summary> Buffer is a part that will be appended to other to complete file </summary>
COMBUF = 1,

/// <summary> Inner DZip File </summary>
DZ = 4,

/// <summary> Data is compressed with GZip </summary>
GZIP = 8,

/// <summary> Data is compressed with BZip2 </summary>
BZIP = 16,

/// <summary> MP3 file </summary>
MP3 = 32,

/// <summary> Jpeg file </summary>
JPEG = 64,

/// <summary> File contains padding zeroes </summary>
ZERO = 128,

/// <summary> Data is readonly </summary>
STORE = 256,

/// <summary> Data is compressed with Lzma </summary>
LZMA = 512,

/// <summary> RAM buffer </summary>
RANDOMACCESS = 1024
}

}