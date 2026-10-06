using System;

namespace SexyPackages.ResourceStreamGroup
{
/// <summary> Compression flags for a ResGroup </summary>

[Flags]

public enum GroupCompressionFlags : uint
{
/// <summary> No Compression </summary>
None = 0,

/// <summary> Compress GPUData with ZLib (Optimal) </summary>
Part1 = 1,

/// <summary> Compress ResidentData with ZLib (Optimal) </summary>
Part0 = 2,

/// <summary> Compress all (SmallestSize) </summary>
All = Part1 | Part0
}

}