using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// The single person entity in the domain. A citizen may accumulate any
/// number of competencies, roles, recognitions, memberships, and ranks
/// over time. None of those concepts are modelled as subclasses: this
/// prototype composes them on the citizen so that a former miner can
/// later become a doctor, and so that hero status is a recognition rather
/// than a specialisation.
///
/// The citizen model exposes only what the current vertical slice
/// needs. It is intentionally extensible: future prototypes may add
/// health, professional history, aptitudes, relationships, and
/// expedition records without changing the citizen class's identity.
/// </summary>
public sealed class Citizen
{
    private readonly Dictionary<CompetencyId, CompetencyEntry> _competencies = new();
    private readonly List<Role> _roles = new();

    public CitizenId Id { get; }
    public string Name { get; }
    public int AppearanceSeed { get; }
    public AppearanceVariantId AppearanceVariant { get; private set; }
    public CitizenProfile Profile { get; }
    public CitizenOrigin Origin { get; }
    public BuildingId? CurrentAssignment { get; private set; }
    public Availability Availability => CurrentAssignment.HasValue
        ? Availability.Assigned
        : Availability.Available;

    /// <summary>
    /// Where the citizen physically is right now. Updated by
    /// <see cref="CityWorld"/> at day/night transitions; the UI
    /// reads this to decide which building shows the worker slot.
    /// </summary>
    public CitizenLocation CurrentLocation { get; private set; } = CitizenLocation.AtHome;

    /// <summary>
    /// Richer FSM-backed behavior state (S-1.5), validated against
    /// <see cref="CitizenBehaviorRules"/>. <see cref="SetLocation"/> and
    /// the stamina mutators drive it; expedition dispatch/return do not
    /// yet (see <c>TO_DO.md</c> S-1.5) — those transitions stay
    /// documented-only until that call site is wired.
    /// </summary>
    public CitizenBehaviorState Behavior => _behaviorFsm.Current;
    private readonly FiniteStateMachine<CitizenBehaviorState> _behaviorFsm =
        new(CitizenBehaviorState.Idle, CitizenBehaviorRules.IsDocumentedTransition);

    public BuildingId? LastVisitedResourceBuildingId { get; private set; }
    public int? LastVisitedResourceUnitId { get; private set; }
    public int? LastVisitedResourcePositionIndex { get; private set; }

    /// <summary>Maximum stamina for this citizen. Default <see cref="StaminaRules.MaxStamina"/>.</summary>
    public int MaxStamina { get; private set; }

    /// <summary>Current stamina. Clamped between 0 and <see cref="MaxStamina"/>.</summary>
    public int CurrentStamina { get; private set; }

    /// <summary>
    /// Remaining ticks of the WellFed buff. Reset to
    /// <see cref="StaminaRules.WellFedBuffDuration"/> when the
    /// citizen eats; decrements by 1 every world tick. While
    /// positive, stamina regen gains the
    /// <see cref="StaminaRules.WellFedRegenBonus"/>.
    /// </summary>
    public int WellFedRemainingTicks { get; private set; }

    public IReadOnlyDictionary<CompetencyId, CompetencyEntry> Competencies =>
        _competencies;
    public IReadOnlyList<Role> Roles => _roles;
    public bool IsHero => HasRole(RoleId.Hero);

