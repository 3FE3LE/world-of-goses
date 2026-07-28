namespace WorldofGoses.Domain;

/// <summary>
/// Result returned by <see cref="Building"/> and <see cref="CityWorld"/>
/// when attempting to change worker assignments.
/// </summary>
public readonly record struct AssignmentResult(
    AssignmentOutcome Outcome,
    CitizenId CitizenId,
    BuildingId BuildingId,
    CitizenAvailabilityReason? UnavailableReason = null)
{
    public bool IsSuccess => Outcome == AssignmentOutcome.Success;

    public static AssignmentResult Ok(CitizenId citizen, BuildingId building) =>
        new(AssignmentOutcome.Success, citizen, building);

    public static AssignmentResult Fail(
        AssignmentOutcome outcome,
        CitizenId citizen,
        BuildingId building,
        CitizenAvailabilityReason? unavailableReason = null) =>
        new(outcome, citizen, building, unavailableReason);

    /// <summary>Convenience factory for the project assignment path that uses <see cref="AssignmentOutcome.NotAssigned"/> as a non-failure placeholder.</summary>
    public static AssignmentResult Ok(AssignmentOutcome placeholder, CitizenId citizen, BuildingId building) =>
        Ok(citizen, building);
}
