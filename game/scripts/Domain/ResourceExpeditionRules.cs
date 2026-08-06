using System;

namespace WorldofGoses.Domain;

public static class ResourceExpeditionRules
{
    public static ResourceExpeditionDefinition Definition(ResourceOpportunityKind kind) => kind switch
    {
        ResourceOpportunityKind.NearbyFoodForage => new ResourceExpeditionDefinition(
            kind,
            DurationTicks: 120,
            SupplyResource: ResourceType.Branches,
            SupplyAmount: 1,
            RewardResource: ResourceType.Food,
            SetbackReturn: 3,
            PartialReturn: 5,
            FullReturn: 7,
            DisplayName: "Nearby Food Forage"),
        ResourceOpportunityKind.FallenWoodSearch => new ResourceExpeditionDefinition(
            kind,
            DurationTicks: 180,
            SupplyResource: ResourceType.Food,
            SupplyAmount: 1,
            RewardResource: ResourceType.Wood,
            SetbackReturn: 4,
            PartialReturn: 6,
            FullReturn: 8,
            DisplayName: "Fallen Wood Search"),
        ResourceOpportunityKind.SpiritTrailSearch => new ResourceExpeditionDefinition(
            kind,
            DurationTicks: 180,
            SupplyResource: ResourceType.Food,
            SupplyAmount: 1,
            RewardResource: ResourceType.Wood,
            SetbackReturn: 4,
            PartialReturn: 6,
            FullReturn: 8,
            DisplayName: "Spirit Trail Search"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
