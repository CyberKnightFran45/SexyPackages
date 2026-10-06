using System.IO;
using SexyParsers.ReflectiveTypeObjectNotation;

namespace SexyPackages.ResourceStreamGroup
{
/// <summary> Codec used for handling RTON files embed in RSBs. </summary>

internal static class RtonCodec
{
/// <summary> Context key: whether to use RTON encryption or not </summary>

public const string CTX_KEY_USE_ENCRYPTION = "UseEncryption";

// RTON to JSON

public static void Decode(NativeBuffer raw, Stream target, CodecContext context)
{
RtonParser.Decode(raw, target, out bool wasEncrypted);

context?.Set(CTX_KEY_USE_ENCRYPTION, wasEncrypted);
}

// JSON to RTON

public static NativeBuffer Encode(Stream source, CodecContext context)
{
bool useEncryption = false;
context?.TryGet(CTX_KEY_USE_ENCRYPTION, out useEncryption);

using var raw = source.ReadPtr();

return RtonParser.Encode(raw, useEncryption);
}

}

}