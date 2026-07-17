using WorldofGoses.Domain;

namespace WorldofGoses.Tests;

/// <summary>
/// Builders for domain-level tests. Tests should construct
/// buildings and citizens through these helpers so the noise of
/// full constructors stays out of the assertions.
/// </summary>
internal static class TestHelpers
{
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
        var citizen = new Citizen(new CitizenId(id), $"Citizen-{id}", id * 11);
        if (miningExperience > 0) citizen.AddExperience(CompetencyId.Mining, miningExperience);
        return citizen;
    }

    /// <summary>Creates a citizen with experience in a specific competency.</summary>
    public static Citizen NewCitizen(int id, CompetencyId competency, int experience)
    {
        var citizen = new Citizen(new CitizenId(id), $"Citizen-{id}", id * 11);
        if (experience > 0) citizen.AddExperience(competency, experience);
        return citizen;
    }
}
