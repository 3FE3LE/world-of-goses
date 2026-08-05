namespace WorldofGoses.Domain;

/// <summary>
/// Pending technique contract. Concrete physical/elemental coefficients are
/// intentionally outside the general statistics calculator and v0.1 scope.
/// </summary>
public interface ITechniqueDamageFormula
{
    double CalculateRawDamage(double physicalChannelPower, double elementalChannelPower);
}
