using System;

namespace SexyPackages.ResourceStreamBundle
{
/// <summary> Selects which content pull out of a RSB </summary>

[Flags]

public enum RsbResType : uint
{
/// <summary> RSB-wide metadata only </summary>

Info = 1 << 0,

/// <summary> Manifest info either from RSB or from <c>__MANIFESTGROUP__</c> </summary>

ManifestGroup = 1 << 1,

/// <summary> RSB's text resources </summary>

Text = 1 << 2,

/// <summary> Extract Game config found in <c>Packages</c> and .ini files
/// from <c>Credits_Common</c> (Chinese Version)  </summary>

GameConfig = 1 << 3,

/// <summary> Extract and decode textures; TO-DO: split ATLASES into sprites </summary>

Textures = 1 << 4,

/// <summary> All animation resources across every ResGroup </summary>

Animations = 1 << 5,

/// <summary> All sound resources across every ResGroup </summary>

Sounds = 1 << 6,

/// <summary> Extract metadata, such as RSB Info and Manifest Group </summary>

Metadata = Info | ManifestGroup,

/// <summary> Extract Text and Game Config </summary>

TextAndConfig = Text | GameConfig,

/// <summary> Extract all media resource, such as Textures, Animations and Sounds </summary>

Media = Textures | Animations | Sounds,

/// <summary> Extract all supported resources by using specialized extractors (inspection only) </summary>

ContentOnly = TextAndConfig | Media
}

}