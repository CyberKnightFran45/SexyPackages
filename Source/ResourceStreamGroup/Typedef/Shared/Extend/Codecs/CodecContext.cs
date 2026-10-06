using System.Collections.Generic;

namespace SexyPackages.ResourceStreamGroup
{
/// <summary> Optional side-channel metadata a codec wants to report back </summary>

public sealed class CodecContext
{
// Properties

private readonly Dictionary<string, object> _values = new();

// Set context

public void Set(string key, object val) => _values[key] = val;

// Get context

public bool TryGet<T>(string key, out T val)
{

if(_values.TryGetValue(key, out var raw) && raw is T typed)
{
val = typed;

return true;
}

val = default;

return false;
}

}

}