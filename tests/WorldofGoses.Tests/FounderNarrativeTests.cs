using System;
using System.Collections.Generic;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class FounderNarrativeTests
{
    [Fact]
    public void Catalog_HasTwelveStableQuestionsWithUniqueChoiceIds()
    {
        Assert.Equal(12, FounderNarrativeCatalog.Questions.Count);
        var questionIds = new HashSet<string>();
        foreach (FounderNarrativeQuestion question in FounderNarrativeCatalog.Questions)
        {
            Assert.True(questionIds.Add(question.Id));
            Assert.InRange(question.Choices.Count, 4, 6);
            var choiceIds = new HashSet<string>();
            foreach (FounderNarrativeChoice choice in question.Choices)
            {
                Assert.True(choiceIds.Add(choice.Id));
                Assert.NotEmpty(choice.Contributions);
            }
        }
    }

    [Theory]
    [InlineData(false, "ardhen")]
    [InlineData(true, "vaelun")]
    public void LegacyLineageScoring_GoldenInputsAndTieBreakRemainUnchanged(bool lastChoices, string expected)
    {
        FounderOnboardingResult result = FounderNarrativeScorer.Calculate(
            lastChoices ? CompleteWithLastChoices() : CompleteWithFirstChoices());

        Assert.Equal(expected, result.Lineage.Value);
    }

    [Theory]
    [InlineData("earth", ElementalAffinity.Earth)]
    [InlineData("water", ElementalAffinity.Water)]
    [InlineData("fire", ElementalAffinity.Fire)]
    [InlineData("air", ElementalAffinity.Air)]
    [InlineData("aether", ElementalAffinity.Aether)]
    [InlineData("none", ElementalAffinity.Silence)]
    public void ElementScoring_GoldenWorldAnswersRemainCanonical(
        string choiceId,
        ElementalAffinity expected)
    {
        FounderNarrativeSession session = CompleteWithFirstChoices();
        session.Answer("world", choiceId);

        Assert.Equal(expected, FounderNarrativeScorer.Calculate(session).ElementalAffinity);
    }

    [Fact]
    public void ElementalAffinity_RemainsIndependentWhenLineageAnswersChange()
    {
        FounderNarrativeSession first = CompleteWithFirstChoices();
        FounderNarrativeSession last = CompleteWithLastChoices();
        first.Answer("world", "fire");
        last.Answer("world", "fire");

        FounderOnboardingResult firstResult = FounderNarrativeScorer.Calculate(first);
        FounderOnboardingResult lastResult = FounderNarrativeScorer.Calculate(last);

        Assert.NotEqual(firstResult.Lineage, lastResult.Lineage);
        Assert.Equal(ElementalAffinity.Fire, firstResult.ElementalAffinity);
        Assert.Equal(firstResult.ElementalAffinity, lastResult.ElementalAffinity);
    }

    [Fact]
    public void Session_ReplacingAnswerRecalculatesEverythingFromStableIds()
    {
        FounderNarrativeSession session = CompleteWithFirstChoices();
        FounderOnboardingResult before = FounderNarrativeScorer.Calculate(session);

        session.Answer("world", "fire");
        session.Answer("threshold", "mobility");
        FounderOnboardingResult changed = FounderNarrativeScorer.Calculate(session);

        FounderNarrativeSession rebuilt = CompleteWithFirstChoices();
        rebuilt.Answer("world", "fire");
        rebuilt.Answer("threshold", "mobility");
        FounderOnboardingResult recalculated = FounderNarrativeScorer.Calculate(rebuilt);

        Assert.NotEqual(before.ElementalAffinity, changed.ElementalAffinity);
        Assert.NotEqual(before.CubeProfile, changed.CubeProfile);
        Assert.Equal(recalculated, changed);
        Assert.Equal(12, changed.NarrativeMemory.AnswerIds.Count);
    }

    [Fact]
    public void ShadowCube_NeverChangesLegacyLineageSelection()
    {
        foreach (FounderNarrativeSession session in new[]
        {
            CompleteWithFirstChoices(),
            CompleteWithLastChoices(),
        })
        {
            FounderOnboardingResult result = FounderNarrativeScorer.Calculate(session);
            Assert.Equal(result.Lineage, CubeScoring.ComputeNearestVertex(result.CubeProfile));
        }
    }

    [Fact]
    public void NarrativeMemory_PreservesStableQuestionAndAnswerIds()
    {
        FounderOnboardingResult result = FounderNarrativeScorer.Calculate(CompleteWithFirstChoices());

        Assert.Contains("word:find", result.NarrativeMemory.AnswerIds);
        Assert.Contains("detail:name", result.NarrativeMemory.AnswerIds);
        Assert.Equal("find", result.NarrativeMemory.BelievedFinalWordId);
        Assert.Equal("name", result.NarrativeMemory.PreservedDetailId);
        Assert.Equal(12, result.NarrativeMemory.EchoIds.Count);
    }

    [Fact]
    public void FounderProfile_StoresOnlyCanonicalOnboardingOutputForNewFounder()
    {
        FounderOnboardingResult result = FounderNarrativeScorer.Calculate(CompleteWithFirstChoices());
        CitizenProfile profile = CitizenProfile.CreateFounder(result, GenderId.Masculine);

        Assert.Equal(result, profile.FounderOnboardingResult);
        Assert.Empty(profile.Aptitudes);
#pragma warning disable CS0618 // Intentional compatibility assertion for DEC-0013 fields.
        Assert.Empty(profile.ProfessionalAffinities);
        Assert.Empty(profile.WeaponPreferences);
        Assert.Empty(profile.PersonalityTraits);
        Assert.Equal(string.Empty, profile.CombatStyle.Value);
        Assert.Equal(string.Empty, profile.PoliticalOrientation.Value);
        Assert.Equal(string.Empty, profile.SpiritualPosture.Value);
#pragma warning restore CS0618
    }

    [Fact]
    public void BodyPresentation_DoesNotChangeCanonicalOnboardingResult()
    {
        FounderOnboardingResult result = FounderNarrativeScorer.Calculate(CompleteWithFirstChoices());

        CitizenProfile feminine = CitizenProfile.CreateFounder(result, GenderId.Feminine);
        CitizenProfile masculine = CitizenProfile.CreateFounder(result, GenderId.Masculine);

        Assert.Equal(result, feminine.FounderOnboardingResult);
        Assert.Equal(result, masculine.FounderOnboardingResult);
        Assert.NotEqual(feminine.Gender, masculine.Gender);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("  Founder  ")]
    [InlineData("12345678901234567890123456789012")]
    public void FounderName_AcceptsValidInput(string name) =>
        Assert.True(AstralOnboardingView.IsFounderNameValid(name));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n")]
    [InlineData("123456789012345678901234567890123")]
    public void FounderName_RejectsInvalidInput(string name) =>
        Assert.False(AstralOnboardingView.IsFounderNameValid(name));

    [Fact]
    public void NarrativeFounderCreation_PersistsCanonicalResultOnSingleCitizen()
    {
        FounderOnboardingResult onboarding =
            FounderNarrativeScorer.Calculate(CompleteWithFirstChoices());
        CitizenProfile profile = CitizenProfile.CreateFounder(onboarding, GenderId.Feminine);
        var world = new CityWorld();
        HeroCreationRequest request =
            new("Aster", profile, profile.Gender, onboarding);

        HeroCreationResult creation = world.TryCreateHero(request);
        Assert.True(creation.IsSuccess);
        Assert.Equal(onboarding, creation.OnboardingResult);
        Assert.Equal(HeroCreationOutcome.AlreadyExists, world.TryCreateHero(request).Outcome);
        Assert.Single(world.Citizens);
        Assert.Equal(onboarding, world.Hero!.Profile.FounderOnboardingResult);
        HeroProfileSnapshot snapshot = Assert.IsType<HeroProfileSnapshot>(HeroProfileSnapshot.From(world));
        Assert.Equal(onboarding.CubeProfile, snapshot.CubeProfile);
        Assert.Equal("Earth", snapshot.ElementalAffinity);

        CityWorld restored = CityWorld.FromSave(
            WorldPersistence.DeserializeFromJson(
                WorldPersistence.SerializeToJson(WorldPersistence.Capture(world))));
        Assert.Equal(onboarding, restored.Hero!.Profile.FounderOnboardingResult);
    }

    internal static FounderNarrativeSession CompleteWithFirstChoices() =>
        Complete(question => question.Choices[0].Id);

    internal static FounderNarrativeSession CompleteWithLastChoices() =>
        Complete(question => question.Choices[^1].Id);

    private static FounderNarrativeSession Complete(
        Func<FounderNarrativeQuestion, string> choose)
    {
        var session = new FounderNarrativeSession();
        foreach (FounderNarrativeQuestion question in FounderNarrativeCatalog.Questions)
        {
            session.Answer(question.Id, choose(question));
        }
        return session;
    }
}
