using System;

namespace WorldofGoses.Domain.Persistence;

/// <summary>Raised when a save belongs to an intentionally unsupported schema.</summary>
public sealed class IncompatibleSaveVersionException : InvalidOperationException
{
    public IncompatibleSaveVersionException(int foundVersion, int expectedVersion)
        : base($"Save schema v{foundVersion} is incompatible with v{expectedVersion}.")
    {
        FoundVersion = foundVersion;
        ExpectedVersion = expectedVersion;
    }

    public int FoundVersion { get; }
    public int ExpectedVersion { get; }
}
