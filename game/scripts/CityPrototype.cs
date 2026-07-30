#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;
using WorldofGoses.Prototypes;

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
                    "GameUiShell/ScreenContent/OfflineReportPanel")
                    .ShowVisualRegressionReport(BuildVisualOfflineReport());
                break;
            case "pause-menu":
                GetNode<PauseMenu>("PauseMenu").ShowForVisualRegression(confirmReset: false);
                break;
            case "pause-menu-reset":
                GetNode<PauseMenu>("PauseMenu").ShowForVisualRegression(confirmReset: true);
                break;
            case "construction-scroll":
                GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
                    .ShowConstructionForVisualRegression(placement: false);
                break;
            case "construction-placement":
                GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
                    .ShowConstructionForVisualRegression(placement: true);
                break;
            case "expedition-idle":
                ShowExpeditionForVisualRegression(ExpeditionFixtureState.Idle);
                break;
            case "hero-incorporation":
                ShowHeroIncorporationForVisualRegression();
                break;
            case "wound-recovery":
                ShowWoundRecoveryForVisualRegression();
                break;
            case "expedition-active":
                ShowExpeditionForVisualRegression(ExpeditionFixtureState.Active);
                break;
            case "expedition-returned":
                ShowExpeditionForVisualRegression(ExpeditionFixtureState.Returned);
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
            case "language-selector":
                GetNode<PauseMenu>("PauseMenu").Open();
                break;
            case "forest-depleted":
                GetNode<CityWorldController>("CityWorldController")
                    .DrainAllForestsForVisualRegression();
                break;
            case "citizen-travel":
                ShowCitizenTravelForVisualRegression();
                break;
            case "policies":
                GetNode<PoliciesPanel>("GameUiShell/ScreenContent/PoliciesPanel").Open();
                break;
            case "migrant":
                GetNode<MigrantPanel>("GameUiShell/ScreenContent/MigrantPanel")
                    .ShowForVisualRegression();
                break;
        }
    }

    private void ShowCitizenTravelForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        MacroStreetLiveView city = GetNode<MacroStreetLiveView>(
            "GameUiShell/ScreenContent/MacroStreetLiveView");
        CityWorld world = controller.World;
        if (world.Hero is not Citizen founder)
        {
            GD.PushError("Citizen travel fixture requires a founded city.");
            return;
        }

        while (!GameClock.IsDaytime(world.CurrentTick)) world.AdvanceWorldTick();
        Building? workplace = world.Buildings.Values
            .Where(building => building.Kind is BuildingKind.Quarry or BuildingKind.Farm)
            .OrderByDescending(building => building.AssignedCitizenIds.Count < building.WorkerCapacity)
            .ThenBy(building => building.Id.Value)
            .FirstOrDefault();
        if (workplace is null || world.PrimaryHome is null)
        {
            GD.PushError("Citizen travel fixture requires a Shelter and a Farm or Quarry.");
            return;
        }
        if (workplace.AssignedCitizenIds.Count >= workplace.WorkerCapacity)
        {
            CitizenId released = workplace.AssignedCitizenIds.Last();
            world.TryUnassignCitizen(workplace.Id, released);
        }
        if (workplace.Stock > 0) workplace.TryConsumeStock(workplace.Stock);
        workplace.ConfigureProductionPolicy(
            enabled: true,
            minStock: 0,
            maxStock: workplace.StorageCapacity,
            priority: workplace.Priority);

        int nextId = world.Citizens.Keys.Max(id => id.Value) + 1;
        var traveller = new Citizen(
            new CitizenId(nextId),
            "Travel proof",
            appearanceSeed: nextId * 11,
            profile: founder.Profile);
        world.RegisterCitizen(traveller);
        AssignmentResult assignment = controller.TryAssignCitizen(workplace.Id, traveller.Id);
        bool activeRoute = city.HasActiveCitizenJourneyForVisualRegression(traveller.Id);
        if (!assignment.IsSuccess
            || traveller.CurrentLocation != CitizenLocation.InTransit
            || !activeRoute)
        {
            GD.PushError(
                $"Citizen travel fixture failed: assignment={assignment.Outcome}, " +
                $"location={traveller.CurrentLocation}, activeRoute={activeRoute}.");
            return;
        }
        GD.Print(
            $"Citizen travel fixture passed: citizen={traveller.Id.Value}, " +
            $"destination={workplace.Id.Value}, activeRoute=true.");
    }

    private async void ValidateModalLayoutAndClosePaths()
    {
        GD.Print("Modal layout/close fixture started.");
        Control city = GetNode<Control>("GameUiShell/ScreenContent");
        ModalHost host = GetNode<ModalHost>(
            "GameUiShell/ScreenContent/ModalHost");
        ExpeditionPanel expedition = GetNode<ExpeditionPanel>(
            "GameUiShell/ScreenContent/ExpeditionPanel");
        ConstructionPanel construction = GetNode<ConstructionPanel>(
            "GameUiShell/ScreenContent/Center/ConstructionPanel");

        expedition.Open();
        await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        ValidateContained("ExpeditionPanel", expedition, city);
        expedition.Close();
        await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);

        GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
            .ShowConstructionForVisualRegression(placement: false);
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
        MacroStreetLiveView city = GetNode<MacroStreetLiveView>(
            "GameUiShell/ScreenContent/MacroStreetLiveView");
        if (controller.World.Hero is not Citizen founder) return;
        city.PrepareFounderArrival();
        var arrival = new FounderArrivalSequence();
        AddChild(arrival);
        arrival.Begin(founder, city.GetFoundingArrivalGlobalPosition());
    }

    private enum ExpeditionFixtureState { Idle, Active, Returned }

    private void ShowHeroIncorporationForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        if (controller.World.Hero is not Citizen founder) return;
        if (controller.World.Citizens.Values.All(citizen => citizen.IsHero))
        {
            int nextId = controller.World.Citizens.Keys.Max(id => id.Value) + 1;
            controller.World.RegisterCitizen(new Citizen(
                new CitizenId(nextId),
                "Expedition candidate",
                appearanceSeed: nextId * 11,
                profile: founder.Profile));
        }
        ShowExpeditionForVisualRegression(ExpeditionFixtureState.Idle);
    }

    private void ShowWoundRecoveryForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        if (controller.World.Hero is not Citizen founder) return;
        int nextId = controller.World.Citizens.Keys.Max(id => id.Value) + 1;
        var patient = new Citizen(
            new CitizenId(nextId),
            "Tamara",
            appearanceSeed: nextId * 11,
            profile: founder.Profile);
        controller.World.RegisterCitizen(patient);
        controller.World.TryIncorporateHero(patient.Id);
        WorldEvent woundEvent = controller.World.Log.Record(
            controller.World.CurrentTick,
            WorldEventKind.WoundSustained,
            WorldEventSubject.Citizen(patient.Id, patient.Name),
            (int)WoundSeverity.Moderate);
        patient.SustainWound(WoundSeverity.Moderate, woundEvent.Id);
        controller.World.Resources.DepositToCityInventory(ResourceType.Food, 2);
        GetNode<ExpeditionPanel>("GameUiShell/ScreenContent/ExpeditionPanel")
            .ShowWoundRecoveryForVisualRegression();
    }

    private void ShowExpeditionForVisualRegression(ExpeditionFixtureState state)
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        ExpeditionPanel panel = GetNode<ExpeditionPanel>("GameUiShell/ScreenContent/ExpeditionPanel");
        if (controller.World.Hero?.CurrentAssignment is BuildingId assignment)
        {
            AssignmentResult result = controller.World.TryUnassignCitizen(assignment, controller.World.Hero.Id);
            if (!result.IsSuccess) controller.World.TryUnassignFromProject(assignment, controller.World.Hero.Id);
        }
        if (controller.World.Resources.Available(ResourceType.Wood) < 1)
        {
            controller.World.Resources.DepositToCityInventory(ResourceType.Wood, 1);
        }
        foreach (Expedition expedition in controller.World.Expeditions.Values)
        {
            if (expedition.Status == ExpeditionStatus.Active)
            {
                controller.CancelExpedition(expedition.Id);
                break;
            }
        }
        if (state == ExpeditionFixtureState.Idle)
        {
            panel.Open();
            return;
        }
        ExpeditionRequest request = ExpeditionRequest.Reconnaissance(controller.World.Hero!.Id);
        if (state == ExpeditionFixtureState.Returned) request = request with { DurationTicks = 1 };
        if (!controller.StartExpedition(request).IsSuccess) return;
        if (state == ExpeditionFixtureState.Returned) controller.World.AdvanceWorldTick();
        panel.Open();
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
