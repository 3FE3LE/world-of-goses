#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

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
            case "expedition-idle":
                GetNode<CityMacroView>("GameUiShell/ScreenContent/CityMacroView")
                    .ShowExpeditionForVisualRegression(CityMacroView.ExpeditionFixtureState.Idle);
                break;
            case "expedition-active":
                GetNode<CityMacroView>("GameUiShell/ScreenContent/CityMacroView")
                    .ShowExpeditionForVisualRegression(CityMacroView.ExpeditionFixtureState.Active);
                break;
            case "expedition-returned":
                GetNode<CityMacroView>("GameUiShell/ScreenContent/CityMacroView")
                    .ShowExpeditionForVisualRegression(CityMacroView.ExpeditionFixtureState.Returned);
                break;
            case "migrant":
                GetNode<MigrantPanel>(
                    "GameUiShell/ScreenContent/CityMacroView/MigrantPanel")
                    .ShowForVisualRegression();
                break;
            case "modal-layout-close":
                ValidateModalLayoutAndClosePaths();
                break;
            case "astral-start":
                GetNode<AstralOnboardingView>("OnboardingView")
                    .ShowForVisualRegression(0);
                break;
            case "astral-ground":
                GetNode<AstralOnboardingView>("OnboardingView")
                    .ShowForVisualRegression(10);
                break;
            case "astral-identity":
                GetNode<AstralOnboardingView>("OnboardingView")
                    .ShowForVisualRegression(12);
                break;
            case "founder-arrival":
                ShowFounderArrivalForVisualRegression();
                break;
        }
    }

    private async void ValidateModalLayoutAndClosePaths()
    {
        GD.Print("Modal layout/close fixture started.");
        CityMacroView city = GetNode<CityMacroView>(
            "GameUiShell/ScreenContent/CityMacroView");
        ModalHost host = GetNode<ModalHost>(
            "GameUiShell/ScreenContent/CityMacroView/ModalHost");
        ExpeditionPanel expedition = GetNode<ExpeditionPanel>(
            "GameUiShell/ScreenContent/CityMacroView/ExpeditionPanel");
        MigrantPanel migrant = GetNode<MigrantPanel>(
            "GameUiShell/ScreenContent/CityMacroView/MigrantPanel");
        ConstructionPanel construction = GetNode<ConstructionPanel>(
            "GameUiShell/ScreenContent/CityMacroView/Center/ConstructionPanel");

        expedition.Open();
        await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        ValidateContained("ExpeditionPanel", expedition, city);
        expedition.Close();
        await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);

        migrant.ShowForVisualRegression();
        await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        ValidateContained("MigrantPanel", migrant, city);
        migrant.Close();
        await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);

        city.ShowConstructionScrollForVisualRegression();
        await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        ValidateContained("ConstructionPanel", construction, city);
        construction.PressHeaderCloseForVisualRegression();
        await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        if (host.IsOpen || construction.Visible)
        {
            GD.PushError("Modal close fixture: construction X did not close ModalHost.");
        }
        else
        {
            GD.Print("Modal layout/close fixture passed.");
        }
    }

    private static void ValidateContained(
        string label,
        Control content,
        Control parent)
    {
        Rect2 parentRect = parent.GetGlobalRect();
        Rect2 contentRect = content.GetGlobalRect();
        if (!parentRect.Encloses(contentRect))
        {
            GD.PushError(
                $"{label} escaped macro viewport. Parent={parentRect}, content={contentRect}.");
        }
    }

    private void ShowFounderArrivalForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        CityMacroView city = GetNode<CityMacroView>(
            "GameUiShell/ScreenContent/CityMacroView");
        if (controller.World.Hero is not Citizen founder) return;
        city.PrepareFounderArrival();
        var arrival = new FounderArrivalSequence();
        AddChild(arrival);
        arrival.Begin(founder, city.GetFoundingArrivalGlobalPosition());
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
