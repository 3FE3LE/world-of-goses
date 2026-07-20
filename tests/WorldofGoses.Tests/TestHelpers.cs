using WorldofGoses.Domain;

namespace WorldofGoses.Tests;

/// <summary>
/// Builders for domain-level tests. Tests should construct
/// buildings and citizens through these helpers so the noise of
/// full constructors stays out of the assertions.
/// </summary>
internal static class TestHelpers
{
    public static CitizenProfile NewProfile(LineageId? lineage = null)
    {
        bool created = CitizenProfile.TryCreate(
            lineage ?? LineageId.Ardhen,
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
        var result = world.TryCreateHero(new HeroCreationRequest("Aster", NewProfile()));
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
        var result = world.TryAuthorizeBasicShelter();
        if (!result.IsSuccess) throw new System.InvalidOperationException(result.Outcome.ToString());
        for (int i = 0; i < extraCitizens; i++)
        {
            world.RegisterCitizen(NewCitizen(100 + i));
        }
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
}
