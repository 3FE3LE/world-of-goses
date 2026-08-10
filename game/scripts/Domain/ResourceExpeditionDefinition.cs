namespace WorldofGoses.Domain;

public readonly record struct ResourceExpeditionDefinition(
    ResourceOpportunityKind Kind,
    int DurationTicks,
    ExpeditionSupplyRequirement SupplyRequirement,
    ExpeditionReward Reward,
    int SetbackReturn,
    int PartialReturn,
    int FullReturn,
    string DisplayName)
{
    public ResourceType? SupplyResource => SupplyRequirement.Resource;
    public int SupplyAmount => SupplyRequirement.Amount;
    public ResourceType? RewardResource => Reward.Resource;
    public ExpeditionRewardKind RewardKind => Reward.Kind;

    public int ReturnFor(ExpeditionEncounterOutcome outcome) => outcome switch
    {
        ExpeditionEncounterOutcome.FullSuccess when Reward.IsMaterial => FullReturn,
        ExpeditionEncounterOutcome.PartialSuccess when Reward.IsMaterial => PartialReturn,
        _ => Reward.IsMaterial ? SetbackReturn : 0,
    };
}
