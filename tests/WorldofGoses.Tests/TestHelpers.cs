using System.Linq;
using WorldofGoses.Domain;

namespace WorldofGoses.Tests;

/// <summary>
/// Builders for domain-level tests. Tests should construct
/// buildings and citizens through these helpers so the noise of
/// full constructors stays out of the assertions.
/// </summary>
internal static class TestHelpers
{
    public static void AdvanceToNextProductionCycle(CityWorld world)
    {
        do
        {
            world.AdvanceWorldTick();
        }
        while (!CityEconomyRules.IsProductionCycle(world.CurrentTick));
    }

    public static void AdvanceProductionCycles(CityWorld world, int cycleCount)
    {
        for (int cycle = 0; cycle < cycleCount; cycle++)
        {
            AdvanceToNextProductionCycle(world);
        }
    }

    public static CitizenProfile NewProfile(LineageId? lineage = null, GenderId? gender = null)
    {
        bool created = CitizenProfile.TryCreate(
            lineage ?? LineageId.Ardhen,
            gender ?? GenderId.Masculine,
            new[] { AptitudeId.Observation, AptitudeId.Empathy, AptitudeId.ManualPrecision },
            new[] { ProfessionFamilyId.Extraction, ProfessionFamilyId.MedicineCare, ProfessionFamilyId.ResearchEducation },
            ElementalAffinityId.Water,
            CombatStyleId.DefensiveSupport,
            new[] { WeaponPreferenceId.Polearm, WeaponPreferenceId.Shield },
            new[] { PersonalityTraitId.Patient, PersonalityTraitId.Protective, PersonalityTraitId.Reflective },
            PoliticalOrientationId.Communitarian,
            SpiritualPostureId.Contemplative,
            out CitizenProfile? profile,
            out string error);
        if (!created) throw new System.InvalidOperationException(error);
        return profile!;
    }

    public static CityWorld NewHeroWorld()
    {
        var world = new CityWorld();
        var result = world.TryCreateHero(new HeroCreationRequest("Aster", NewProfile(), GenderId.Masculine));
        if (!result.IsSuccess) throw new System.InvalidOperationException(result.Outcome.ToString());
        return world;
    }

    /// <summary>
    /// Explicit economic scenario used by production, assignment and
    /// mobilisation tests. It is test data, never the game's initial state.
    /// </summary>
    public static CityWorld NewConstructionWorld(int extraCitizens = 0)
    {
        var world = NewHeroWorld();
        world.SeedStartingForests();
        // Basic Shelter costs Wood × 4 (deposit = 1, remainder = 3).
        // Gather all 4 so the project can complete without stalling
        // mid-life; tests that want a stalled project can call
        // GatherWood themselves.
        world.GatherWood(new BuildingId(100), 4);
        var result = world.TryAuthorizeBasicShelter();
        if (!result.IsSuccess) throw new System.InvalidOperationException(result.Outcome.ToString());
        if (result.ProjectId is BuildingId projectId)
        {
            world.ConfirmCitizenArrivedAtAssignment(world.Hero!.Id, projectId);
        }
        for (int i = 0; i < extraCitizens; i++)
        {
            world.RegisterCitizen(NewCitizen(100 + i));
        }
        return world;
    }

    /// <summary>
    /// A world with the Basic Shelter already built (Home registered
    /// as a building) and zero projects. Also registers a placeholder
    /// Farm so Food deposits land somewhere (the Quarry construction
    /// recipe needs Iron + Food). Tests that need a Home but don't
    /// want to share state with the Basic Shelter project use this
    /// helper.
    /// </summary>
    public static CityWorld WorldWithHome()
    {
        var world = NewConstructionWorld();
        // The shelter project is in flight; fast-forward to completion
        // so the world is ready to authorise the next project.
        var projectId = world.Projects.Values.First().Id;
        FastForwardToCompletion(world, projectId);
        // Register a placeholder Farm so Food deposits land.
        var farm = new Building(
            id: new BuildingId(9001),
            displayName: "Test farm (placeholder)",
            kind: BuildingKind.Farm,
            producedResourceType: ResourceType.Food,
            producedCompetencyId: CompetencyId.Farming,
            workerCapacity: 4,
            visualCapacity: 2,
            baseProductionPerWorker: 1,
            storageCapacity: 1000,
            resourceLabel: "Food",
            resourceUnit: "food");
        world.RegisterBuilding(farm);
        return world;
    }

