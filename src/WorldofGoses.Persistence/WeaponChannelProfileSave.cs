using WorldofGoses.Domain;
namespace WorldofGoses.Persistence;

public sealed class WeaponChannelProfileSave
{
    public string Family { get; set; } = "";
    public double PhysicalTransfer { get; set; }
    public double ElementalResonance { get; set; }
}
