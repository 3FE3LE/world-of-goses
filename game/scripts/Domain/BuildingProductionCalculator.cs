using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// Deterministic calculation of the production a building yields per
/// tick given its currently assigned citizens. The formula is
/// intentionally explicit and small so the relationship between
/// workers, experience, and output is obvious.
///
/// Per worker: baseProduction + floor(baseProduction * 0.05 * competencyExperience).
/// </summary>
public static class BuildingProductionCalculator
{
    public static int ProductionPerTick(Building building, IReadOnlyDictionary<CitizenId, Citizen> citizens)
    {
        if (building.AssignedCount == 0) return 0;

        int total = 0;
        foreach (var citizenId in building.AssignedCitizenIds)
        {
            if (!citizens.TryGetValue(citizenId, out var citizen)) continue;
            total += WorkerContribution(citizen, building.ProducedCompetencyId, building.BaseProductionPerWorker);
        }
        return total;
    }

    public static int WorkerContribution(Citizen citizen, CompetencyId competency, int baseProductionPerWorker)
    {
        int experience = citizen.GetExperience(competency);
        int bonus = (baseProductionPerWorker * experience) / 20;
        return baseProductionPerWorker + bonus;
    }
}
