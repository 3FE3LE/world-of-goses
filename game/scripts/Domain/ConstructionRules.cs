using System;

namespace WorldofGoses.Domain;

/// <summary>
/// Visual phase derived from a single progress ratio. Thresholds
/// are presentation-only; the domain stores a single
/// <see cref="ConstructionProject.Progress"/> value.
/// </summary>
public enum ConstructionVisualPhase
{
    Planned = 0,
    Started = 1,
    UnderConstruction = 2,
    Advanced = 3,
    NearlyComplete = 4,
    Complete = 5,
}

/// <summary>
/// Pure rules for a single <see cref="ConstructionProject"/>:
/// contribution per work interval, stamina cost, and visual
/// thresholds. All values are provisional tuning constants; they
/// do not describe architectural phases.
/// </summary>
public static class ConstructionRules
{
    /// <summary>Ticks between work intervals inside a single in-game day.</summary>
    public const int WorkIntervalTicks = 600;

    /// <summary>Total work required to finish the first Basic Shelter.</summary>
    public const int RequiredWork = 720;

    /// <summary>Maximum simultaneous contributors on the worksite.</summary>
    public const int WorkerCapacity = 4;

    /// <summary>Base contribution per interval, before individual modifiers.</summary>
    public const int BaseContributionPerWorkInterval = 40;

    /// <summary>Stamina paid by a contributing citizen per interval.</summary>
    public const int CostPerWorkInterval = 8;

    /// <summary>Contribution bonus for every relevant personal aptitude present in the profile.</summary>
    public const int AptitudeBonusPerAptitude = 4;

    /// <summary>Cap on the construction-experience bonus.</summary>
    public const int CompetencyBonusCap = 12;

    /// <summary>
    /// Per-interval contribution for a single contributor, given
    /// their current stamina and accumulated construction
    /// experience. Returns zero (and avoids consuming stamina) when
    /// the citizen cannot afford the cost; the project never
    /// subtracts work in this slice.
    /// </summary>
    public static int ContributionPerWorkInterval(Citizen citizen)
    {
        if (citizen is null) return 0;
        if (citizen.CurrentStamina < CostPerWorkInterval) return 0;

        int experience = citizen.GetExperience(CompetencyId.Construction);
        int bonus = CompetencyBonusAt(experience);
        int aptitudeBonus = AptitudeBonus(citizen.Profile);
        return BaseContributionPerWorkInterval + aptitudeBonus + bonus;
    }

    /// <summary>Stamina cost per interval for a single contributor.</summary>
    public static int StaminaCostPerWorkInterval() => CostPerWorkInterval;

    /// <summary>Cap-aware competency bonus for a given accumulated experience value.</summary>
    public static int CompetencyBonusAt(int experience)
    {
        int clamped = Math.Min(experience, 24);
        return (clamped / 8) * 4;
    }

    /// <summary>Bonus per matching personal aptitude present in the citizen's profile.</summary>
    public static int AptitudeBonus(CitizenProfile profile)
    {
        if (profile is null) return 0;
        int matches = 0;
        foreach (var aptitude in profile.Aptitudes)
        {
            if (IsRelevantAptitude(aptitude)) matches++;
        }
        return matches * AptitudeBonusPerAptitude;
    }

    /// <summary>Visual phase for the given progress ratio.</summary>
    public static ConstructionVisualPhase PhaseFor(int progress, int requiredWork)
    {
        if (requiredWork <= 0 || progress <= 0) return ConstructionVisualPhase.Planned;
        if (progress >= requiredWork) return ConstructionVisualPhase.Complete;
        int percent = (int)((long)progress * 100 / requiredWork);
        if (percent < 1) return ConstructionVisualPhase.Planned;
        if (percent < 20) return ConstructionVisualPhase.Started;
        if (percent < 50) return ConstructionVisualPhase.UnderConstruction;
        if (percent < 80) return ConstructionVisualPhase.Advanced;
        return ConstructionVisualPhase.NearlyComplete;
    }

    /// <summary>Single human-readable description for a visual phase.</summary>
    public static string Describe(ConstructionVisualPhase phase) => phase switch
    {
        ConstructionVisualPhase.Planned => "Planned",
        ConstructionVisualPhase.Started => "Started",
        ConstructionVisualPhase.UnderConstruction => "Under construction",
        ConstructionVisualPhase.Advanced => "Advanced",
        ConstructionVisualPhase.NearlyComplete => "Nearly complete",
        ConstructionVisualPhase.Complete => "Complete",
        _ => "Unknown",
    };

    private static bool IsRelevantAptitude(AptitudeId aptitude)
    {
        return aptitude == AptitudeId.Strength
            || aptitude == AptitudeId.ManualPrecision
            || aptitude == AptitudeId.Observation
            || aptitude == AptitudeId.Adaptability;
    }
}
