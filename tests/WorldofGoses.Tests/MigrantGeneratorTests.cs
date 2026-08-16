using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Migrants used to be the same handful of people. Every axis was
/// <c>seed % catalogueCount</c> over the citizen id, the first migrant of any
/// city has citizen id 2, and the three aptitudes were three consecutive
/// catalogue entries — so two different cities recruited the same person, and
/// no citizen ever held an unusual combination.
/// </summary>
public sealed class MigrantGeneratorTests
{
    /// <summary>
    /// The one that started this: two cities must not receive the same first
    /// migrant. Their founders differ, so their arrivals must.
    /// </summary>
    [Fact]
    public void TwoCitiesDoNotReceiveTheSameFirstMigrant()
    {
        const int firstMigrantId = 2;
        var identities = new HashSet<string>();

        foreach (LineageId lineage in ProfileCatalog.Lineages.Select(entry => entry.Id))
        {
            int citySeed = MigrantGenerator.CitySeed(FounderOf(lineage));
            CitizenProfile profile =
                MigrantGenerator.Profile(citySeed, arrivalTick: 0, firstMigrantId);
            identities.Add(Identity(
                profile,
                MigrantGenerator.Name(citySeed, arrivalTick: 0, firstMigrantId)));
        }

        Assert.Equal(ProfileCatalog.Lineages.Count, identities.Count);
    }

    [Fact]
    public void TheSameCityDoesNotRepeatItselfAcrossArrivals()
    {
        int citySeed = MigrantGenerator.CitySeed(FounderOf(LineageId.Ardhen));
        var identities = new HashSet<string>();

        // Twenty arrivals spread over a few in-game days.
        for (int index = 0; index < 20; index++)
        {
            int tick = 400 + index * 137;
            int citizenId = 2 + index;
            CitizenProfile profile = MigrantGenerator.Profile(citySeed, tick, citizenId);
            identities.Add(Identity(
                profile,
                MigrantGenerator.Name(citySeed, tick, citizenId)));
        }

        Assert.True(identities.Count >= 18, $"Only {identities.Count} distinct of 20.");
    }

    /// <summary>
    /// Aptitudes were picked as three consecutive catalogue entries, so a
    /// profile could only ever hold one of ten adjacent triples out of the
    /// hundred and twenty combinations that exist.
    /// </summary>
    [Fact]
    public void AptitudesAreCombinationsRatherThanConsecutiveRuns()
    {
        int citySeed = MigrantGenerator.CitySeed(FounderOf(LineageId.Kovari));
        var triples = new HashSet<string>();
        int nonConsecutive = 0;

        for (int index = 0; index < 40; index++)
        {
            CitizenProfile profile =
                MigrantGenerator.Profile(citySeed, 100 + index * 53, 2 + index);
            Assert.Equal(3, profile.Aptitudes.Count);
            Assert.Distinct(profile.Aptitudes);
            triples.Add(string.Join(",", profile.Aptitudes.Select(id => id.Value).OrderBy(v => v)));

            List<int> positions = profile.Aptitudes
                .Select(id => IndexOfAptitude(id))
                .OrderBy(position => position)
                .ToList();
            if (positions[1] != positions[0] + 1 || positions[2] != positions[1] + 1)
            {
                nonConsecutive++;
            }
        }

        Assert.True(triples.Count > 10, $"Only {triples.Count} distinct aptitude triples.");
        Assert.True(nonConsecutive > 30, $"Only {nonConsecutive} of 40 were non-consecutive.");
    }

    /// <summary>
    /// Determinism survives the change: the same inputs must rebuild the same
    /// person, or a save could not regenerate its own pending prospect.
    /// </summary>
    [Fact]
    public void GenerationIsReproducibleFromItsInputs()
    {
        int citySeed = MigrantGenerator.CitySeed(FounderOf(LineageId.Theryn));

        CitizenProfile first = MigrantGenerator.Profile(citySeed, 900, 5);
        CitizenProfile second = MigrantGenerator.Profile(citySeed, 900, 5);

        Assert.Equal(Identity(first, ""), Identity(second, ""));
        Assert.Equal(
            MigrantGenerator.Name(citySeed, 900, 5),
            MigrantGenerator.Name(citySeed, 900, 5));
        Assert.Equal(
            MigrantGenerator.SeededExperience(first, citySeed, 900, 5),
            MigrantGenerator.SeededExperience(second, citySeed, 900, 5));
    }

