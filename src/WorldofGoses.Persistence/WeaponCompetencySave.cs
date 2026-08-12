using WorldofGoses.Domain;
namespace WorldofGoses.Persistence;

public sealed class WeaponCompetencySave
{
    public string Family { get; set; } = "";
    public int Level { get; set; }
    public double Experience { get; set; }
}
