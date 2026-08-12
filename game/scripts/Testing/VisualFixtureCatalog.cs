#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses.Testing;

/// <summary>
/// Macro HUD composition state, exposed so the catalog and tests can
/// address each authored macro HUD variant without knowing its enum
/// value.
/// </summary>
public enum MacroHudFixtureState
{
    Default,
    Selection,
    ActiveConstruction,
    ActiveExpedition,
}

/// <summary>
/// Typed dispatch entry point for the visual-regression fixture
/// bodies that live on <c>CityPrototype</c>. Issue #5 establishes
/// this interface so the catalog can route a fixture name to the
/// matching <c>Show*ForVisualRegression</c> method without reading
/// <c>CityPrototype</c>'s private API. The bodies themselves stay
/// on the prototype (they touch the scene tree, which is the
/// prototype's responsibility); the interface is the seam.
/// </summary>
public interface IVisualFixtureHost
{
    void ApplyNamedFixture(string name);
}

/// <summary>
/// Catalog of authored visual regression fixtures. Each entry maps a
/// fixture name (passed via <c>--wog-visual-fixture=&lt;name&gt;</c>) to
/// the function that prepares the world for its screenshot.
///
/// <para>Architecture Hardening A10 introduces this catalog as the
/// single dispatch table for fixture orchestration. A10 issue #5
/// moves the case-statement dispatch out of <c>CityPrototype</c>:
/// the prototype implements <see cref="IVisualFixtureHost"/>, and
/// the catalog knows every named fixture. The bodies stay on the
/// prototype (they touch the scene tree), but the dispatch table
/// and the closed enumeration of known fixtures live here.</para>
/// </summary>
public sealed class VisualFixtureCatalog
{
    /// <summary>
    /// The exhaustive set of fixture names the catalog understands.
    /// A fixture not present here is treated as "unknown" by the
    /// harness: it logs a warning and falls through to the default
    /// state. This is intentionally a closed enumeration so the
    /// composition tests can assert that every documented fixture has
    /// a runtime entry (no orphan names).
    /// </summary>
    public IReadOnlyCollection<string> KnownFixtures { get; }

    private readonly Dictionary<string, VisualFixtureKind> _kinds =
        new(StringComparer.Ordinal);

    public VisualFixtureCatalog()
    {
        // Macro compositions.
        Register("macro-current", VisualFixtureKind.MacroComposition);
        Register("macro-summary-low-food", VisualFixtureKind.MacroComposition);
        Register("macro-summary-housing-full", VisualFixtureKind.MacroComposition);
        Register("macro-summary-no-construction", VisualFixtureKind.MacroComposition);
        Register("macro-hud-default", VisualFixtureKind.MacroComposition);
        Register("macro-hud-selection", VisualFixtureKind.MacroComposition);
        Register("macro-hud-active-construction", VisualFixtureKind.MacroComposition);
        Register("macro-hud-active-expedition", VisualFixtureKind.MacroComposition);

        // First night.
        Register("first-night-active", VisualFixtureKind.FirstNight);
        Register("first-night-concluded", VisualFixtureKind.FirstNight);

        // Hero / building / expedition.
        Register("hero-profile", VisualFixtureKind.HeroProfile);
        Register("building-detail", VisualFixtureKind.BuildingDetail);
        Register("expedition-rail", VisualFixtureKind.ExpeditionRail);
        Register("expedition-rail-empty", VisualFixtureKind.ExpeditionRail);
        Register("expedition-rail-active", VisualFixtureKind.ExpeditionRail);
        Register("expedition-rail-returned", VisualFixtureKind.ExpeditionRail);

        // Long terrarium probe (handled by the harness directly).
        Register("long-terrarium", VisualFixtureKind.LongTerrarium);

        KnownFixtures = _kinds.Keys;
    }

    /// <summary>
    /// Classifies the given fixture name. Returns
    /// <see cref="VisualFixtureKind.Other"/> for names not in the
    /// catalog; the harness logs a warning in that case so the
    /// operator notices the typo.
    /// </summary>
    public VisualFixtureKind Classify(string name)
    {
        if (_kinds.TryGetValue(name, out var kind))
        {
            return kind;
        }
        return VisualFixtureKind.Other;
    }

    /// <summary>
    /// True when the catalog has an entry for the given fixture name.
    /// </summary>
    public bool Contains(string name) =>
        name is not null && _kinds.ContainsKey(name);

    private void Register(string name, VisualFixtureKind kind) =>
        _kinds[name] = kind;
}


