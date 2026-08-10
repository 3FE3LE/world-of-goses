using System;

namespace WorldofGoses.Domain;

public static class ResourceExpeditionRules
{
    public static ResourceExpeditionDefinition Definition(ResourceOpportunityKind kind) => kind switch
    {
        ResourceOpportunityKind.NearbyFoodForage => new ResourceExpeditionDefinition(
            kind,
            DurationTicks: 120,
            SupplyRequirement: ExpeditionSupplyRequirement.Required(ResourceType.Branches, 1),
            Reward: ExpeditionReward.Supplies(ResourceType.Food, 7),
            SetbackReturn: 3,
            PartialReturn: 5,
            FullReturn: 7,
            DisplayName: "Nearby Food Forage"),
        ResourceOpportunityKind.FallenWoodSearch => new ResourceExpeditionDefinition(
            kind,
            DurationTicks: 180,
            SupplyRequirement: ExpeditionSupplyRequirement.Required(ResourceType.Food, 1),
            Reward: ExpeditionReward.Supplies(ResourceType.Wood, 8),
            SetbackReturn: 4,
            PartialReturn: 6,
            FullReturn: 8,
            DisplayName: "Fallen Wood Search"),
        ResourceOpportunityKind.SpiritTrailSearch => new ResourceExpeditionDefinition(
            kind,
            DurationTicks: ExpeditionTiming.SpiritTrailDurationTicks,
            SupplyRequirement: ExpeditionSupplyRequirement.None,
            Reward: ExpeditionReward.Discovery,
            SetbackReturn: 0,
            PartialReturn: 0,
            FullReturn: 0,
            DisplayName: "Spirit Trail Search"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
