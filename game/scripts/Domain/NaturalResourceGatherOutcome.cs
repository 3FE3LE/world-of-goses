namespace WorldofGoses.Domain;

public enum NaturalResourceGatherOutcome
{
    Available = 0,
    Gathered = 1,
    HeroUnavailable = 2,
    NodeUnavailable = 3,
    StorageFull = 4,
    MissingRequiredTool = 5,
}
