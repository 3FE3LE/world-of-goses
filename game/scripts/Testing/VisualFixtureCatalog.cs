#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses.Testing;

/// <summary>
/// Catalog of authored visual regression fixtures. Each entry maps a
/// fixture name (passed via <c>--wog-visual-fixture=&lt;name&gt;</c>) to
/// the function that prepares the world for its screenshot.
///
/// <para>Architecture Hardening A10 introduces this catalog as the
/// single dispatch table for fixture orchestration. The previous
/// shape inlined every fixture as a
/// <c>Show*ForVisualRegression</c> method on <c>CityPrototype</c>;
/// the catalog keeps one typed table here so a future slice can move
/// the per-fixture composition steps into a fixture builder without
/// touching <c>CityPrototype</c>'s 2 600-line body.</para>
///
/// <para>This first slice ships the catalog scaffold and the
/// classification helper. Per-fixture composition still lives on
/// <c>CityPrototype</c>; the next slice ports it entry-by-entry.
/// Adding a fixture before the port is a one-line entry in the
/// <see cref="KnownFixtures"/> table; adding a fixture after the port
/// is a one-line entry in the catalog's
/// <see cref="Register"/> method.</para>
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

    private void Register(string name, VisualFixtureKind kind) =>
        _kinds[name] = kind;
}
