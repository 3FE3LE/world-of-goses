using System;

namespace WorldofGoses;

/// <summary>Central real-time persistence cadence for the desktop session.</summary>
public static class SimulationPersistencePolicy
{
    public static readonly TimeSpan AutoSaveInterval = TimeSpan.FromMinutes(3);
}
