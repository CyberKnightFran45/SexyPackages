namespace SexyPackages.MarmaladeDZip
{
/// <summary> Index table for a Dz chunk </summary>

public struct ChunkMapEntry
{
/// <summary> Folder index </summary>

public ushort FolderIndex;

/// <summary> File index </summary>

public ushort FileIndex;

/// <summary> Chunk index </summary>
	
public ushort ChunkIndex;
	
// ctor

public ChunkMapEntry(ushort folderIdx, ushort fileIdx, ushort chunkIdx)
{
FolderIndex = folderIdx;

FileIndex = fileIdx;
ChunkIndex = chunkIdx;
}

}

}