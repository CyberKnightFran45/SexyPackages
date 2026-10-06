using System.Text.Json.Serialization;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> ManifestRes Image Props (JSON model) </summary>

public class ManifestResImageProperty_Json
{
/// <summary> Image type (1 for Atlas, 0 for regular) </summary>

public ushort Type{ get; set; }

/// <summary> Atlas flags </summary>

public ushort AtlasFlags{ get; set; }

/// <summary> Offset X </summary>

public ushort X{ get; set; }

/// <summary> Offset Y </summary>

public ushort Y{ get; set; }

/// <summary> Offset X for Atlas </summary>

public ushort AtlasX{ get; set; }

/// <summary> Offset Y for Atlas </summary>

public ushort AtlasY{ get; set; }

/// <summary> Atlas Width </summary>

public ushort AtlasWidth{ get; set; }

/// <summary> Atlas Height </summary>

public ushort AtlasHeight{ get; set; }

/// <summary> Number of Rows </summary>

public ushort Rows{ get; set; }

/// <summary> Number of Columns </summary>

public ushort Cols{ get; set; }

/// <summary> Parent image name </summary>

public string ParentName{ get; set; }

// ctor

public ManifestResImageProperty_Json()
{
}

// ctor 2

public ManifestResImageProperty_Json(in ManifestResImageProperty imgProp, string parentName)
{
Type = imgProp.Type;
AtlasFlags = imgProp.AtlasFlags;

X = imgProp.X;
Y = imgProp.Y;

AtlasX = imgProp.AtlasX;
AtlasY = imgProp.AtlasY;

AtlasWidth = imgProp.AtlasWidth;
AtlasHeight = imgProp.AtlasHeight;

Rows = imgProp.Rows;
Cols = imgProp.Cols;

ParentName = parentName;
}

}

}