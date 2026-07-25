namespace WorldofGoses.Domain;

public readonly record struct ExpeditionId(int Value)
{
    public override string ToString() => $"exp-{Value:D3}";
}
