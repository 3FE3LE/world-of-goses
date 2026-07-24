#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Root of the prototype scene. Composes the macro city view and the
/// building detail view, hosts the <see cref="CityWorldController"/>,
/// and handles top-level input. The actual visual logic lives in
/// the view scripts; this script is intentionally thin.
/// </summary>
public partial class CityPrototype : Node
{
    public override void _Ready()
    {
        GD.Print("World of Goses prototype starting.");
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") == "1")
        {
            CallDeferred(MethodName.ApplyVisualRegressionFixture);
        }
    }

    private void ApplyVisualRegressionFixture()
    {
        const string fixturePrefix = "--wog-visual-fixture=";
        string? fixture = null;
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (!argument.StartsWith(fixturePrefix, StringComparison.Ordinal)) continue;
            fixture = argument[fixturePrefix.Length..];
            break;
        }

        switch (fixture)
        {
            case "tutorial":
                GetNode<TutorialOverlay>("TutorialOverlay").ShowForVisualRegression();
                break;
            case "tutorial-long":
                GetNode<TutorialOverlay>("TutorialOverlay").ShowForVisualRegression(2);
                break;
            case "offline-report":
                GetNode<OfflineReportPanel>(
                    "GameUiShell/ScreenContent/CityMacroView/OfflineReportPanel")
                    .ShowVisualRegressionReport(BuildVisualOfflineReport());
                break;
            case "pause-menu":
                GetNode<PauseMenu>("PauseMenu").ShowForVisualRegression(confirmReset: false);
                break;
            case "pause-menu-reset":
                GetNode<PauseMenu>("PauseMenu").ShowForVisualRegression(confirmReset: true);
                break;
            case "resource-menu":
                GetNode<OrthogonalParcelTerrain>(
                    "GameUiShell/ScreenContent/CityMacroView/OrthogonalParcelTerrain")
                    .ShowResourceMenuForVisualRegression();
                break;
            case "resource-gather":
                GetNode<OrthogonalParcelTerrain>(
                    "GameUiShell/ScreenContent/CityMacroView/OrthogonalParcelTerrain")
                    .StartGatherForVisualRegression();
                break;
            case "construction-scroll":
                GetNode<CityMacroView>("GameUiShell/ScreenContent/CityMacroView")
                    .ShowConstructionScrollForVisualRegression();
                break;
            case "construction-placement":
                GetNode<CityMacroView>("GameUiShell/ScreenContent/CityMacroView")
                    .ShowPlacementForVisualRegression();
                break;
        }
    }

    private static OfflineProgressionReport BuildVisualOfflineReport()
    {
        const int rowCount = 80;
        var events = new List<WorldEvent>(rowCount);
        WorldEventSubject farm = WorldEventSubject.Building(new BuildingId(2), "Farm");
        WorldEventSubject quarry = WorldEventSubject.Building(new BuildingId(3), "Quarry");
        for (int index = 0; index < rowCount; index++)
        {
            WorldEventKind kind = index % 9 == 0
                ? WorldEventKind.ProductionBlocked
                : index % 2 == 0
                    ? WorldEventKind.StockProduced
                    : WorldEventKind.WorkerRecovered;
            WorldEventSubject subject = index % 3 == 0 ? quarry : farm;
            events.Add(new WorldEvent(
                new WorldEventId(index + 1),
                181_000 + index * 12,
                kind,
                subject,
                index % 5 + 1,
                index == 0 ? null : new WorldEventId(index)));
        }

        return new OfflineProgressionReport(
            ticksApplied: 960,
            stockAdded: 144,
            stockWasted: 12,
            simulatedTime: TimeSpan.FromHours(8),
            events);
    }
}
