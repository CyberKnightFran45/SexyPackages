namespace SexyPackages.MarmaladeDZip
{
/// <summary> Describes a Chunk inside a DZip file. </summary>

public class ChunkEntry
{
/// <summary> Chunk metadata </summary>

public ChunkMetadata Metadata{ get; set; }

/// <summary> File name </summary>

public string FileName{ get; set; }

/// <summary> Path to folder </summary>

public string FolderPath{ get; set; }

/// <summary> File Size (after Compression). </summary>

public int SizeCompressed{ get; set; }

// ctor

public ChunkEntry()
{
}

// ctor 2

public ChunkEntry(ChunkMetadata metadata, string fileName, string dirPath, int sizeCompressed)
{
Metadata = metadata;

FileName = fileName;
FolderPath = dirPath;

SizeCompressed = sizeCompressed;
}

}

}