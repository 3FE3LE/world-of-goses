#nullable enable
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;

namespace WorldofGoses;

public sealed record ExpeditionPlanningSnapshot(
    bool ResourceSortiesUnlocked,

    /// <summary>
    /// True when the fire spirit has departed
    /// (<c>WorldEventKind.SpiritDeparted</c> in the log) so the
    /// <see cref="ResourceOpportunityKind.SpiritTrailSearch"/>
    /// objective is meaningful — the trail cannot be read while the
    /// spirit is still present
    /// (<c>docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md</c> §11–12). The
    /// underlying gate is the same as the other resource sorties
    /// (Campfire + Cache), but the spirit gate is independent and
    /// must be false until the night concludes.
    /// </summary>
    bool SpiritTrailUnlocked,
    int AvailableReturnCapacity,
    IReadOnlyList<ExpeditionPlanningSnapshot.OpportunityItem> Opportunities)
{
    public sealed record OpportunityItem(
        ResourceOpportunityId Id,
        ResourceOpportunityKind Kind,
        ResourceOpportunityState State,
        int DurationTicks,
        ResourceType SupplyResource,
        int SupplyAmount,
        ResourceType RewardResource,
        int MinimumReturn,
        int PartialReturn,
        int MaximumReturn,
        int CarryCapacity,
        string DisplayName)
    {
        public bool CanDispatch => State == ResourceOpportunityState.Available
            && CarryCapacity >= MinimumReturn;
    }

    public static ExpeditionPlanningSnapshot From(CityWorld world)
    {
        bool unlocked = world.HasFoundingSiteModule(FoundingSiteModule.Campfire)
            && world.HasFoundingSiteModule(FoundingSiteModule.Cache);
        int capacity = world.AvailableFoundingStorageCapacity();
        var opportunities = new List<OpportunityItem>();
        foreach (ResourceOpportunity opportunity in world.ResourceOpportunities.Values)
        {
            ResourceExpeditionDefinition definition =
                ResourceExpeditionRules.Definition(opportunity.Kind);
            opportunities.Add(new OpportunityItem(
                opportunity.Id,
                opportunity.Kind,
                opportunity.State,
                definition.DurationTicks,
                definition.SupplyResource,
                definition.SupplyAmount,
                definition.RewardResource,
                definition.SetbackReturn,
                definition.PartialReturn,
                definition.FullReturn,
                unlocked ? System.Math.Min(definition.FullReturn, capacity) : 0,
                definition.DisplayName));
        }
        bool spiritTrailUnlocked = world.Log.Events.Any(
            evt => evt.Kind == WorldEventKind.SpiritDeparted);
        return new ExpeditionPlanningSnapshot(unlocked, spiritTrailUnlocked, capacity, opportunities);
    }
}
