namespace WorldofGoses.Domain;

public sealed record CubeFaceCalculation(
    CubeFace Face,
    double BaseCubeValue,
    double GearSupport,
    double EffectiveCubeValue);
