#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain.Combat;

public enum CombatSide
{
    Party,
    Enemy,
}

/// <summary>Semantic stature for presentation; it never expands the body radius.</summary>
public enum CombatStature
{
    Small,
    Standard,
    Tall,
    Large,
}

/// <summary>
/// Injuries a citizen can carry away from an expedition. Persist independently
/// from health: healing life does not clear an injury.
/// </summary>
public enum InjuryKind
{
    Contusion,
    OpenWound,
    TemporaryIncapacitation,

    /// <summary>
    /// A bone that gave. The only injury an encounter can hand out for a reason
    /// other than how much health was lost: it comes from the Fracture status
    /// still being active when the fight ends.
    /// </summary>
    Fracture,
}

/// <summary>
/// The mutable per-encounter state of one participant. This is a working copy for
/// the duration of an encounter, NOT a second persistent person: a party member
/// carries the <see cref="CitizenId"/> of the real
/// <see cref="Citizen"/> it was built from, and the application layer writes the
/// consequences back onto that citizen.
///
/// <para>
/// Derived statistics are snapshotted on entry rather than recomputed per step:
/// the values already come from the on-demand statistics service, and holding
/// them fixed for the encounter is what keeps a replay from a seed deterministic.
/// Equipment changes mid-encounter are not part of this slice.
/// </para>
/// </summary>
public sealed class CombatantState
{
    private readonly List<StatusEffect> _statuses = new();
    private readonly Dictionary<string, int> _cooldowns = new();
    private readonly List<InjuryKind> _injuries = new();

