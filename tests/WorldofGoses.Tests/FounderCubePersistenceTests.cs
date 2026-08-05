#pragma warning disable CS0618 // Explicit v29/v30 compatibility assertions for retained DTO fields.
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class FounderCubePersistenceTests
{
    [Fact]
    public void MigrateV28ToV29_GeneratesVertexFallbackAndPreservesAffinityAndLegacyFields()
    {
        WorldSave legacy = V28FounderSave();
        CitizenProfileSave oldProfile = Assert.Single(legacy.Citizens).Profile!;
        string[] oldTraits = oldProfile.PersonalityTraits.ToArray();
        string affinity = oldProfile.ElementalAffinity;

        WorldSave migrated = WorldPersistence.MigrateV28ToV29(legacy);
        CitizenProfileSave profile = Assert.Single(migrated.Citizens).Profile!;

        Assert.Equal(29, migrated.Version);
        Assert.Equal(affinity, profile.ElementalAffinity);
        Assert.Equal(oldTraits, profile.PersonalityTraits);
        Assert.NotNull(profile.CubeProfile);
        Assert.Equal(60, profile.CubeProfile!.Body);
        Assert.Equal(40, profile.CubeProfile.Bond);
        Assert.NotNull(profile.NarrativeMemory);
    }

    [Theory]
    [InlineData("")]
    [InlineData("none")]
    [InlineData("neutral")]
    public void MigrateV28ToV29_MapsMissingAffinityToSilence(string legacyAffinity)
    {
        WorldSave legacy = V28FounderSave();
        Assert.Single(legacy.Citizens).Profile!.ElementalAffinity = legacyAffinity;

        WorldSave migrated = WorldPersistence.MigrateV28ToV29(legacy);

        Assert.Equal("silence", Assert.Single(migrated.Citizens).Profile!.ElementalAffinity);
    }

    [Fact]
    public void V28FallbackThenV30Save_RoundTripsCanonicalFounderWithoutReplayingOnboarding()
    {
        WorldSave loadedV28 = WorldPersistence.DeserializeFromJson(
            WorldPersistence.SerializeToJson(V28FounderSave()));
        WorldSave migrated = WorldPersistence.MigrateToCurrent(loadedV28);
        CityWorld restoredV28 = CityWorld.FromSave(migrated);

        FounderOnboardingResult fallback = restoredV28.Hero!.Profile.FounderOnboardingResult!;
        Assert.Empty(fallback.NarrativeMemory.AnswerIds);
        Assert.Equal(CubeScoring.ComputeCubeVertex(fallback.Lineage), fallback.CubeProfile);

        WorldSave savedV30 = WorldPersistence.Capture(restoredV28);
        CityWorld restoredV30 = CityWorld.FromSave(
            WorldPersistence.DeserializeFromJson(
                WorldPersistence.SerializeToJson(savedV30)));

        Assert.Equal(fallback, restoredV30.Hero!.Profile.FounderOnboardingResult);
        Assert.Equal(WorldSave.CurrentVersion, savedV30.Version);
    }

    private static WorldSave V28FounderSave()
    {
        WorldSave save = WorldPersistence.Capture(TestHelpers.NewHeroWorld());
        save.Version = 28;
        CitizenProfileSave profile = Assert.Single(save.Citizens).Profile!;
        profile.CubeProfile = null;
        profile.NarrativeMemory = null;
        return save;
    }
}
