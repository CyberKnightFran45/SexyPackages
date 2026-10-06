using System;
using System.Threading.Tasks;

/// <summary> Tables with options for doing Parallelism </summary>

internal static class ParallelTables
{
/// <summary> Multi-thread options </summary>

public static readonly ParallelOptions MultiThreadOptions = new()
{
MaxDegreeOfParallelism = Environment.ProcessorCount * 2
};

/// <summary> Max amount of files to process in a single thread </summary>

public const int MAX_FILES_SINGLE_THREAD = 100;
}