namespace WorldofGoses.Domain;

public readonly record struct ResourceExpeditionDefinition(
    ResourceOpportunityKind Kind,
    int DurationTicks,
    ResourceType SupplyResource,
    int SupplyAmount,
    ResourceType RewardResource,
    int SetbackReturn,
    int PartialReturn,
    int FullReturn,
    string DisplayName)
{
    public int ReturnFor(ExpeditionEncounterOutcome outcome) => outcome switch
    {
        ExpeditionEncounterOutcome.FullSuccess => FullReturn,
        ExpeditionEncounterOutcome.PartialSuccess => PartialReturn,
        _ => SetbackReturn,
    };
}
