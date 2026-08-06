#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
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
    /// <summary>
    /// Top-level back key. Iterates the input tree so a single ESC
    /// pulse closes exactly one overlay:
    /// <list type="number">
    /// <item>Topmost modal — <see cref="ModalHost"/> is the leafmost
    /// listener and eats the input via <c>SetInputAsHandled</c> when
    /// <c>IsOpen</c>; no further handler runs.</item>
    /// <item>Pause menu — closes itself when visible; otherwise
    /// deliberately lets the event propagate instead of opening
    /// (the pause menu has its own button, see <see cref="PauseMenu"/>).</item>
    /// <item>Hero profile / building detail — this handler at the
    /// scene root returns to <see cref="CityWorldController.Selection.MacroView"/>
    /// via <see cref="CityWorldController.ReturnToCity"/>.</item>
    /// </list>
    /// Without this fallback the player had no way to leave the hero
    /// profile or building detail with the keyboard once a modal
    /// had been opened and dismissed.
    /// </summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("ui_cancel")) return;
        CityWorldController controller = GetNodeOrNull<CityWorldController>("CityWorldController");
        if (controller is null) return;
        controller.ReturnToCity();
        GetViewport().SetInputAsHandled();
    }

    public override void _Ready()
    {
        GD.Print("World of Goses prototype starting.");
        var firstNightScene = new FirstNightScene
        {
            Name = "FirstNightScene",
            ControllerPath = new NodePath("CityWorldController"),
        };
        AddChild(firstNightScene);
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
            case "construction-placement-hover-invalid":
                GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
                    .ShowConstructionPlacementHoverForVisualRegression(valid: false);
                break;
            case "founding-blueprint":
                ShowFoundingSiteForVisualRegression(moduleChoice: false, blockedCargo: false);
                break;
            case "founding-module-choice":
                ShowFoundingSiteForVisualRegression(moduleChoice: true, blockedCargo: false);
                break;
            case "founding-blocked-cargo":
                ShowFoundingSiteForVisualRegression(moduleChoice: false, blockedCargo: true);
                break;
            case "early-game-resources":
                ShowEarlyGameResourcesForVisualRegression();
                break;
            case "shelter-resources":
                ShowShelterResourcesForVisualRegression();
                break;
            case "cultivation-prepared":
                ShowCultivationForVisualRegression();
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
            case "world-status-treatment":
                ShowWorldStatusTreatmentForVisualRegression();
                break;
            case "citizen-click-summary":
                ShowCitizenClickSummaryForVisualRegression();
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
            // The four moments the ambient day/night curve is built
            // around. Reviewing the tint needs a pinned hour: otherwise
            // the captured time is whatever the save happens to hold.
            case "time-midnight":
                PinTimeOfDayForVisualRegression(0.0);
                break;
            case "time-dawn":
                PinTimeOfDayForVisualRegression(0.229);
                break;
            case "time-noon":
                PinTimeOfDayForVisualRegression(0.5);
                break;
            case "time-dusk":
                PinTimeOfDayForVisualRegression(0.771);
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
            case "camera-depth-third-row":
                GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
                    .ShowThirdStreetDepthForVisualRegression();
                break;
            case "long-terrarium-20-rows":
                ShowLongTerrariumForVisualRegression(additionalRows: 20);
                break;
            case "long-terrarium-16-rows":
                ShowLongTerrariumForVisualRegression(additionalRows: 15);
                break;
            case "terrarium-8x9-window":
                ShowTerrariumWindowForVisualRegression(rows: 8, columns: 9);
                break;
            case "policies":
                GetNode<PoliciesPanel>("GameUiShell/ScreenContent/PoliciesPanel").Open();
                break;
            case "combat-debug":
                ShowCombatDebugForVisualRegression();
                break;
            case "migrant":
                GetNode<MigrantPanel>("GameUiShell/ScreenContent/MigrantPanel")
                    .ShowForVisualRegression();
                break;
        }
    }

    /// <summary>
    /// Adds the combat debug panel at runtime and resolves one expedition, so the
    /// slice is reachable in the engine without editing CityPrototype.tscn. The
    /// panel is developer scaffolding; the player-facing preparation screen belongs
    /// to a later slice.
    /// </summary>
    private void ShowCombatDebugForVisualRegression()
    {
        // Parent to the UI layer so the panel gets a real rect and draws above the
        // HUD. The scene root is not a Control, so anchoring against it yields the
        // panel's minimum size instead of the screen.
        Node host = GetNodeOrNull<Control>("GameUiShell/ScreenContent") ?? (Node)this;
        var panel = new CombatDebugPanel
        {
            Name = "CombatDebugPanel",
            ControllerPath = host == this
                ? "../CityWorldController"
                : "../../../CityWorldController",
        };
        host.AddChild(panel);
        panel.Open();
        panel.RunForVisualRegression();
    }

    private void ShowLongTerrariumForVisualRegression(int additionalRows)
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        Citizen? loadedFounder = controller.World.Hero;
        var fixture = new CityWorld();
        if (loadedFounder is not null)
        {
            HeroCreationResult heroResult = fixture.TryCreateHero(new HeroCreationRequest(
                "Aster",
                loadedFounder.Profile,
                loadedFounder.Profile.Gender));
            if (!heroResult.IsSuccess)
            {
                GD.PushError($"Long-terrarium fixture could not create founder: {heroResult.Outcome}.");
                return;
            }
            fixture.SeedStartingForests();
            fixture.SeedStartingOpportunities();
        }

        WorldSave save = WorldPersistence.Capture(fixture);
        AddTerrariumRowsForVisualRegression(save, additionalRows);
        controller.World.Restore(save);
        GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
            .ShowLongTerrariumForVisualRegression();
    }

    private void ShowTerrariumWindowForVisualRegression(int rows, int columns)
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        Citizen? loadedFounder = controller.World.Hero;
        var fixture = new CityWorld();
        if (loadedFounder is not null)
        {
            HeroCreationResult heroResult = fixture.TryCreateHero(new HeroCreationRequest(
                "Aster",
                loadedFounder.Profile,
                loadedFounder.Profile.Gender));
            if (!heroResult.IsSuccess)
            {
                GD.PushError($"Terrarium fixture could not create founder: {heroResult.Outcome}.");
                return;
            }
            fixture.SeedStartingForests();
            fixture.SeedStartingOpportunities();
        }

        WorldSave save = WorldPersistence.Capture(fixture);
        ResizeTerrariumForVisualRegression(save, rows, columns);
        controller.World.Restore(save);
        GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
            .ShowLongTerrariumForVisualRegression();
    }

    internal static void ResizeTerrariumForVisualRegression(
        WorldSave save,
        int rows,
        int columns)
    {
        if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
        if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns));

        int existingColumnCount = save.Parcels
            .Select(parcel => parcel.LogicalColumn)
            .DefaultIfEmpty(-1)
            .Max() + 1;
        if (columns < existingColumnCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(columns),
                "Terrarium fixture cannot discard existing parcel columns.");
        }
        int addedColumns = columns - existingColumnCount;
        if (addedColumns % 2 != 0)
        {
            throw new ArgumentException(
                "Terrarium fixture must add the same number of columns on both sides.",
                nameof(columns));
        }
        int leftParcelColumns = addedColumns / 2;
        if (leftParcelColumns > 0)
        {
            foreach (ParcelSave parcel in save.Parcels)
            {
                parcel.LogicalColumn += leftParcelColumns;
            }
            int leftLotColumns = leftParcelColumns * ParcelGrid.LotsPerAxis;
            int leftFrontageColumns =
                leftParcelColumns * ParcelGrid.FrontageColumnsPerParcel;
            foreach (ParcelPlacementSave placement in save.ParcelPlacements)
            {
                placement.LotColumn += leftLotColumns;
                placement.StartColumn += leftFrontageColumns;
            }
            foreach (CorridorReservationSave corridor in save.CorridorReservations)
            {
                corridor.StartColumn += leftFrontageColumns;
            }
        }

        var existing = save.Parcels
            .ToDictionary(parcel => (parcel.LogicalRow, parcel.LogicalColumn));
        int nextParcelId = save.Parcels
            .Select(parcel => parcel.Id)
            .DefaultIfEmpty(0)
            .Max() + 1;
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                if (existing.ContainsKey((row, column))) continue;
                save.Parcels.Add(new ParcelSave
                {
                    Id = nextParcelId++,
                    LogicalColumn = column,
                    LogicalRow = row,
                    IsUnlocked = true,
                    TerritoryState = ParcelTerritoryState.Available.ToString(),
                });
            }
        }
    }

    internal static void AddTerrariumRowsForVisualRegression(
        WorldSave save,
        int additionalRows)
    {
        if (additionalRows < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(additionalRows));
        }
        if (additionalRows == 0) return;

        int columnCount = save.Parcels
            .Where(parcel => parcel.LogicalRow == 0)
            .Select(parcel => parcel.LogicalColumn)
            .DefaultIfEmpty(-1)
            .Max() + 1;
        if (columnCount <= 0)
        {
            throw new InvalidOperationException(
                "Long-terrarium fixture requires an initialized founding parcel row.");
        }
        int firstNewRow = save.Parcels
            .Select(parcel => parcel.LogicalRow)
            .DefaultIfEmpty(-1)
            .Max() + 1;
        int nextParcelId = save.Parcels
            .Select(parcel => parcel.Id)
            .DefaultIfEmpty(0)
            .Max() + 1;

        for (int row = firstNewRow; row < firstNewRow + additionalRows; row++)
        {
            for (int column = 0; column < columnCount; column++)
            {
                save.Parcels.Add(new ParcelSave
                {
                    Id = nextParcelId++,
                    LogicalColumn = column,
                    LogicalRow = row,
                    IsUnlocked = true,
                    TerritoryState = ParcelTerritoryState.Available.ToString(),
                });
            }
        }
    }

    private void ShowFoundingSiteForVisualRegression(bool moduleChoice, bool blockedCargo)
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        Citizen? loadedFounder = controller.World.Hero;
        if (loadedFounder is null)
        {
            GD.PushError("Founding Site visual fixture requires a loaded founder profile.");
            return;
        }

        var fixture = new CityWorld();
        HeroCreationResult heroResult = fixture.TryCreateHero(new HeroCreationRequest(
            "Aster",
            loadedFounder.Profile,
            loadedFounder.Profile.Gender));
        if (!heroResult.IsSuccess)
        {
            GD.PushError($"Founding Site visual fixture could not create founder: {heroResult.Outcome}.");
            return;
        }
        fixture.SeedStartingForests();
        fixture.SeedStartingOpportunities();
        fixture.Resources.DepositToCityInventory(ResourceType.Branches, 3);
        fixture.Resources.DepositToCityInventory(ResourceType.SmallStone, 2);

        if (moduleChoice || blockedCargo)
        {
            ConstructionAuthorizationResult authorization =
                fixture.TryAuthorizeConstruction(ConstructionKind.FoundingSite);
            if (!authorization.IsSuccess || authorization.ProjectId is not BuildingId projectId)
            {
                GD.PushError($"Founding Site visual fixture authorization failed: {authorization.Outcome}.");
                return;
            }
            ConstructionProject project = fixture.GetProject(projectId)!;
            project.Progress = project.RequiredWork;
            fixture.AdvanceWorldTick();
            if (blockedCargo)
            {
                fixture.TryUnassignFromProject(project.Id, fixture.Hero!.Id);
                fixture.Resources.DepositToCityInventory(
                    ResourceType.WildFood,
                    FoundingSiteRules.CarriedCapacity);
            }
            else
            {
                fixture.Resources.DepositToCityInventory(ResourceType.Branches, 2);
                fixture.Resources.DepositToCityInventory(ResourceType.PlantFiber, 3);
            }
        }

        controller.World.Restore(WorldPersistence.Capture(fixture));
        GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
            .ShowConstructionForVisualRegression(placement: false);
    }

    private void ShowEarlyGameResourcesForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        bool profileCreated = CitizenProfile.TryCreate(
            LineageId.Ardhen,
            GenderId.Masculine,
            new[] { AptitudeId.Observation, AptitudeId.Empathy, AptitudeId.ManualPrecision },
            new[] { ProfessionFamilyId.Extraction, ProfessionFamilyId.MedicineCare, ProfessionFamilyId.ResearchEducation },
            ElementalAffinityId.Water,
            CombatStyleId.DefensiveSupport,
            new[] { WeaponPreferenceId.Polearm, WeaponPreferenceId.Shield },
            new[] { PersonalityTraitId.Patient, PersonalityTraitId.Protective, PersonalityTraitId.Reflective },
            PoliticalOrientationId.Communitarian,
            SpiritualPostureId.Contemplative,
            out CitizenProfile? profile,
            out string profileError);
        if (!profileCreated || profile is null)
        {
            GD.PushError($"Early-game resource fixture could not create a profile: {profileError}.");
            return;
        }

        var fixture = new CityWorld();
        HeroCreationResult heroResult = fixture.TryCreateHero(new HeroCreationRequest(
            "Aster",
            profile,
            profile.Gender));
        if (!heroResult.IsSuccess)
        {
            GD.PushError($"Early-game resource fixture could not create founder: {heroResult.Outcome}.");
            return;
        }
        fixture.SeedStartingForests();
        fixture.SeedStartingOpportunities();
        controller.World.Restore(WorldPersistence.Capture(fixture));
        GetNode<AstralOnboardingView>("OnboardingView").Hide();
        GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
            .ShowEarlyGameResourcesForVisualRegression();
    }

    private void ShowShelterResourcesForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        Citizen? loadedFounder = controller.World.Hero;
        if (loadedFounder is null)
        {
            GD.PushError("Shelter resources visual fixture requires a loaded founder profile.");
            return;
        }

        var fixture = new CityWorld();
        HeroCreationResult heroResult = fixture.TryCreateHero(new HeroCreationRequest(
            "Aster",
            loadedFounder.Profile,
            loadedFounder.Profile.Gender));
        if (!heroResult.IsSuccess)
        {
            GD.PushError($"Shelter resources visual fixture could not create founder: {heroResult.Outcome}.");
            return;
        }
        fixture.SeedStartingForests();
        fixture.SeedStartingOpportunities();
        fixture.Resources.DepositToCityInventory(ResourceType.Wood, 4);
        ConstructionAuthorizationResult shelter =
            fixture.TryAuthorizeConstruction(ConstructionKind.BasicShelter);
        if (!shelter.IsSuccess || shelter.ProjectId is not BuildingId shelterId)
        {
            GD.PushError($"Shelter resources visual fixture could not authorize shelter: {shelter.Outcome}.");
            return;
        }
        ConstructionProject shelterProject = fixture.GetProject(shelterId)!;
        shelterProject.Progress = shelterProject.RequiredWork;
        fixture.AdvanceWorldTick();
        fixture.ConfirmCitizenArrivedHome(fixture.Hero!.Id);
        fixture.Resources.DepositToCityInventory(ResourceType.Branches, 3);
        fixture.Resources.DepositToCityInventory(ResourceType.PlantFiber, 2);
        fixture.Resources.DepositToCityInventory(ResourceType.SmallStone, 2);
        fixture.Resources.DepositToCityInventory(ResourceType.WildFood, 4);

        controller.World.Restore(WorldPersistence.Capture(fixture));
        GetNode<AstralOnboardingView>("OnboardingView").Hide();
        controller.SelectBuilding(shelterId);
        GetNode<BuildingDetailView>("GameUiShell/ScreenContent/BuildingDetailView")
            .ExpandShelterResourcesForVisualRegression();
    }

    private void ShowCultivationForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        Citizen? loadedFounder = controller.World.Hero;
        if (loadedFounder is null)
        {
            GD.PushError("Cultivation visual fixture requires a loaded founder profile.");
            return;
        }

        var fixture = new CityWorld();
        HeroCreationResult heroResult = fixture.TryCreateHero(new HeroCreationRequest(
            "Aster",
            loadedFounder.Profile,
            loadedFounder.Profile.Gender));
        if (!heroResult.IsSuccess)
        {
            GD.PushError($"Cultivation visual fixture could not create founder: {heroResult.Outcome}.");
            return;
        }

        fixture.SeedStartingForests();
        fixture.SeedStartingOpportunities();
        fixture.Resources.DepositToCityInventory(ResourceType.Wood, 4);
        ConstructionAuthorizationResult shelter =
            fixture.TryAuthorizeConstruction(ConstructionKind.BasicShelter);
        if (!shelter.IsSuccess || shelter.ProjectId is not BuildingId shelterId)
        {
            GD.PushError($"Cultivation visual fixture could not authorize shelter: {shelter.Outcome}.");
            return;
        }
        ConstructionProject shelterProject = fixture.GetProject(shelterId)!;
        shelterProject.Progress = shelterProject.RequiredWork;
        fixture.AdvanceWorldTick();
        fixture.ConfirmCitizenArrivedHome(fixture.Hero!.Id);

        fixture.Resources.DepositToCityInventory(ResourceType.Branches, 1);
        fixture.Resources.DepositToCityInventory(ResourceType.SmallStone, 1);
        fixture.Resources.DepositToCityInventory(ResourceType.WildFood, 8);
        ConstructionAuthorizationResult cultivation =
            fixture.TryAuthorizeConstruction(ConstructionKind.CultivationSite);
        if (!cultivation.IsSuccess || cultivation.ProjectId is not BuildingId cultivationId)
        {
            GD.PushError(
                $"Cultivation visual fixture could not authorize site: {cultivation.Outcome}.");
            return;
        }
        ConstructionProject cultivationProject = fixture.GetProject(cultivationId)!;
        cultivationProject.Progress = cultivationProject.RequiredWork;
        fixture.AdvanceWorldTick();
        while (!GameClock.IsDaytime(fixture.CurrentTick)) fixture.AdvanceWorldTick();

        controller.World.Restore(WorldPersistence.Capture(fixture));
        GetNode<AstralOnboardingView>("OnboardingView").Hide();
        GetNode<CityStatusPanel>("GameUiShell/CityStatusPanel").Refresh(controller);
        GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
            .ShowEarlyGameResourcesForVisualRegression();
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

    private void PinTimeOfDayForVisualRegression(double dayFraction)
    {
        GetNode<TimeOfDayFilter>("GameUiShell/ScreenContent/TimeOfDayFilter")
            .PinDayFractionForVisualRegression(dayFraction);
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

    private void ShowWorldStatusTreatmentForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        CityWorld world = controller.World;
        if (world.Hero is not Citizen founder || world.PrimaryHome is null) return;

        int nextId = world.Citizens.Keys.Max(id => id.Value) + 1;
        var patient = new Citizen(
            new CitizenId(nextId),
            "Tamara",
            appearanceSeed: nextId * 11,
            profile: founder.Profile);
        world.RegisterCitizen(patient);
        world.TryIncorporateHero(patient.Id);
        WorldEvent woundEvent = world.Log.Record(
            world.CurrentTick,
            WorldEventKind.WoundSustained,
            WorldEventSubject.Citizen(patient.Id, patient.Name),
            (int)WoundSeverity.Moderate);
        patient.SustainWound(WoundSeverity.Moderate, woundEvent.Id);
        world.Resources.DepositToCityInventory(ResourceType.Food, WoundRules.ModerateFoodCost);
        if (!world.TryBeginWoundRecovery(patient.Id).IsSuccess) return;

        GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
            .ShowCitizenStatusForVisualRegression(patient.Id);
    }

    private void ShowCitizenClickSummaryForVisualRegression()
    {
        // The bubble fixture above already drives the hover path; this one
        // exercises the dedicated click path (TryClick → SelectCitizen →
        // SelectionInfoPanel) so the regression matrix proves both the
        // pointer overlay and the at-a-glance summary arrive when the
        // player clicks a citizen — not just when the macro view paints
        // the bubble for a known citizen by hand.
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        CityWorld world = controller.World;
        if (world.Hero is not Citizen founder || world.PrimaryHome is null) return;

        int nextId = world.Citizens.Keys.Max(id => id.Value) + 1;
        var inspector = new Citizen(
            new CitizenId(nextId),
            "Inspector",
            appearanceSeed: nextId * 11,
            profile: founder.Profile);
        world.RegisterCitizen(inspector);
        MacroStreetLiveView city = GetNode<MacroStreetLiveView>(
            "GameUiShell/ScreenContent/MacroStreetLiveView");
        city.TriggerCitizenClickForVisualRegression(inspector.Id);
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
