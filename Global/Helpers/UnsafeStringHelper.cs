using System;
using System.Text;

namespace SexyPackages
{
/// <summary> Convert strings from unsafe to managed and viceversa </summary>

public static unsafe class UnsafeStringHelper
{
// Convert ptr to string

public static string ExtractString(byte* ptr, int maxLength,
                                   EncodingType encodeFlags = EncodingType.UTF8)
{
var encoding = encodeFlags.GetEncoding();

int length = 0;

while(length < maxLength && ptr[length] != 0x00)
length++;

return encoding.GetString(ptr, length);
}

// Write fixed string

public static void WriteFixedString(byte* dest, int maxLen, string val)
{
Span<byte> span = new(dest, maxLen);
span.Clear();

if(string.IsNullOrEmpty(val) )
return;

int byteCount = Encoding.UTF8.GetByteCount(val);
int toWrite = byteCount;

if(byteCount >= maxLen)
toWrite = maxLen - 1;

Span<byte> temp = stackalloc byte[byteCount];
Encoding.UTF8.GetBytes(val, temp);

temp[.. toWrite].CopyTo(span);
}

}

}