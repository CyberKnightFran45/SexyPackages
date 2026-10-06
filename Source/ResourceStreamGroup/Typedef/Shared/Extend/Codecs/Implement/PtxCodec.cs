using System.IO;
using SexyPackages.ResourceStreamBundle;
using TextureTranscoder.Parsers.PopCapTexture;

namespace SexyPackages.ResourceStreamGroup
{
/** <summary> Codec used for handling Textures embed in RSBs (only parses them by the moment) </summary>

<remarks> TO-DO: Atlas Slice & Splice. </remarks> **/

internal static class PtxCodec
{
/// <summary> Context key: RSB platform </summary>

public const string CTX_KEY_PLATFORM = "Platform";

/// <summary> Context key: PTX format </summary>

public const string CTX_KEY_FORMAT = "Format";

/// <summary> Context key: image width </summary>

public const string CTX_KEY_WIDTH = "Width";

/// <summary> Context key: image height </summary>

public const string CTX_KEY_HEIGHT = "Height";

/// <summary> Context key: texture pitch </summary>

public const string CTX_KEY_PITCH = "Pitch";

/// <summary> Context key: alpha size (extend1) </summary>

public const string CTX_KEY_ALPHA_SIZE = "AlphaSize";

/// <summary> Context key: texture scale (extend2) </summary>

public const string CTX_KEY_SCALE = "Scale";

// Get ptx format (direct cast)

private static PtxFormat CastFormat(uint flags) => (PtxFormat)flags;

// Normalize format (Android)

private static PtxFormat NormalizeFormat_Android(uint flags) => flags switch
{
0 => PtxFormat.RGBA8888,
_ => CastFormat(flags)
};

// Normalize format (Android, Chinese Version)

private static PtxFormat NormalizeFormat_AndroidCN(uint flags) => flags switch
{
147 => PtxFormat.ETC1_RGB_A_Palette,
_ => CastFormat(flags)
};

// Normalize format (iOS)

private static PtxFormat NormalizeFormat_iOS(uint flags) => flags switch
{
0 => PtxFormat.ARGB8888,
_ => CastFormat(flags)
};

// Get ptx format

private static PtxFormat GetFormat(uint flags, RsbPlatform platform) => platform switch
{
RsbPlatform.AndroidCN => NormalizeFormat_AndroidCN(flags),
RsbPlatform.iOS => NormalizeFormat_iOS(flags),
_ => NormalizeFormat_Android(flags)
};

// Build PtxInfo from given Context

private static PtxInfo BuildPtxInfo(CodecContext context)
{
context.TryGet(CTX_KEY_PLATFORM, out RsbPlatform platform);
context.TryGet(CTX_KEY_FORMAT, out uint flags);

var format = GetFormat(flags, platform);

context.TryGet(CTX_KEY_WIDTH, out uint width);
context.TryGet(CTX_KEY_HEIGHT, out uint height);
context.TryGet(CTX_KEY_PITCH, out uint pitch);
context.TryGet(CTX_KEY_ALPHA_SIZE, out uint aSize);
context.TryGet(CTX_KEY_SCALE, out uint scale);

return new(width, height, pitch, format, aSize, (PtxAlphaChannel)scale);
}

// PTX to PNG

public static void Decode(NativeBuffer raw, Stream target, CodecContext context)
{
var info = BuildPtxInfo(context);

PtxParser.Decode(raw.GetView(), target, ref info);
}

// Update context with given PtxInfo

private static void UpdateContext(CodecContext context, in PtxInfo info)
{
context.Set(CTX_KEY_WIDTH, info.Width);
context.Set(CTX_KEY_HEIGHT, info.Height);
context.Set(CTX_KEY_PITCH, info.Pitch);

context.Set(CTX_KEY_ALPHA_SIZE, info.AlphaSize);
context.Set(CTX_KEY_SCALE, (uint)info.AlphaChannel);
}

// PNG to PTX

public static NativeBuffer Encode(Stream source, CodecContext context)
{
context.TryGet(CTX_KEY_PLATFORM, out RsbPlatform platform);
context.TryGet(CTX_KEY_FORMAT, out uint flags);
	
var format = GetFormat(flags, platform);
var rawBuffer = PtxParser.Encode(source, format, out var info);

UpdateContext(context, info);

return rawBuffer;
}

}

}