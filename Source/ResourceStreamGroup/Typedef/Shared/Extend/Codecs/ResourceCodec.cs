using System;
using System.Collections.Generic;
using System.IO;

namespace SexyPackages.ResourceStreamGroup
{
/// <summary> Parses individual resources based on their file extension. </summary>

public static class ResourceCodec
{
// Res encoder

private delegate NativeBuffer ResEncoder(Stream source, CodecContext context);

// Encoders registered

private static readonly Dictionary<string, ResEncoder> Encoders = new(StringComparer.OrdinalIgnoreCase)
{

[".RTON"] = RtonCodec.Encode,
[".NEWTON"] = NewtonCodec.Encode,
[".PTX"]  = PtxCodec.Encode,
// [".pam"]  = PopAnimCodec.Encode,
// [".bnk"]  = SoundBankCodec.Encode,

};

// Res decoder

private delegate void ResDecoder(NativeBuffer source, Stream target, CodecContext context);

// Decoders registered

private static readonly Dictionary<string, ResDecoder> Decoders = new(StringComparer.OrdinalIgnoreCase)
{

[".RTON"] = RtonCodec.Decode,
[".NEWTON"] = NewtonCodec.Decode,
[".PTX"]  = PtxCodec.Decode,
// [".pam"]  = PopAnimCodec.Decode,
// [".bnk"]  = SoundBankCodec.Decode,

};

// Extensions map (encoded --> decoded)

private static readonly Dictionary<string, string> ExtensionsMap = new(StringComparer.OrdinalIgnoreCase)
{

[".RTON"] = ".json",
[".NEWTON"] = ".new.json",
[".PTX"]  = ".png",
[".PAM"]  = ".pop_anim",
[".BNK"]  = ".wav",

};

// Check if parser exists

private static bool HasParser<T>(Dictionary<string, T> parsers, string resName)
{
string fileExt = Path.GetExtension(resName);

return parsers.ContainsKey(fileExt);
}

// Check if encoder exists

public static bool HasEncoder(string resName) => HasParser(Encoders, resName);

// Check if decoder exists

public static bool HasDecoder(string resName) => HasParser(Decoders, resName);

// Get decoded extension

public static string GetDecodedExtension(string resName)
{
string fileExt = Path.GetExtension(resName);

return ExtensionsMap.TryGetValue(fileExt, out var decodedExt) ? decodedExt : null;
}

/// <summary> Attempts to encode a resource from a source stream into a RAM buffer. </summary>

public static bool TryEncode(string resName, Stream source, out NativeBuffer encoded,
                             CodecContext context = null)
{
string ext = Path.GetExtension(resName);

if(Encoders.TryGetValue(ext, out var encoder) )
{
encoded = encoder(source, context);

return true;
}

encoded = null;

return false;
}

/// <summary> Attempts to decode a resource from a RAM buffer into the target stream. </summary>

public static bool TryDecode(string resName, NativeBuffer source, Stream target,
                             CodecContext context = null)
{
string ext = Path.GetExtension(resName);

if(Decoders.TryGetValue(ext, out var decoder) )
{
decoder(source, target, context);

return true;
}

return false;
}

}

}