    /// <summary>
    /// The arrival tick is part of a migrant's identity, which is why the save
    /// had to start storing it.
    /// </summary>
    [Fact]
    public void ArrivalTickChangesWhoArrives()
    {
        int citySeed = MigrantGenerator.CitySeed(FounderOf(LineageId.Orveth));

        Assert.NotEqual(
            Identity(MigrantGenerator.Profile(citySeed, 100, 2), ""),
            Identity(MigrantGenerator.Profile(citySeed, 101, 2), ""));
    }

    [Fact]
    public void ArrivalsCarryTheProfessionsTheyPractisedElsewhere()
    {
        int citySeed = MigrantGenerator.CitySeed(FounderOf(LineageId.Eirune));
        int withHistory = 0;
        var practised = new HashSet<CompetencyId>();

        for (int index = 0; index < 30; index++)
        {
            int tick = 200 + index * 91;
            int citizenId = 2 + index;
            CitizenProfile profile = MigrantGenerator.Profile(citySeed, tick, citizenId);
            IReadOnlyList<(CompetencyId Competency, int Experience)> seeded =
                MigrantGenerator.SeededExperience(profile, citySeed, tick, citizenId);

            Assert.True(seeded.Count <= MigrantGenerator.MaximumSeededCompetencies);
            Assert.Distinct(seeded.Select(entry => entry.Competency));
            if (seeded.Count > 0) withHistory++;
            foreach ((CompetencyId competency, int experience) in seeded)
            {
                Assert.True(experience > 0);
                practised.Add(competency);
                Assert.InRange(
                    CityCompetency.LevelFor(
                        experience,
                        AptitudeLearning.LearningFactor(profile, competency)),
                    1,
                    MigrantGenerator.MaximumSeededLevel);
            }
        }

        Assert.True(withHistory >= 20, $"Only {withHistory} of 30 arrived with a trade.");
        Assert.True(practised.Count >= 5, $"Only {practised.Count} distinct trades appeared.");
    }

    /// <summary>
    /// Negative verification: strip the city out of the seed and the first
    /// migrant becomes the same person everywhere, which is the bug.
    /// </summary>
    [Fact]
    public void WithoutTheCityInTheSeed_EveryCityGetsTheSamePerson()
    {
        var identities = new HashSet<string>();
        foreach (LineageId _ in ProfileCatalog.Lineages.Select(entry => entry.Id))
        {
            identities.Add(Identity(
                MigrantGenerator.Profile(citySeed: 0, arrivalTick: 0, citizenId: 2),
                MigrantGenerator.Name(citySeed: 0, arrivalTick: 0, citizenId: 2)));
        }

        Assert.Single(identities);
    }

    private static int IndexOfAptitude(AptitudeId aptitude)
    {
        for (int index = 0; index < ProfileCatalog.Aptitudes.Count; index++)
        {
            if (ProfileCatalog.Aptitudes[index].Id.Equals(aptitude)) return index;
        }
        return -1;
    }

    private static string Identity(CitizenProfile profile, string name) =>
        string.Join(
            "|",
            name,
            profile.Lineage.Value,
            profile.Gender,
            profile.ElementalAffinity.Value,
            profile.CubeProfile.Body,
            profile.CubeProfile.Bond,
            profile.CubeProfile.Stability,
            profile.CubeProfile.Impulse,
            profile.CubeProfile.Domain,
            profile.CubeProfile.Reach,
            string.Join(",", profile.Aptitudes.Select(id => id.Value)));

    private static Citizen FounderOf(LineageId lineage)
    {
        CitizenProfile profile = CitizenProfile.CreateFounder(
            new FounderOnboardingResult(
                lineage,
                ElementalAffinity.Earth,
                CubeScoring.ComputeCubeVertex(lineage),
                FounderNarrativeMemory.Empty),
            GenderId.Feminine);
        return new Citizen(new CitizenId(1), $"Founder of {lineage.Value}", 1, profile);
    }
}
