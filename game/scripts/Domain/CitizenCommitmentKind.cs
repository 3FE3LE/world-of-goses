namespace WorldofGoses.Domain;

/// <summary>
/// The mutually-exclusive primary commitment that controls whether a citizen
/// can accept another city or expedition responsibility.
/// </summary>
public enum CitizenCommitmentKind
{
    None = 0,
    BuildingWork = 1,
    Construction = 2,
    Expedition = 3,
    Recovery = 4,
}
