namespace WorldofGoses.Domain;

public readonly record struct NaturalResourceGatherResult(
    NaturalResourceGatherOutcome Outcome,
    int GatheredAmount = 0,
    ToolKind? RequiredTool = null)
{
    public bool IsSuccess => Outcome == NaturalResourceGatherOutcome.Gathered;
    public bool CanGather => Outcome == NaturalResourceGatherOutcome.Available;
}
