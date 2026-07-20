namespace WorldofGoses.Domain;

/// <summary>Result of <see cref="CityWorld.TryAuthorizeBasicShelter"/>.</summary>
public readonly record struct ConstructionAuthorizationResult(
    ConstructionAuthorizationOutcome Outcome,
    BuildingId? ProjectId = null)
{
    public bool IsSuccess => Outcome == ConstructionAuthorizationOutcome.Success;

    public static ConstructionAuthorizationResult Success(BuildingId projectId) =>
        new(ConstructionAuthorizationOutcome.Success, projectId);

    public static ConstructionAuthorizationResult Fail(ConstructionAuthorizationOutcome outcome) =>
        new(outcome);
}
