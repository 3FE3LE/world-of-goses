namespace WorldofGoses.Domain;

/// <summary>Expected outcomes when creating the world's principal hero.</summary>
public enum HeroCreationOutcome
{
    Success = 0,
    AlreadyExists = 1,
    WorldNotEmpty = 2,
    InvalidName = 3,
    MissingProfile = 4,
    SaveFailed = 5,
}
