namespace WorldofGoses.Domain;

/// <summary>Possible outcomes of authorising the first worksite.</summary>
public enum ConstructionAuthorizationOutcome
{
    Success = 0,
    NoHero = 1,
    AlreadyAuthorized = 2,
    HomeAlreadyBuilt = 3,
    WorldNotEmpty = 4,
    HomeRequired = 5,
    MissingMaterials = 6,
    NoAvailableLot = 7,
    BuildingAlreadyBuilt = 8,
}
