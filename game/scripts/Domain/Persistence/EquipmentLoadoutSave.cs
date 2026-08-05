#nullable enable
namespace WorldofGoses.Domain.Persistence;

public sealed class EquipmentLoadoutSave
{
    public WeaponChannelProfileSave? Weapon { get; set; }
    public GearSupportProfileSave Helmet { get; set; } = new();
    public GearSupportProfileSave Chest { get; set; } = new();
    public GearSupportProfileSave Legs { get; set; } = new();
    public GearSupportProfileSave Boots { get; set; } = new();
    public GearSupportProfileSave Gloves { get; set; } = new();
}
