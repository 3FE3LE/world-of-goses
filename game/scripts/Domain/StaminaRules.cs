using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// Single seam for every tunable stamina / food parameter. All
/// callers go through these methods so future extensions (per-
/// competency cost, food quality, citizen-level Max) only need to
/// change the bodies here — never the callers.
///
/// <para>
/// The constants are provisional tuning values for the current
/// prototype. They are not product rules. See
/// <c>docs/engineering/design-review.md §5</c>.
/// </para>
///
/// <para>
/// Cycle-order note: on a production cycle that is also a scheduled meal,
/// citizens eat and recover before paying the labour cost. Ordinary clock
/// ticks perform neither action.
/// </para>
/// </summary>
public static class StaminaRules
{
    /// <summary>Default maximum stamina for any citizen.</summary>
    public const int MaxStamina = 100;

    /// <summary>Default cost per worker per completed production cycle.</summary>
    public const int DefaultCostPerWorkerPerCycle = 2;

    /// <summary>Stamina recovered per unit of food consumed.</summary>
    public const int RegenPerFoodUnit = 30;

    /// <summary>Food units consumed per citizen at a scheduled meal.</summary>
    public const int FoodConsumedPerRegen = 1;

    /// <summary>
    /// Stamina recovered every tick for every citizen, day and
    /// night, with or without food. This is the "sleep is not
    /// enough" floor: the food system modulates regen above it,
    /// not below.
    /// </summary>
    public const int BaseRegenPerTick = 1;

    /// <summary>
    /// Extra stamina recovered per tick while the citizen's
    /// <c>WellFedRemainingTicks</c> is positive. Set when they eat.
    /// </summary>
    public const int WellFedRegenBonus = 1;

    /// <summary>
    /// Duration of the WellFed buff in ticks after a citizen eats.
    /// At 1 Hz, 100 ticks = 100 real seconds. The buff decrements
    /// by 1 every world tick.
    /// </summary>
    public const int WellFedBuffDuration = CityEconomyRules.MealIntervalTicks;

    private static readonly Dictionary<BuildingKind, int> CostByKind = new()
    {
        { BuildingKind.Quarry, 2 },
        { BuildingKind.Farm, 2 },
        // Future kinds default to DefaultCostPerWorkerPerCycle.
    };

    /// <summary>
    /// Cost per worker per completed production cycle for the given building kind.
    /// </summary>
    public static int CostPerWorkerPerCycle(BuildingKind kind) =>
        CostByKind.TryGetValue(kind, out var v) ? v : DefaultCostPerWorkerPerCycle;

    /// <summary>
    /// Per-citizen cost. The signature takes the citizen so future
    /// extensions can vary cost by competency (e.g. reduce cost when
    /// the citizen has high mining experience). Today the citizen is
    /// ignored.
    /// </summary>
    public static int CostForWorker(Citizen citizen, BuildingKind kind) =>
        CostPerWorkerPerCycle(kind);

    /// <summary>
    /// Stamina recovered when <paramref name="foodRequested"/> food
    /// units are consumed by <paramref name="consumer"/>. Today a
    /// 1:1 ratio; future bodies can scale by food source metadata or
    /// citizen hunger state.
    /// </summary>
    public static int RegenFromFood(int foodRequested, Citizen consumer) =>
        foodRequested * RegenPerFoodUnit;
}
