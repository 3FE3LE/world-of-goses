namespace WorldofGoses.Domain;

/// <summary>
/// Coarse availability state for a citizen with respect to a workplace
/// assignment. The domain keeps this minimal: future prototypes may extend
/// it (injured, traveling, on leave) without changing the citizen model.
/// </summary>
public enum Availability
{
    Available = 0,
    Assigned = 1,
}