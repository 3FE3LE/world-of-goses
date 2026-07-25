using System;
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
        var questionIds = new System.Collections.Generic.HashSet<string>();
        foreach (FounderNarrativeQuestion question in FounderNarrativeCatalog.Questions)
        {
            Assert.True(questionIds.Add(question.Id));
            Assert.InRange(question.Choices.Count, 4, 6);
            var choiceIds = new System.Collections.Generic.HashSet<string>();
            foreach (FounderNarrativeChoice choice in question.Choices)
            {
                Assert.True(choiceIds.Add(choice.Id));
                Assert.NotEmpty(choice.Contributions);
            }
        }
    }

    [Fact]
    public void Session_ReplacingAnswerRecalculatesFromStableIds()
    {
        FounderNarrativeSession session = CompleteWithFirstChoices();
        FounderNarrativeResult before = FounderNarrativeScorer.Calculate(session);

        session.Answer("world", "fire");
        FounderNarrativeResult after = FounderNarrativeScorer.Calculate(session);

        Assert.Equal(ElementalAffinityId.Earth, before.Element);
        Assert.Equal(ElementalAffinityId.Fire, after.Element);
        Assert.Equal(12, session.Answers.Count);
    }

    [Fact]
    public void Scoring_ProducesCompleteValidCitizenProfile()
    {
        FounderNarrativeResult result =
            FounderNarrativeScorer.Calculate(CompleteWithLastChoices());

        Assert.Equal(3, result.Aptitudes.Count);
        Assert.Equal(3, result.ProfessionalAffinities.Count);
        Assert.InRange(result.WeaponPreferences.Count, 1, 2);
        Assert.Equal(3, result.Traits.Count);
        Assert.Equal(12, result.Identity.PrologueFragments.Count);
        Assert.Equal(result.Lineage, result.Profile.Lineage);
        Assert.NotEmpty(result.Identity.RiskProfile);
        Assert.NotEmpty(result.Identity.LeadershipStyle);
    }

    [Fact]
    public void BodyPresentation_ChangesOnlyGender()
    {
        FounderNarrativeResult scored =
            FounderNarrativeScorer.Calculate(CompleteWithFirstChoices());
        FounderNarrativeResult masculine =
            FounderNarrativeScorer.WithGender(scored, GenderId.Masculine);

        Assert.Equal(GenderId.Masculine, masculine.Profile.Gender);
        Assert.Equal(scored.Lineage, masculine.Lineage);
        Assert.Equal(scored.Aptitudes, masculine.Aptitudes);
        Assert.Equal(scored.Traits, masculine.Traits);
        Assert.Equal(scored.Element, masculine.Element);
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
    public void NarrativeFounderCreation_CreatesExactlyOneCitizen()
    {
        FounderNarrativeResult result =
            FounderNarrativeScorer.WithGender(
                FounderNarrativeScorer.Calculate(CompleteWithFirstChoices()),
                GenderId.Feminine);
        var world = new CityWorld();
        HeroCreationRequest request =
            new("Aster", result.Profile, result.Profile.Gender);

        Assert.True(world.TryCreateHero(request).IsSuccess);
        Assert.Equal(HeroCreationOutcome.AlreadyExists, world.TryCreateHero(request).Outcome);
        Assert.Single(world.Citizens);
        Assert.True(world.Hero!.IsHero);
        Assert.Equal(CitizenOrigin.AstralFounder, world.Hero.Origin);

        CityWorld restored = CityWorld.FromSave(
            WorldPersistence.DeserializeFromJson(
                WorldPersistence.SerializeToJson(
                    WorldPersistence.Capture(world))));
        Assert.Equal(CitizenOrigin.AstralFounder, restored.Hero!.Origin);
    }

    private static FounderNarrativeSession CompleteWithFirstChoices() =>
        Complete(question => question.Choices[0].Id);

    private static FounderNarrativeSession CompleteWithLastChoices() =>
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
