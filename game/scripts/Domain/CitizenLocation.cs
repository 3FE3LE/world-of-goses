namespace WorldofGoses.Domain;

/// <summary>
/// Where a citizen is physically right now. Distinct from
/// <see cref="Citizen.CurrentAssignment"/> (the worker's job):
/// assignment is static, location changes between day and night
/// when mobilisation runs.
///
/// <para>
/// AtWork — the citizen is at their assigned production building.
/// AtHome — the citizen is at the Home building (resting, sleeping,
/// or simply idle because they have no job).
/// </para>
/// </summary>
public enum CitizenLocation
{
    AtWork = 0,
    AtHome = 1,
}
