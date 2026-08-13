#nullable enable
using System.Collections.Generic;
namespace WorldofGoses.Persistence;

/// <summary>
/// Per-citizen personal-equipment DTO introduced in #26. Holds the
/// registry of weapon instances the citizen owns and which one is
/// equipped. Future helmet/chest/legs/boots/gloves ids extend this
/// record additively.
/// </summary>
public sealed class PersonalEquipmentSave
{
    public List<WeaponItemInstanceSave> Weapons { get; set; } = new();
    /// <summary>The id of the equipped weapon. <c>null</c> on a
    /// citizen with no equipment.</summary>
    public string? EquippedWeaponId { get; set; }
}
