using WorldofGoses.Domain;
namespace WorldofGoses.Persistence;

public sealed class CurrentHealthAndConditionSave
{
    public double? CurrentHealth { get; set; }
    public double? ConditionFactor { get; set; }
}
