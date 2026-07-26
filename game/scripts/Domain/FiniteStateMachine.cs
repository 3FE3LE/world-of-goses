using System;
using System.Collections.Generic;

#nullable enable

namespace WorldofGoses.Domain;

/// <summary>
/// Generic, in-house finite state machine (S-1.5). Built instead of
/// vendoring a third-party Godot addon (e.g. <c>godot-finite-state-machine</c>)
/// so the project does not pull unreviewed third-party code with engine
/// access into the tree for what is, mechanically, a validated-transition
/// table plus a current-state field.
///
/// <para>
/// Transitions are validated against an injected predicate rather than a
/// hard-coded switch, so the same class serves any domain enum (see
/// <see cref="CitizenBehaviorRules"/> for the citizen behavior catalog).
/// An invalid transition is rejected (returns <c>false</c>) rather than
/// throwing: callers that haven't catalogued every real code path yet
/// (see <see cref="Citizen"/>'s partial wiring) degrade to "behavior not
/// updated" instead of crashing the simulation.
/// </para>
/// </summary>
public sealed class FiniteStateMachine<TState> where TState : struct, Enum
{
    private readonly Func<TState, TState, bool> _isAllowed;

    public TState Current { get; private set; }

    /// <summary>Raised after a successful transition with (from, to, trigger).</summary>
    public event Action<TState, TState, string>? Transitioned;

    public FiniteStateMachine(TState initial, Func<TState, TState, bool> isAllowed)
    {
        ArgumentNullException.ThrowIfNull(isAllowed);
        Current = initial;
        _isAllowed = isAllowed;
    }

    /// <summary>
    /// Attempts to move to <paramref name="to"/>. A transition to the
    /// current state is always a no-op success. Returns false, leaving
    /// <see cref="Current"/> unchanged, when the predicate rejects the
    /// transition.
    /// </summary>
    public bool TryTransition(TState to, string trigger)
    {
        if (EqualityComparer<TState>.Default.Equals(Current, to)) return true;
        if (!_isAllowed(Current, to)) return false;

        TState from = Current;
        Current = to;
        Transitioned?.Invoke(from, to, trigger);
        return true;
    }
}
