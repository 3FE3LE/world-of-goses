using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// Tracks foot-traffic wear per walkable floor tile (S-1.3 phase 2): grass
/// worn down to dirt by repeated trampling, forming paths without any
/// explicit "draw a path" logic. Presentation-only state — like
/// <c>OrthogonalParcelTerrain</c>'s own ground tiles, this never feeds back
/// into simulation (no effect on gather rates, movement speed, etc.),
/// deliberately session-scoped rather than added to <c>WorldSave</c> for now.
/// </summary>
public sealed class TerrainWearGrid
{
    public const float WearPerTrample = 0.05f;
    public const float DirtThreshold = 0.5f;

    private readonly Dictionary<(int Street, int TileIndex), float> _wear = new();

    public void Trample(int street, int tileIndex)
    {
        _wear.TryGetValue((street, tileIndex), out float current);
        _wear[(street, tileIndex)] = Math.Min(1f, current + WearPerTrample);
    }

    public bool IsWorn(int street, int tileIndex) =>
        _wear.TryGetValue((street, tileIndex), out float value) && value >= DirtThreshold;
}
