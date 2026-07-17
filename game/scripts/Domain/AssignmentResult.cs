namespace WorldofGoses.Domain;

/// <summary>
/// Result returned by <see cref="Building"/> and <see cref="CityWorld"/>
/// when attempting to change worker assignments.
/// </summary>
public readonly record struct AssignmentResult(
    AssignmentOutcome Outcome,
    CitizenId CitizenId,
    BuildingId BuildingId)
{
    public bool IsSuccess => Outcome == AssignmentOutcome.Success;

    public static AssignmentResult Ok(CitizenId citizen, BuildingId building) =>
        new(AssignmentOutcome.Success, citizen, building);

    public static AssignmentResult Fail(AssignmentOutcome outcome, CitizenId citizen, BuildingId building) =>
        new(outcome, citizen, building);
}
