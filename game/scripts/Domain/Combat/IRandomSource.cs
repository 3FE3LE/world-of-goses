#nullable enable
using System;

namespace WorldofGoses.Domain.Combat;

/// <summary>
/// Injected randomness so an encounter is reproducible from a seed. The combat
/// domain never touches System.Random directly and never reads the clock.
/// </summary>
public interface IRandomSource
{
    /// <summary>A value in [0, 1).</summary>
    double NextDouble();

    /// <summary>A value in [0, exclusiveMaximum).</summary>
    int NextInt(int exclusiveMaximum);
}

/// <summary>
/// Small explicit PRNG. Deliberately not System.Random: this needs a stable
/// sequence for a given seed across runtimes so a recorded CombatLog can be
/// replayed and diffed, which System.Random does not guarantee across versions.
/// Implementation is SplitMix64.
/// </summary>
public sealed class DeterministicRandom : IRandomSource
{
    private ulong _state;

    public DeterministicRandom(ulong seed)
    {
        Seed = seed;
        _state = seed;
    }

    public ulong Seed { get; }

    public double NextDouble()
    {
        // 53 significant bits, the exact mantissa width of a double.
        return (NextUInt64() >> 11) * (1.0 / 9007199254740992.0);
    }

    public int NextInt(int exclusiveMaximum)
    {
        if (exclusiveMaximum <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(exclusiveMaximum), exclusiveMaximum, "Bound must be positive.");
        }
        return (int)(NextDouble() * exclusiveMaximum);
    }

    private ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15UL;
        ulong z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
