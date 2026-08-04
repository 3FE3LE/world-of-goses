namespace WorldofGoses.Domain;

public readonly record struct ResourceOpportunityId(int Value)
{
    public override string ToString() => Value.ToString();
}
