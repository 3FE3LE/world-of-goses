namespace WorldofGoses.Domain;

/// <summary>
/// Stable reason exposed by the domain when a citizen is or is not available.
/// Presentation can format this value without reconstructing commitment rules.
/// </summary>
public enum CitizenAvailabilityReason
{
    Available = 0,
    AssignedToBuilding = 1,
    AssignedToConstruction = 2,
    OnExpedition = 3,
    Recovering = 4,
}
