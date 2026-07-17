namespace WorldofGoses.Domain;

/// <summary>
/// Outcome of a worker assignment or removal attempt. Pure data so
/// the presentation layer can format messages without re-querying
/// the domain.
/// </summary>
public enum AssignmentOutcome
{
    Success = 0,
    BuildingNotFound = 1,
    CitizenNotFound = 2,
    AlreadyAssigned = 3,
    NotAssigned = 4,
    AtCapacity = 5,
    CitizenUnavailable = 6,
}
