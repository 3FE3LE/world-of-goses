namespace WorldofGoses.Domain;

/// <summary>
/// Body-affecting identity choice the player picks during onboarding.
/// The visual registry resolves a <see cref="CharacterBodyVariant"/>
/// from a <see cref="GenderId"/>; the simulation treats it as
/// identity metadata only.
/// </summary>
public enum GenderId
{
    Feminine = 0,
    Masculine = 1,
}