#nullable enable
using WorldofGoses.Domain;
namespace WorldofGoses.Persistence;

/// <summary>
/// Materialised weapon instance identity introduced in #26. Owns
/// the <see cref="ItemInstanceId"/> plus the original family /
/// channel profile / origin that the future inventory/equipment
/// seam will hang on. Pre-v35 saves do not own this DTO; the
/// migration rebuilds it from the existing
/// <see cref="EquipmentLoadoutSave.Weapon"/> so no equipped weapon
/// is lost.
/// </summary>
public sealed class WeaponItemInstanceSave
{
    public string Id { get; set; } = "";
    public string Family { get; set; } = "";
    public WeaponChannelProfileSave Channels { get; set; } = new();
    public int Origin { get; set; }
}
