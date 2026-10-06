using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace SexyPackages.PopCapPackage
{
// Provides useful Tasks for PAK Files

public static class PakHelper
{
// Get file alignment

private static int GetAlign(bool isPtx) => isPtx ? 4096 : 8;

// Check if Res is aligned

public static bool IsAligned(long pos, bool isPtx)
{
int align = GetAlign(isPtx);

return pos % align == 0;
}

// Align PAK Data

public static void AlignStream(Stream target, bool isPtx)
{
int align = GetAlign(isPtx);
long pos = target.Position + 2;

var misalignment = (int)(pos % align);
int padding = misalignment == 0 ? 0 : align - misalignment;

target.WriteUInt16( (ushort)padding);
target.Fill(padding);
}

}

}