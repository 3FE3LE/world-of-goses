using System;
using System.Collections.Generic;

#nullable enable

namespace WorldofGoses.Domain;

/// <summary>
/// Behavioral state of a citizen. The current implementation is a flat
/// enum; the consumer (citizen mobilisation, day/night loop, expedition
/// system) reads <see cref="Citizen.CurrentLocation"/> and a separate
/// "is on expedition" flag. The switch-like flow is fine while the
/// number of states is small and the transitions are driven by domain
/// events.
///
/// <para>
/// This enum is the seam for a future FSM library migration
/// (godot-finite-state-machine, custom hierarchical state machine).
/// When the first NPC with autonomous behavior is added — NPC
/// merchants, citizens with needs, fauna — <see cref="CitizenBehaviorRules"/>
/// becomes the source of truth for transitions and the FSM library
/// takes over. Consumers do not need to change because they read
/// <see cref="Citizen.CurrentLocation"/> (UI-facing) and
/// <see cref="CitizenBehaviorState"/> (domain-facing), and the
/// transition logic is encapsulated.
/// </para>
/// </summary>
public enum CitizenBehaviorState
{
    /// <summary>Citizen is unassigned, idle, or at home with no task.</summary>
    Idle = 0,

    /// <summary>Citizen is at their assigned production building.</summary>
    Working = 1,

    /// <summary>Citizen is at the Home building (resting, sleeping).</summary>
    Resting = 2,

    /// <summary>Citizen is moving toward a destination (building, expedition origin).</summary>
    Travelling = 3,

    /// <summary>Citizen is on an active expedition outside the city.</summary>
    OnExpedition = 4,

    /// <summary>Citizen is at zero stamina and cannot produce.</summary>
    Injured = 5,
}

/// <summary>
/// Documented transitions between <see cref="CitizenBehaviorState"/> values.
/// Today these are evaluated as a switch in
/// <see cref="CitizenAssignmentService"/> and the day/night loop in
/// <see cref="CityWorld"/>. The static <see cref="CitizenBehaviorRules"/>
/// catalog is the seam: when an FSM library is integrated, the
/// transitions move into the library's inspector and this catalog is
/// regenerated from the same data.
/// </summary>
public readonly record struct CitizenBehaviorTransition(
    CitizenBehaviorState From,
    CitizenBehaviorState To,
    string Trigger);

/// <summary>
/// Centralized catalog of state transitions. Consumers do not call
/// this directly today; it documents the contract and will be the
/// data source for the future FSM library integration.
/// </summary>
public static class CitizenBehaviorRules
{
    public static readonly IReadOnlyList<CitizenBehaviorTransition> Transitions = new CitizenBehaviorTransition[]
    {
        new(CitizenBehaviorState.Idle,        CitizenBehaviorState.Working,     "Assigned to a production building"),
        new(CitizenBehaviorState.Working,     CitizenBehaviorState.Resting,     "Day ends / mobilisation"),
        new(CitizenBehaviorState.Working,     CitizenBehaviorState.Travelling,  "Cancelled assignment + new target"),
        new(CitizenBehaviorState.Resting,     CitizenBehaviorState.Working,     "Day begins / mobilisation"),
        new(CitizenBehaviorState.Idle,        CitizenBehaviorState.Travelling,  "Hero dispatched on expedition"),
        new(CitizenBehaviorState.Travelling,  CitizenBehaviorState.OnExpedition,"Expedition reaches Active state"),
        new(CitizenBehaviorState.OnExpedition,CitizenBehaviorState.Idle,        "Expedition returns or is cancelled"),
        new(CitizenBehaviorState.Working,     CitizenBehaviorState.Injured,     "Stamina depleted to zero"),
        new(CitizenBehaviorState.Injured,     CitizenBehaviorState.Resting,     "Stamina restored to threshold"),
    };

    /// <summary>
    /// Returns true if the documented catalog mentions a path from
    /// <paramref name="from"/> to <paramref name="to"/>. Useful for
    /// tests and for the future FSM library to validate that no
    /// consumer introduces a transition outside the contract.
    /// </summary>
    public static bool IsDocumentedTransition(CitizenBehaviorState from, CitizenBehaviorState to)
    {
        foreach (var transition in Transitions)
        {
            if (transition.From == from && transition.To == to) return true;
        }
        return false;
    }
}
