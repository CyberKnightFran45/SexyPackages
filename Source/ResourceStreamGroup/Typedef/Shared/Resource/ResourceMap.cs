namespace SexyPackages.ResourceStreamGroup
{
/// <summary> Maps resources inside a RSG with their Name </summary>

public class ResourceMap
{
// Resident files

public CommonResMap ResidentFiles{ get; } = new();

// GPU files

public PtxMap TextureFiles{ get; } = new();

// File count

public int FileCount => ResidentFiles.Count;

// Texture count

public int TextureCount => TextureFiles.Count;

// ctor

public ResourceMap()
{
}

// ctor 2

public ResourceMap(int fileCount, int textureCount)
{
ResidentFiles = new(fileCount);
TextureFiles = new(textureCount);
}

// Add resident info

public void AddResident(string path, RsgResidentInfo info) => ResidentFiles.Add(path, info);

// Add texture info

public void AddTexture(string path, RsgTextureInfo info) => TextureFiles.Add(path, info);

// Clear map

public void Clear()
{
ResidentFiles.Clear();
TextureFiles.Clear();
}

}

}