namespace SexyPackages.ResourceStreamBundle
{
/// <summary> RSB Compression flags (extra layer after packing) </summary>

public enum RsbCompressionFlags
{
/// <summary> No compression </summary>
None,

/// <summary> Use PopCap ZLib (smf) </summary>
ZLib,

/// <summary> Compress with Lzma (rslb) </summary>
Lzma
}

}