    public Citizen(
        CitizenId id,
        string name,
        int appearanceSeed,
        CitizenProfile profile,
        int? initialStamina = null,
        int? maxStamina = null,
        int initialWellFedTicks = 0,
        AppearanceVariantId? appearanceVariant = null,
        CitizenOrigin origin = CitizenOrigin.Mortal)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Id = id;
        Name = name;
        AppearanceSeed = appearanceSeed;
        AppearanceVariant = appearanceVariant ?? AppearanceVariantId.Standard;
        Profile = profile;
        Origin = origin;
        MaxStamina = maxStamina ?? StaminaRules.MaxStamina;
        CurrentStamina = initialStamina ?? MaxStamina;
        WellFedRemainingTicks = initialWellFedTicks;
        if (MaxStamina <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxStamina),
                $"MaxStamina must be positive (got {MaxStamina}).");
        }
        if (CurrentStamina < 0 || CurrentStamina > MaxStamina)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialStamina),
                $"InitialStamina ({CurrentStamina}) must be in [0, {MaxStamina}].");
        }
        if (WellFedRemainingTicks < 0 || WellFedRemainingTicks > StaminaRules.WellFedBuffDuration)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialWellFedTicks),
                $"InitialWellFedTicks ({WellFedRemainingTicks}) must be in [0, {StaminaRules.WellFedBuffDuration}].");
        }
    }

    /// <summary>Cosmetic-only: replace the current appearance variant. Does not affect production, profession or stats.</summary>
    public void SetAppearanceVariant(AppearanceVariantId variant) => AppearanceVariant = variant;

    /// <summary>
    /// Attaches the citizen to a building as their primary workplace.
    /// Domain logic only; the presentation layer must not bypass this.
    /// </summary>
    internal void AssignTo(BuildingId buildingId) => CurrentAssignment = buildingId;

    /// <summary>
    /// Detaches the citizen from any current workplace assignment.
    /// </summary>
    internal void ClearAssignment() => CurrentAssignment = null;

    /// <summary>
    /// Sets the citizen's physical location. Called by
    /// <see cref="CityWorld"/> during mobilisation at day/night
    /// transitions. Internal because only the world owns this.
    /// </summary>
    internal void SetLocation(CitizenLocation location)
    {
        CurrentLocation = location;
        if (location == CitizenLocation.AtWork)
        {
            _behaviorFsm.TryTransition(CitizenBehaviorState.Working, "Mobilised to work");
        }
        else
        {
            _behaviorFsm.TryTransition(
                CurrentAssignment.HasValue ? CitizenBehaviorState.Resting : CitizenBehaviorState.Idle,
                "Mobilised to home");
        }
    }

    internal void VisitResource(BuildingId buildingId, int unitId, int positionIndex)
    {
        LastVisitedResourceBuildingId = buildingId;
        LastVisitedResourceUnitId = unitId;
        LastVisitedResourcePositionIndex = positionIndex;
    }

    /// <summary>
    /// Records or updates the citizen's accumulated experience in a
    /// competency. New competencies are added; existing ones are updated.
    /// </summary>
    public void AddExperience(CompetencyId competency, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (_competencies.TryGetValue(competency, out var existing))
        {
            _competencies[competency] = existing.WithExperience(existing.Experience + amount);
        }
        else
        {
            _competencies[competency] = new CompetencyEntry(competency, amount);
        }
    }

    /// <summary>
    /// Returns the citizen's experience in a competency, or zero if
    /// the citizen has no recorded experience in it.
    /// </summary>
    public int GetExperience(CompetencyId competency)
    {
        return _competencies.TryGetValue(competency, out var entry) ? entry.Experience : 0;
    }

    /// <summary>
    /// Attaches a role or recognition to the citizen. Re-granting an
    /// already-held role refreshes its granted tick.
    /// </summary>
    public void GrantRole(RoleId role, int atTick)
    {
        for (int i = 0; i < _roles.Count; i++)
        {
            if (_roles[i].Id.Value == role.Value)
            {
                _roles[i] = new Role(role, atTick);
                return;
            }
        }
        _roles.Add(new Role(role, atTick));
    }

    /// <summary>
    /// Removes a previously-attached role. Returns true if a role
    /// was removed; false if the citizen did not hold it.
    /// </summary>
    public bool RevokeRole(RoleId role)
    {
        for (int i = 0; i < _roles.Count; i++)
        {
            if (_roles[i].Id.Value == role.Value)
            {
                _roles.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    public bool HasRole(RoleId role)
    {
        for (int i = 0; i < _roles.Count; i++)
        {
            if (_roles[i].Id.Value == role.Value)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Reduces <see cref="CurrentStamina"/> by <paramref name="amount"/>,
    /// clamped at zero. Negative or zero <paramref name="amount"/> is a
    /// no-op.
    /// </summary>
    public void ConsumeStamina(int amount)
    {
        if (amount <= 0) return;
        CurrentStamina = Math.Max(0, CurrentStamina - amount);
        if (CurrentStamina == 0)
        {
            _behaviorFsm.TryTransition(CitizenBehaviorState.Injured, "Stamina depleted to zero");
        }
    }

    /// <summary>
    /// Increases <see cref="CurrentStamina"/> by <paramref name="amount"/>,
    /// clamped at <see cref="MaxStamina"/>. Negative or zero
    /// <paramref name="amount"/> is a no-op.
    /// </summary>
    public void RestoreStamina(int amount)
    {
        if (amount <= 0) return;
        CurrentStamina = Math.Min(MaxStamina, CurrentStamina + amount);
        if (CurrentStamina > 0 && Behavior == CitizenBehaviorState.Injured)
        {
            _behaviorFsm.TryTransition(CitizenBehaviorState.Resting, "Stamina restored to threshold");
        }
    }

    /// <summary>
    /// Resets the WellFed buff to <see cref="StaminaRules.WellFedBuffDuration"/>
    /// ticks. Called when the citizen eats a food unit.
    /// </summary>
    public void RefreshWellFedBuff()
    {
        WellFedRemainingTicks = StaminaRules.WellFedBuffDuration;
    }

    /// <summary>
    /// Advances the WellFed buff by one world tick. Decrements the
    /// remaining ticks to a floor of zero.
    /// </summary>
    public void AdvanceWellFedTick()
    {
        if (WellFedRemainingTicks > 0) WellFedRemainingTicks--;
    }

    internal void AdvanceWellFedTicks(int tickCount)
    {
        if (tickCount <= 0) return;
        WellFedRemainingTicks = Math.Max(0, WellFedRemainingTicks - tickCount);
    }

    /// <summary>
    /// Stamina regenerated for this citizen in a single world tick,
    /// given the current buff state. Encapsulates the base + buff
    /// formula so callers do not duplicate the arithmetic.
    /// </summary>
    public int RegenPerTick()
    {
        int regen = StaminaRules.BaseRegenPerTick;
        if (WellFedRemainingTicks > 0) regen += StaminaRules.WellFedRegenBonus;
        return regen;
    }
}
