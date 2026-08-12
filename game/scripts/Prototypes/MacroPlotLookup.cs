#nullable enable
using System.Collections.Generic;
using WorldofGoses.Domain;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Read-only record-bag for the macro street view (A4). The renderer fills
/// it via <see cref="MacroStreetLiveView.RefreshPlots"/>; the interaction
/// controller and the journey presenter read it for click routing, hover
/// updates, and find-by-id queries. This is the seam between "what the
/// view saw" and "what collaborators can query" — a record-bag, not an
/// interface, so the two collaborators share the same instance without
/// introducing a new abstract type.
/// </summary>
internal sealed class MacroPlotLookup
{
    public IReadOnlyList<MacroStreetRenderer.PlotBox> Plots { get; private set; } = System.Array.Empty<MacroStreetRenderer.PlotBox>();
    public IReadOnlyList<MacroStreetRenderer.TreeBox> Trees { get; private set; } = System.Array.Empty<MacroStreetRenderer.TreeBox>();
    public IReadOnlyDictionary<int, CityMacroSnapshot.CitizenItem> CitizenStates { get; private set; }
        = new Dictionary<int, CityMacroSnapshot.CitizenItem>();
    public IReadOnlyDictionary<(int Row, int Column), ParcelTerritoryState> ParcelTerritory
    { get; private set; } = new Dictionary<(int, int), ParcelTerritoryState>();

    /// <summary>Replace every collection in one atomic update. The renderer
    /// calls this at the end of <c>RefreshPlots</c> so readers see the
    /// new state coherently instead of mid-update.</summary>
    public void Update(
        IReadOnlyList<MacroStreetRenderer.PlotBox> plots,
        IReadOnlyList<MacroStreetRenderer.TreeBox> trees,
        IReadOnlyDictionary<int, CityMacroSnapshot.CitizenItem> citizenStates,
        IReadOnlyDictionary<(int Row, int Column), ParcelTerritoryState> parcelTerritory)
    {
        Plots = plots;
        Trees = trees;
        CitizenStates = citizenStates;
        ParcelTerritory = parcelTerritory;
    }
}
