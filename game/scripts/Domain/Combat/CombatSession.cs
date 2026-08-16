#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain.Combat;

public enum CombatSessionCommandKind
{
    SetAutoSkills,
    ActivateMemberSkill,
}

/// <summary>A persisted, replayable player command issued before a logical step.</summary>
public sealed record CombatSessionCommand(
    int BeforeStep,
    CombatSessionCommandKind Kind,
    int Value);

public sealed record CombatSkillState(
    bool Ready,
    int Remaining,
    int Duration,
    string? TechniqueId);

public sealed record CombatParticipantState(
    string Id,
    CitizenId? CitizenId,
    string DisplayName,
    double CurrentHealth,
    double MaxHealth,
    bool Defeated,
    double PositionX,
    double AttackRange,
    double BodyRadius,
    CombatFacing Facing,
    CombatSpatialActivity Activity,
    double LastDisplacement,
    CombatStature Stature,

    /// <summary>The force this combatant puts behind a blow.</summary>
    /// <remarks>
    /// Here for the same reason <see cref="AttackRange"/> and
    /// <see cref="BodyRadius"/> are: presentation needs the combatant's physical
    /// facts to draw them convincingly. Specifically, a hit reaction is sized by
    /// the attacker's Impulse against the target's <see cref="Stability"/> —
    /// the same ratio the domain uses for a real knockback — so that a shove
    /// looks like it came from the blow that caused it. Presentation composes
    /// that itself; the domain does not decide how a hit looks.
    /// </remarks>
    double Impulse = 0,

    /// <summary>How well this combatant resists being moved.</summary>
    double Stability = 0);

public sealed record CombatSessionSnapshot(
    bool Active,
    bool AutoSkillsEnabled,
    int Step,
    CombatOutcome Outcome,
    int EnemyCount,
    double BattlefieldMinimumX,
    double BattlefieldMaximumX,
    IReadOnlyList<CombatParticipantState> Party,
    IReadOnlyList<CombatParticipantState> Enemies,
    IReadOnlyList<CombatSkillState> MemberSkills,
    IReadOnlyList<CombatLogEntry> Log);

public sealed record CombatAdvanceResult(
    CombatOutcome Outcome,
    IReadOnlyList<CombatLogEntry> Events);

/// <summary>
/// View-independent incremental owner around one CombatEncounter. It records
/// commands against logical steps so persistence can rebuild the exact state by
/// replaying the same deterministic engine instead of serializing a second set
/// of combat rules.
/// </summary>
public sealed class CombatSession
{
    private readonly CombatEncounter _encounter;
    private readonly List<CombatSessionCommand> _commands = new();
    private readonly HashSet<int> _pendingManualSlots = new();

    public CombatSession(CombatEncounter encounter)
    {
        _encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
    }

    public bool AutoSkillsEnabled { get; private set; } = true;
    public int Step => _encounter.Step;
    public CombatOutcome Outcome => _encounter.Outcome;
    public bool IsActive => Outcome == CombatOutcome.InProgress;
    public IReadOnlyList<CombatSessionCommand> Commands => _commands;
    public IReadOnlyList<CombatLogEntry> Log => _encounter.Log;
    public IReadOnlyList<CombatantState> Party => _encounter.Party;
    public IReadOnlyList<CombatantState> Enemies => _encounter.Enemies;

    public void SetAutoSkillsEnabled(bool enabled)
    {
        if (AutoSkillsEnabled == enabled) return;
        AutoSkillsEnabled = enabled;
        _commands.Add(new CombatSessionCommand(
            Step,
            CombatSessionCommandKind.SetAutoSkills,
            enabled ? 1 : 0));
    }

