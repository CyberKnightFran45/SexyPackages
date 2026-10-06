using System.IO;
using SexyParsers.Newton;

namespace SexyPackages.ResourceStreamGroup
{
/// <summary> Codec used for handling NEWTON files embed in RSBs. </summary>

internal static class NewtonCodec
{
// NEWTON to JSON

public static void Decode(NativeBuffer raw, Stream target, CodecContext context)
{
using MemoryStream input = new();

RawBufferHelper.Dump(raw, input, 0, (uint)raw.Size);
input.Seek(0, SeekOrigin.Begin);

NewtonParser.DecodeStream(input, target);
}

// JSON to NEWTON

public static NativeBuffer Encode(Stream source, CodecContext context)
{
using MemoryStream dest = new();
NewtonParser.EncodeStream(source, dest);

dest.Seek(0, SeekOrigin.Begin);

return dest.ReadPtr();
}

}

}