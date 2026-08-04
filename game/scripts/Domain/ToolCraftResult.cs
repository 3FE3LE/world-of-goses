namespace WorldofGoses.Domain;

public readonly record struct ToolCraftResult(
    ToolCraftOutcome Outcome,
    ResourceType? MissingResource = null)
{
    public bool IsSuccess => Outcome == ToolCraftOutcome.Crafted;
}
