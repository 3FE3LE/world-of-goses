#nullable enable
using System.Collections.Generic;
using WorldofGoses.Domain;

namespace WorldofGoses;

public sealed record ExpeditionPlanningSnapshot(
    bool ResourceSortiesUnlocked,
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
        return new ExpeditionPlanningSnapshot(unlocked, capacity, opportunities);
    }
}
