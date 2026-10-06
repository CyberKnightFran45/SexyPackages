namespace SexyPackages.ResourceStreamGroup
{
// Helper functions for RSG

public static class RsgHelper
{
// Check if compression flags are valid.

public static bool CheckFlags(GroupCompressionFlags flags)
{
return (flags & ~GroupCompressionFlags.All) == 0;
}

// Warn RSG flags

internal static void WarnUnknownFlags(GroupCompressionFlags flags)
{
var rawFlags = (uint)flags;

TraceLogger.WriteWarn($"Unknown compression flags: 0x{rawFlags:X8} ({rawFlags})");
}

// Log unknown RSG flags

internal static void LogFlagsIfUnknown(GroupCompressionFlags flags)
{
bool isValid = CheckFlags(flags);

if(isValid)
return;

WarnUnknownFlags(flags);
}

// Calculate padding for RSG blocks

// Files: padIfAligned = false (no padding added when already aligned)
// Internal RSG data: padIfAligned = true (force full block on alignment)

public static int ComputePadding(int length, bool padIfAligned)
{
const int FILE_ALIGNMENT = 4096;

int required = length % FILE_ALIGNMENT;

if(required == 0)
return padIfAligned ? FILE_ALIGNMENT : 0;

return FILE_ALIGNMENT - required;
}

}

}