namespace SexyPackages.ResourceStreamBundle
{
/// <summary> The Extraction state of a RSB </summary>

internal sealed class RsbExtractionState
{
/// <summary> Map of Groups and their resources </summary>

public GroupMap GroupsMap{ get; set; }

/// <summary> Wheter to encrypt RTON files in Packages or not </summary>

public bool? EncryptPackages{ get; set; }
}

}