    public static CityWorld NewProductionWorld()
    {
        var world = NewHeroWorld();
        var hero = world.Hero!;
        hero.AddExperience(CompetencyId.Mining, 3);

        var quarry = NewBuilding(id: new BuildingId(1));
        var farm = NewBuilding(
            id: new BuildingId(2),
            kind: BuildingKind.Farm,
            producedCompetencyId: CompetencyId.Farming,
            producedResourceType: ResourceType.Food,
            workerCapacity: 4,
            visualCapacity: 2,
            storageCapacity: 30,
            displayName: "Test farm",
            resourceLabel: "Food",
            resourceUnit: "food");
        var home = new Building(
            id: new BuildingId(3),
            displayName: "Test home",
            kind: BuildingKind.Home,
            producedResourceType: ResourceType.Stone,
            producedCompetencyId: CompetencyId.Mining,
            workerCapacity: 5,
            visualCapacity: 5,
            baseProductionPerWorker: 0,
            storageCapacity: 0,
            resourceLabel: "Rest",
            resourceUnit: "rest",
            productionEnabled: false);
        world.RegisterBuilding(quarry);
        world.RegisterBuilding(farm);
        world.RegisterBuilding(home);

        // Operating recipes consume 1 iron per producing tick. Seed
        // enough iron in the Quarry so production tests can run a full
        // scenario without starving; Farm needs iron for the same reason.
        quarry.DepositIron(1000);
        farm.DepositIron(1000);

        var worker = NewCitizen(2, miningExperience: 1);
        var grower = NewCitizen(3, CompetencyId.Farming, experience: 3);
        world.RegisterCitizen(worker);
        world.RegisterCitizen(grower);
        world.RegisterCitizen(NewCitizen(4));
        world.RegisterCitizen(NewCitizen(5));

        hero.GrantRole(RoleId.Miner, world.CurrentTick);
        worker.GrantRole(RoleId.Miner, world.CurrentTick);
        world.TryAssignCitizen(quarry.Id, hero.Id);
        world.TryAssignCitizen(quarry.Id, worker.Id);
        world.TryAssignCitizen(farm.Id, grower.Id);
        hero.SetLocation(CitizenLocation.AtWork);
        worker.SetLocation(CitizenLocation.AtWork);
        grower.SetLocation(CitizenLocation.AtWork);
        return world;
    }

    /// <summary>
    /// Builds a <see cref="Building"/> with sensible defaults
    /// suitable for general tests. Override individual parameters
    /// for specific scenarios. Defaults: Quarry, mining, 6
    /// workers, 3 visible, 20 stock capacity, label "Stone".
    /// </summary>
    public static Building NewBuilding(
        BuildingId? id = null,
        BuildingKind kind = BuildingKind.Quarry,
        CompetencyId? producedCompetencyId = null,
        ResourceType producedResourceType = ResourceType.Stone,
        int workerCapacity = 6,
        int visualCapacity = 3,
        int baseProductionPerWorker = 1,
        int storageCapacity = 20,
        string displayName = "Test quarry",
        string resourceLabel = "Stone",
        string resourceUnit = "stone")
    {
        return new Building(
            id: id ?? new BuildingId(1),
            displayName: displayName,
            kind: kind,
            producedResourceType: producedResourceType,
            producedCompetencyId: producedCompetencyId ?? CompetencyId.Mining,
            workerCapacity: workerCapacity,
            visualCapacity: visualCapacity,
            baseProductionPerWorker: baseProductionPerWorker,
            storageCapacity: storageCapacity,
            resourceLabel: resourceLabel,
            resourceUnit: resourceUnit);
    }

    /// <summary>Creates a citizen with optional mining experience.</summary>
    public static Citizen NewCitizen(int id, int miningExperience = 0)
    {
        var citizen = new Citizen(new CitizenId(id), $"Citizen-{id}", id * 11, NewProfile());
        if (miningExperience > 0) citizen.AddExperience(CompetencyId.Mining, miningExperience);
        return citizen;
    }

    /// <summary>Creates a citizen with experience in a specific competency.</summary>
    public static Citizen NewCitizen(int id, CompetencyId competency, int experience)
    {
        var citizen = new Citizen(new CitizenId(id), $"Citizen-{id}", id * 11, NewProfile());
        if (experience > 0) citizen.AddExperience(competency, experience);
        return citizen;
    }

    /// <summary>
    /// Fast-forwards a project to completion by directly setting
    /// <see cref="ConstructionProject.Progress"/> to <see cref="ConstructionProject.RequiredWork"/>
    /// and triggering the world's completion pass. Returns the
    /// resulting <see cref="Building"/> registered in the world.
    /// </summary>
    public static Building FastForwardToCompletion(CityWorld world, BuildingId projectId)
    {
        var project = world.GetProject(projectId)!;
        // Advance the tick counter so the building is registered on
        // a fresh tick — the completion path emits events with the
        // current tick as their timestamp.
        world.AdvanceWorldTick();
        // Bypass the per-tick contribution loop: set Progress to
        // RequiredWork and call the world's completion hook directly
        // by triggering one more tick (the project draws inputs and
        // grants contributions; we just need Progress >= RequiredWork
        // for CompleteFinishedProjects to fire).
        // Use reflection-free helper: simulate by adding enough work
        // via assignment + many ticks.
        var citizen = world.Hero!;
        if (project.AssignedCount == 0)
        {
            world.TryAssignToProject(projectId, citizen.Id);
        }
        world.ConfirmCitizenArrivedAtAssignment(citizen.Id, projectId);
        int safety = 1000;
        while (project.Progress < project.RequiredWork && safety-- > 0)
        {
            world.AdvanceWorldTick();
        }
        var building = world.GetBuilding(projectId);
        if (building is null)
        {
            throw new System.InvalidOperationException(
                $"Project {projectId.Value} did not complete within the safety budget.");
        }
        return building;
    }
}