    public CombatantState(
        string id,
        string displayName,
        CombatSide side,
        CitizenId? citizenId,
        double maxHealth,
        double currentHealth,
        double physicalChannelPower,
        double elementalChannelPower,
        double physicalMitigation,
        double elementalMitigation,
        double generalDamageReduction,
        double criticalChance,
        double attackSpeed,
        ElementalAffinity elementalAffinity,
        PhysicalExpression physicalExpression,
        WeaponFamily? weaponFamily,
        IReadOnlyList<TechniqueDefinition> techniques,
        double fatigue = 0,
        CombatSpatialState? spatial = null,
        CombatStature stature = CombatStature.Standard,
        double physicalEvasion = 0,
        double elementalEvasion = 0,
        double controlPower = 0,
        double controlResistance = 0)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id is required.", nameof(id));
        if (maxHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maxHealth));
        ArgumentNullException.ThrowIfNull(techniques);
        if (!Enum.IsDefined(stature)) throw new ArgumentOutOfRangeException(nameof(stature));

        Id = id;
        DisplayName = displayName;
        Side = side;
        CitizenId = citizenId;
        MaxHealth = maxHealth;
        CurrentHealth = Math.Clamp(currentHealth, 0, maxHealth);
        PhysicalChannelPower = physicalChannelPower;
        ElementalChannelPower = elementalChannelPower;
        PhysicalMitigation = physicalMitigation;
        ElementalMitigation = elementalMitigation;
        GeneralDamageReduction = generalDamageReduction;
        CriticalChance = criticalChance;
        AttackSpeed = attackSpeed;
        ElementalAffinity = elementalAffinity;
        PhysicalExpression = physicalExpression;
        WeaponFamily = weaponFamily;
        Techniques = techniques;
        Fatigue = fatigue;
        Spatial = spatial ?? new CombatSpatialState(
            facing: side == CombatSide.Party ? CombatFacing.Right : CombatFacing.Left);
        Stature = stature;
        PhysicalEvasion = Math.Clamp(physicalEvasion, 0, 1);
        ElementalEvasion = Math.Clamp(elementalEvasion, 0, 1);
        ControlPower = Math.Max(0, controlPower);
        ControlResistance = Math.Max(0, controlResistance);
    }

    public string Id { get; }
    public string DisplayName { get; }
    public CombatSide Side { get; }

    /// <summary>Set for party members; null for provisional enemies.</summary>
    public CitizenId? CitizenId { get; }

    public double MaxHealth { get; }
    public double CurrentHealth { get; private set; }
    public double PhysicalChannelPower { get; }
    public double ElementalChannelPower { get; }
    public double PhysicalMitigation { get; }
    public double ElementalMitigation { get; }
    public double GeneralDamageReduction { get; }
    public double CriticalChance { get; }
    public double AttackSpeed { get; }

    /// <summary>
    /// Chance to avoid a fully physical technique outright, in <c>[0, 1]</c>.
    /// A hybrid technique is evaded on the blend of the two, weighted by its
    /// own physical share — the same way its mitigation is blended, so a mixed
    /// blow can never be resolved against whichever of the pair is lower.
    /// </summary>
    public double PhysicalEvasion { get; }

    /// <summary>Chance to avoid a fully elemental technique outright.</summary>
    public double ElementalEvasion { get; }

    /// <summary>
    /// This combatant's multiplier for making a physical expression stick.
    /// Zero means "not measured", and an attacker with zero is not rolled
    /// against a target that has resistance — see
    /// <see cref="ControlResistance"/>.
    /// </summary>
    public double ControlPower { get; }

    /// <summary>
    /// This combatant's multiplier for shrugging a physical expression off.
    /// Zero disables the roll entirely and every expression lands, which is
    /// what a combatant assembled without control statistics gets.
    /// </summary>
    public double ControlResistance { get; }
    public ElementalAffinity ElementalAffinity { get; }
    public PhysicalExpression PhysicalExpression { get; }
    public WeaponFamily? WeaponFamily { get; }
    public IReadOnlyList<TechniqueDefinition> Techniques { get; }
    public double Fatigue { get; private set; }
    public CombatSpatialState Spatial { get; }
    public CombatStature Stature { get; }

    public IReadOnlyList<StatusEffect> Statuses => _statuses;
    public IReadOnlyList<InjuryKind> Injuries => _injuries;

    /// <summary>
    /// A defeated party member is incapacitated, not deleted: it still exists in
    /// the domain and still appears in the expedition result.
    /// </summary>
    public bool IsDefeated => CurrentHealth <= 0;

    public bool IsAlive => !IsDefeated;
    public double HealthRatio => MaxHealth <= 0 ? 0 : CurrentHealth / MaxHealth;

    /// <summary>Techniques that can actually be activated this encounter.</summary>
    public IEnumerable<TechniqueDefinition> ActiveTechniques
    {
        get
        {
            foreach (TechniqueDefinition technique in Techniques)
            {
                if (technique.Kind == TechniqueKind.Active) yield return technique;
            }
        }
    }

    public double ApplyResult(double amount)
    {
        if (amount <= 0) return 0;
        double before = CurrentHealth;
        CurrentHealth = Math.Max(0, CurrentHealth - amount);
        return before - CurrentHealth;
    }

    public void Heal(double amount)
    {
        if (amount <= 0) return;
        CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
    }

    public void AddFatigue(double amount)
    {
        if (amount <= 0) return;
        Fatigue += amount;
    }

    public void AddInjury(InjuryKind injury)
    {
        if (!_injuries.Contains(injury)) _injuries.Add(injury);
    }

    public void ReplaceStatuses(IReadOnlyList<StatusEffect> statuses)
    {
        ArgumentNullException.ThrowIfNull(statuses);
        _statuses.Clear();
        _statuses.AddRange(statuses);
    }

    public int CooldownFor(string techniqueId) =>
        _cooldowns.TryGetValue(techniqueId, out int remaining) ? remaining : 0;

    public bool IsReady(string techniqueId) => CooldownFor(techniqueId) <= 0;

    public void StartCooldown(TechniqueDefinition technique)
    {
        ArgumentNullException.ThrowIfNull(technique);
        // Activation time is paid up front together with the cooldown so a slow
        // technique genuinely costs rhythm rather than being free to spam.
        _cooldowns[technique.Id] = technique.Cooldown + technique.ActivationTime;
    }

    public void TickCooldowns()
    {
        foreach (string id in new List<string>(_cooldowns.Keys))
        {
            int remaining = _cooldowns[id] - 1;
            if (remaining <= 0) _cooldowns.Remove(id);
            else _cooldowns[id] = remaining;
        }
    }
}
