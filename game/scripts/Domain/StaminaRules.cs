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
/// <c>docs/PRODUCT_DIRECTION.md §5</c>.
/// </para>
///
/// <para>
/// Tick-order note (intentional): within a tick, citizens eat first,
/// then pay the cost. A worker at <c>CurrentStamina == 0</c> who does
/// have food available eats one unit, climbs to 1, then the cost
/// knocks them back to 0. They contribute nothing this tick — a
/// degenerate "ate for nothing" case, but the intuitive reading
/// (citizens eat breakfast before working) is preserved.
/// </para>
/// </summary>
public static class StaminaRules
{
    /// <summary>Default maximum stamina for any citizen.</summary>
    public const int MaxStamina = 100;

    /// <summary>Default cost per worker per tick for any unknown building kind.</summary>
    public const int DefaultCostPerWorkerPerTick = 2;

    /// <summary>Stamina recovered per unit of food consumed.</summary>
    public const int RegenPerFoodUnit = 1;

    /// <summary>Food units consumed per citizen per tick when regen happens.</summary>
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
    public const int WellFedBuffDuration = 100;

    private static readonly Dictionary<BuildingKind, int> CostByKind = new()
    {
        { BuildingKind.Quarry, 2 },
        { BuildingKind.Farm, 2 },
        // Future kinds default to DefaultCostPerWorkerPerTick.
    };

    /// <summary>
    /// Cost per worker per tick for the given building kind.
    /// </summary>
    public static int CostPerWorkerPerTick(BuildingKind kind) =>
        CostByKind.TryGetValue(kind, out var v) ? v : DefaultCostPerWorkerPerTick;

    /// <summary>
    /// Per-citizen cost. The signature takes the citizen so future
    /// extensions can vary cost by competency (e.g. reduce cost when
    /// the citizen has high mining experience). Today the citizen is
    /// ignored.
    /// </summary>
    public static int CostForWorker(Citizen citizen, BuildingKind kind) =>
        CostPerWorkerPerTick(kind);

    /// <summary>
    /// Stamina recovered when <paramref name="foodRequested"/> food
    /// units are consumed by <paramref name="consumer"/>. Today a
    /// 1:1 ratio; future bodies can scale by food source metadata or
    /// citizen hunger state.
    /// </summary>
    public static int RegenFromFood(int foodRequested, Citizen consumer) =>
        foodRequested * RegenPerFoodUnit;
}
