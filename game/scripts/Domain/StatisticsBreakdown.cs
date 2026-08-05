using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>Reusable domain explanation of one calculated statistic.</summary>
public sealed record StatisticsBreakdown(
    string StatisticId,
    double BaseCubeValue,
    double GearSupport,
    double EffectiveCubeValue,
    double? WeaponCoefficient,
    double? SkillFactor,
    double? ConditionFactor,
    double? CitySupportFactor,
    IReadOnlyList<CubeFaceCalculation> FaceCalculations,
    IReadOnlyDictionary<string, double> IntermediateScores,
    double FinalValue,
    double? AppliedCap,
    bool WasCapped);
