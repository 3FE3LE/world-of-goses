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
    public CitizenCommitment Commitment { get; private set; } = CitizenCommitment.None;
    public CitizenWorkOrder? WorkOrder { get; private set; }
    public BuildingId? CurrentAssignment => WorkOrder?.TargetId;
    public CitizenVitalStatus VitalStatus { get; private set; }
    public CitizenWound Wound { get; private set; }
    public bool IsWounded => Wound is not null;
    public int ResumeWorkNotBeforeTick { get; private set; }
    public Availability Availability => IsAvailable
        ? Availability.Available
        : Availability.Assigned;
    public CitizenAvailabilityReason AvailabilityReason => Commitment.Kind switch
    {
        CitizenCommitmentKind.None => IsWounded
            ? CitizenAvailabilityReason.Wounded
            : CitizenAvailabilityReason.Available,
        CitizenCommitmentKind.BuildingWork => CitizenAvailabilityReason.AssignedToBuilding,
        CitizenCommitmentKind.Construction => CitizenAvailabilityReason.AssignedToConstruction,
        CitizenCommitmentKind.Expedition => CitizenAvailabilityReason.OnExpedition,
        CitizenCommitmentKind.Recovery => CitizenAvailabilityReason.Recovering,
        _ => throw new InvalidOperationException($"Unknown commitment kind {Commitment.Kind}."),
    };
    public bool IsAvailable => Commitment.IsAvailable && !IsWounded;

    /// <summary>
    /// Where the citizen physically is right now. Updated by
    /// <see cref="CityWorld"/> at day/night transitions; the UI
    /// reads this to decide which building shows the worker slot.
    /// </summary>
    public CitizenLocation CurrentLocation { get; private set; } = CitizenLocation.AtHome;
    public int? TransitStartedAtTick { get; private set; }
    public bool IsReturningHome { get; private set; }

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

    /// <summary>
    /// Stamina currently usable after durable health limits. Healing the
    /// wound raises this cap; ordinary stamina recovery never removes it.
    /// </summary>
    public int EffectiveMaxStamina => Wound is null
        ? MaxStamina
        : WoundRules.EffectiveStaminaCap(MaxStamina, Wound.Severity);

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
    public bool CanJoinExpedition => IsHero
        && !IsWounded
        && Commitment.Kind is not (CitizenCommitmentKind.Expedition
            or CitizenCommitmentKind.Recovery)
        && VitalStatus == CitizenVitalStatus.Stable;

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
    internal bool TryCommitToBuilding(BuildingId buildingId) =>
        TrySetWorkOrder(CitizenCommitmentKind.BuildingWork, buildingId);

    internal bool TryCommitToConstruction(BuildingId projectId) =>
        TrySetWorkOrder(CitizenCommitmentKind.Construction, projectId);

    /// <summary>
    /// Detaches the citizen from any current workplace assignment.
    /// </summary>
    internal bool ReleaseCommitment(CitizenCommitmentKind expectedKind, int expectedEntityId)
    {
        bool activeMatches = Commitment.Kind == expectedKind
            && Commitment.EntityId == expectedEntityId;
        bool standingOrderMatches = WorkOrder is { } order
            && order.Kind == expectedKind
            && order.TargetId.Value == expectedEntityId;
        if (!activeMatches && !standingOrderMatches)
        {
            return false;
        }

        if (activeMatches) Commitment = CitizenCommitment.None;
        WorkOrder = null;
        return true;
    }

    internal void RestoreCommitment(
        CitizenCommitment commitment,
        CitizenWorkOrder? workOrder = null,
        CitizenVitalStatus vitalStatus = CitizenVitalStatus.Stable,
        int resumeWorkNotBeforeTick = 0)
    {
        Commitment = commitment;
        WorkOrder = workOrder ?? CitizenWorkOrder.FromCommitment(commitment);
        VitalStatus = vitalStatus;
        ResumeWorkNotBeforeTick = Math.Max(0, resumeWorkNotBeforeTick);
    }

    private bool TrySetWorkOrder(CitizenCommitmentKind kind, BuildingId targetId)
    {
        var commitment = new CitizenCommitment(kind, targetId.Value);
        if (!TrySetCommitment(commitment)) return false;
        WorkOrder = new CitizenWorkOrder(kind, targetId);
        return true;
    }

    private bool TrySetCommitment(CitizenCommitment commitment)
    {
        if (!Commitment.IsAvailable || IsWounded) return false;
        Commitment = commitment;
        return true;
    }

    internal void SustainWound(WoundSeverity severity, WorldEventId originatingEventId)
    {
        if (Wound is null)
        {
            Wound = new CitizenWound(
                severity,
                originatingEventId,
                WoundRules.RecoveryTicksFor(severity));
        }
        else
        {
            Wound.WorsenTo(severity);
        }
        CurrentStamina = Math.Min(CurrentStamina, EffectiveMaxStamina);
    }

    internal void RestoreWound(CitizenWound wound)
    {
        Wound = wound;
        CurrentStamina = Math.Min(CurrentStamina, EffectiveMaxStamina);
    }

    internal bool BeginWoundRecovery(BuildingId shelterId, int currentTick)
    {
        if (Wound is null
            || Commitment.Kind is CitizenCommitmentKind.Expedition
                or CitizenCommitmentKind.Recovery)
        {
            return false;
        }
        Commitment = new CitizenCommitment(CitizenCommitmentKind.Recovery, shelterId.Value);
        ResumeWorkNotBeforeTick = currentTick;
        if (CurrentLocation != CitizenLocation.AtHome)
        {
            BeginTravelHome(currentTick);
        }
        _behaviorFsm.TryTransition(CitizenBehaviorState.Resting, "Wound treatment began");
        return true;
    }

    internal bool AdvanceWoundRecoveryTick()
        => AdvanceWoundRecoveryTicks(1);

    internal bool AdvanceWoundRecoveryTicks(int tickCount)
    {
        if (Wound is null
            || Commitment.Kind != CitizenCommitmentKind.Recovery
            || CurrentLocation != CitizenLocation.AtHome
            || !Wound.AdvanceRecoveryTicks(tickCount))
        {
            return false;
        }

        Wound = null;
        Commitment = WorkOrder is { } workOrder
            ? new CitizenCommitment(workOrder.Kind, workOrder.TargetId.Value)
            : CitizenCommitment.None;
        return true;
    }

    internal bool BeginVitalRecovery(int currentTick)
    {
        if (VitalStatus != CitizenVitalStatus.Stable) return false;
        if (Commitment.Kind is CitizenCommitmentKind.Expedition or CitizenCommitmentKind.Recovery)
        {
            return false;
        }
        VitalStatus = CitizenVitalStatus.Recovering;
        BeginTravelHome(currentTick);
        _behaviorFsm.TryTransition(CitizenBehaviorState.Resting, "Vital recovery interrupted work");
        return true;
    }

    internal void MarkFoodBlocked() => VitalStatus = CitizenVitalStatus.BlockedNoFood;

    internal void MarkFoodReceived()
    {
        if (VitalStatus == CitizenVitalStatus.BlockedNoFood)
        {
            VitalStatus = CitizenVitalStatus.Recovering;
        }
    }

    internal bool CompleteVitalRecovery()
    {
        if (VitalStatus == CitizenVitalStatus.Stable
            || !CitizenNeedsRules.CanResume(this))
        {
            return false;
        }
        VitalStatus = CitizenVitalStatus.Stable;
        return true;
    }

    /// <summary>
    /// Sets the citizen's physical location. Called by
    /// <see cref="CityWorld"/> during mobilisation at day/night
    /// transitions. Internal because only the world owns this.
    /// </summary>
    internal void SetLocation(CitizenLocation location)
    {
        CurrentLocation = location;
        if (location != CitizenLocation.InTransit)
        {
            TransitStartedAtTick = null;
            IsReturningHome = false;
        }
        if (location == CitizenLocation.AtWork)
        {
            _behaviorFsm.TryTransition(CitizenBehaviorState.Working, "Mobilised to work");
        }
        else if (location == CitizenLocation.InTransit)
        {
            _behaviorFsm.TryTransition(CitizenBehaviorState.Travelling, "Travelling to assignment");
        }
        else
        {
            _behaviorFsm.TryTransition(
                CurrentAssignment.HasValue ? CitizenBehaviorState.Resting : CitizenBehaviorState.Idle,
                "Mobilised to home");
        }
    }

    internal void BeginTravelToAssignment(int currentTick)
    {
        IsReturningHome = false;
        TransitStartedAtTick = currentTick;
        SetLocation(CitizenLocation.InTransit);
        IsReturningHome = false;
        TransitStartedAtTick = currentTick;
    }

    internal void BeginTravelHome(int currentTick)
    {
        IsReturningHome = true;
        TransitStartedAtTick = currentTick;
        SetLocation(CitizenLocation.InTransit);
        IsReturningHome = true;
        TransitStartedAtTick = currentTick;
    }

    internal bool AbstractTravelHasCompleted(int currentTick) =>
        CurrentLocation == CitizenLocation.InTransit
        && TransitStartedAtTick is int startedAt
        && currentTick - startedAt >= CityEconomyRules.AbstractTravelTicks;

    /// <summary>
    /// Drives the two expedition-dispatch transitions (S-1.5 follow-up)
    /// back to back: <see cref="CityWorld.StartExpedition"/> only ever
    /// calls this on the hero, and only after confirming the hero is
    /// unassigned — per <see cref="SetLocation"/>'s own logic that always
    /// means <see cref="CitizenBehaviorState.Idle"/>, never <c>Resting</c>
    /// or <c>Injured</c> (both require an assignment), so both hops are
    /// always documented and never silently rejected. No travel-time delay
    /// is modelled yet, so <c>Travelling</c> is visited but not lingered
    /// in — see <c>TO_DO.md</c> S-1.5 for why that's an honest gap, not a bug.
    /// </summary>
    internal bool DispatchOnExpedition(ExpeditionId expeditionId)
    {
        if (IsWounded
            || Commitment.Kind is CitizenCommitmentKind.Expedition or CitizenCommitmentKind.Recovery)
        {
            return false;
        }
        Commitment = new CitizenCommitment(CitizenCommitmentKind.Expedition, expeditionId.Value);
        CurrentLocation = CitizenLocation.InTransit;
        _behaviorFsm.TryTransition(CitizenBehaviorState.Travelling, "Hero dispatched on expedition");
        _behaviorFsm.TryTransition(CitizenBehaviorState.OnExpedition, "Expedition reaches Active state");
        return true;
    }

    /// <summary>
    /// Returns the citizen to <see cref="CitizenBehaviorState.Idle"/> when
    /// their expedition ends — whether by natural completion, failure, or
    /// cancellation; the catalog documents all three under the same
    /// "returns or is cancelled" trigger.
    /// </summary>
    internal bool ReturnFromExpedition(ExpeditionId expeditionId, int currentTick = 0)
    {
        if (Commitment.Kind != CitizenCommitmentKind.Expedition
            || Commitment.EntityId != expeditionId.Value)
        {
            return false;
        }
        Commitment = WorkOrder is { } workOrder
            ? new CitizenCommitment(workOrder.Kind, workOrder.TargetId.Value)
            : CitizenCommitment.None;
        CurrentLocation = CitizenLocation.AtHome;
        VitalStatus = CitizenVitalStatus.Recovering;
        ResumeWorkNotBeforeTick = checked(
            (currentTick / GameClock.TicksPerInGameDay + 1)
            * GameClock.TicksPerInGameDay);
        _behaviorFsm.TryTransition(
            WorkOrder.HasValue ? CitizenBehaviorState.Resting : CitizenBehaviorState.Idle,
            "Expedition returns or is cancelled");
        return true;
    }

    /// <summary>
    /// Reverts a dispatch authorization before simulation advances. Unlike a
    /// physical return, this does not apply travel recovery because the team
    /// never crossed an expedition tick.
    /// </summary>
    internal bool CancelExpeditionDispatch(ExpeditionId expeditionId)
    {
        if (Commitment.Kind != CitizenCommitmentKind.Expedition
            || Commitment.EntityId != expeditionId.Value)
        {
            return false;
        }
        Commitment = WorkOrder is { } workOrder
            ? new CitizenCommitment(workOrder.Kind, workOrder.TargetId.Value)
            : CitizenCommitment.None;
        CurrentLocation = CitizenLocation.AtHome;
        _behaviorFsm.TryTransition(
            WorkOrder.HasValue ? CitizenBehaviorState.Resting : CitizenBehaviorState.Idle,
            "Expedition dispatch cancelled before departure");
        return true;
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
        CurrentStamina = Math.Min(EffectiveMaxStamina, CurrentStamina + amount);
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
