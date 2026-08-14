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
    /// (<c>docs/systems/first-night.md</c> §11–12). The
    /// Spirit Trail is a narrative objective and therefore does not share the
    /// Campfire + Cache gate used by material sorties. This gate must remain
    /// false until the night concludes.
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
        ExpeditionSupplyRequirement SupplyRequirement,
        ExpeditionReward Reward,
        int MinimumReturn,
        int PartialReturn,
        int MaximumReturn,
        int CarryCapacity,
        string DisplayName,
        bool AccessUnlocked)
    {
        public ResourceType? SupplyResource => SupplyRequirement.Resource;
        public int SupplyAmount => SupplyRequirement.Amount;
        public ResourceType? RewardResource => Reward.Resource;
        public bool CanDispatch => State == ResourceOpportunityState.Available
            && AccessUnlocked
            && (!Reward.IsMaterial || CarryCapacity >= MinimumReturn);
    }

    public static ExpeditionPlanningSnapshot From(CityWorld world)
    {
        bool unlocked = world.HasFoundingSiteModule(FoundingSiteModule.Campfire)
            && world.HasFoundingSiteModule(FoundingSiteModule.Cache);
        bool spiritTrailUnlocked = world.Log.Events.Any(
            evt => evt.Kind == WorldEventKind.SpiritDeparted);
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
                definition.SupplyRequirement,
                definition.Reward,
                definition.SetbackReturn,
                definition.PartialReturn,
                definition.FullReturn,
                unlocked && definition.Reward.IsMaterial
                    ? System.Math.Min(definition.FullReturn, capacity)
                    : 0,
                definition.DisplayName,
                opportunity.Kind == ResourceOpportunityKind.SpiritTrailSearch
                    ? spiritTrailUnlocked
                    : unlocked));
        }
        return new ExpeditionPlanningSnapshot(unlocked, spiritTrailUnlocked, capacity, opportunities);
    }
}