    public bool TryActivateMemberSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Party.Count || !IsActive) return false;
        CombatantState member = Party[slotIndex];
        TechniqueDefinition? active = PrimaryActive(member);
        if (member.IsDefeated || active is null || !member.IsReady(active.Id)) return false;
        if (!_pendingManualSlots.Add(slotIndex)) return false;
        _commands.Add(new CombatSessionCommand(
            Step,
            CombatSessionCommandKind.ActivateMemberSkill,
            slotIndex));
        return true;
    }

    public CombatAdvanceResult Advance(int steps = 1)
    {
        if (steps < 0) throw new ArgumentOutOfRangeException(nameof(steps));
        int firstEvent = Log.Count;
        for (int index = 0; index < steps && IsActive; index++)
        {
            var manualActorIds = new HashSet<string>();
            var requestedSlots = new List<int>(_pendingManualSlots);
            foreach (int slot in _pendingManualSlots)
            {
                if (slot >= 0 && slot < Party.Count) manualActorIds.Add(Party[slot].Id);
            }
            _pendingManualSlots.Clear();
            int beforeEncounterEvents = Log.Count;
            _encounter.Advance(1, AutoSkillsEnabled, manualActorIds);
            if (IsActive)
            {
                foreach (int slot in requestedSlots)
                {
                    if (slot < 0 || slot >= Party.Count) continue;
                    string actorId = Party[slot].Id;
                    bool resolved = false;
                    for (int eventIndex = beforeEncounterEvents; eventIndex < Log.Count; eventIndex++)
                    {
                        CombatLogEntry entry = Log[eventIndex];
                        if (entry.Kind == CombatLogKind.TechniqueResolved
                            && entry.ActorId == actorId)
                        {
                            resolved = true;
                            break;
                        }
                    }
                    if (!resolved && Party[slot].IsAlive) _pendingManualSlots.Add(slot);
                }
            }
        }

        var events = new List<CombatLogEntry>();
        for (int index = firstEvent; index < Log.Count; index++) events.Add(Log[index]);
        return new CombatAdvanceResult(Outcome, events);
    }

    public CombatOutcome ResolveToEnd()
    {
        while (IsActive) Advance();
        return Outcome;
    }

    public CombatSessionSnapshot Snapshot()
    {
        var party = new List<CombatParticipantState>(Party.Count);
        var enemies = new List<CombatParticipantState>(Enemies.Count);
        var skills = new List<CombatSkillState>(Party.Count);
        foreach (CombatantState member in Party)
        {
            party.Add(Participant(member));
            TechniqueDefinition? active = PrimaryActive(member);
            int remaining = active is null ? 0 : member.CooldownFor(active.Id);
            int duration = active is null ? 0 : active.Cooldown + active.ActivationTime;
            skills.Add(new CombatSkillState(
                active is not null && member.IsAlive && remaining <= 0,
                remaining,
                duration,
                active?.Id));
        }
        foreach (CombatantState enemy in Enemies) enemies.Add(Participant(enemy));

        int livingEnemies = 0;
        foreach (CombatantState enemy in Enemies)
        {
            if (enemy.IsAlive) livingEnemies++;
        }
        return new CombatSessionSnapshot(
            IsActive,
            AutoSkillsEnabled,
            Step,
            Outcome,
            livingEnemies,
            _encounter.BattlefieldMinimumX,
            _encounter.BattlefieldMaximumX,
            party,
            enemies,
            skills,
            Log);
    }

    public static CombatSession Restore(
        CombatEncounter encounter,
        int stepsAdvanced,
        IReadOnlyList<CombatSessionCommand> commands)
        => Restore(new CombatSession(encounter), stepsAdvanced, commands);

    public static CombatSession Restore(
        CombatSession session,
        int stepsAdvanced,
        IReadOnlyList<CombatSessionCommand> commands)
    {
        if (stepsAdvanced < 0) throw new ArgumentOutOfRangeException(nameof(stepsAdvanced));
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(commands);
        int commandIndex = 0;
        while (session.Step < stepsAdvanced && session.IsActive)
        {
            while (commandIndex < commands.Count
                && commands[commandIndex].BeforeStep == session.Step)
            {
                session.Replay(commands[commandIndex]);
                commandIndex++;
            }
            session.Advance();
        }
        if (session.Step != stepsAdvanced)
        {
            throw new InvalidOperationException(
                "Persisted combat steps extend beyond the deterministic terminal outcome.");
        }
        while (commandIndex < commands.Count)
        {
            session.Replay(commands[commandIndex]);
            commandIndex++;
        }
        session._commands.Clear();
        session._commands.AddRange(commands);
        return session;
    }

    private void Replay(CombatSessionCommand command)
    {
        if (command.BeforeStep != Step)
        {
            throw new InvalidOperationException("Combat command history is not aligned to its logical step.");
        }
        switch (command.Kind)
        {
            case CombatSessionCommandKind.SetAutoSkills:
                AutoSkillsEnabled = command.Value != 0;
                break;
            case CombatSessionCommandKind.ActivateMemberSkill:
                _pendingManualSlots.Add(command.Value);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command));
        }
    }

    private static CombatParticipantState Participant(CombatantState combatant) => new(
        combatant.Id,
        combatant.CitizenId,
        combatant.DisplayName,
        combatant.CurrentHealth,
        combatant.MaxHealth,
        combatant.IsDefeated,
        combatant.Spatial.PositionX,
        combatant.Spatial.AttackRange,
        combatant.Spatial.BodyRadius,
        combatant.Spatial.Facing,
        combatant.Spatial.Activity,
        combatant.Spatial.LastDisplacement,
        combatant.Stature,
        combatant.Spatial.Impulse,
        combatant.Spatial.Stability);

    private static TechniqueDefinition? PrimaryActive(CombatantState combatant)
    {
        foreach (TechniqueDefinition technique in combatant.ActiveTechniques) return technique;
        return null;
    }
}
