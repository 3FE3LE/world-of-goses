namespace WorldofGoses.Domain;

/// <summary>
/// Why a <see cref="ConstructionProject"/> did or did not advance on
/// the most recent tick. Mirrors <see cref="ProductionStopCause"/>
/// so the presentation layer can describe the same kinds of stop
/// without a parallel vocabulary.
/// </summary>
public enum ConstructionStopCause
{
    Authorized = 0,
    Paused = 1,
    NoWorkers = 2,
    WorkersExhausted = 3,
    Night = 4,
    Completed = 5,
    NoHero = 6,
    MissingMaterials = 7,
    WorkersInTransit = 8,
    AwaitingModule = 9,
}
