#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// Catalog of the founding narrative. The catalog holds IDs and
/// scoring data only; the displayed text lives in the .po files
/// keyed by <see cref="Tr.Narrative"/>. The UI calls
/// <c>LocaleManager.Translate(id)</c> to resolve the actual string.
///
/// <para>
/// <see cref="FounderNarrativeQuestion.Title"/>,
/// <see cref="FounderNarrativeQuestion.Text"/>,
/// <see cref="FounderNarrativeChoice.Text"/>, and
/// <see cref="FounderNarrativeChoice.ImmediateConsequence"/> are
/// translation keys (e.g. <c>"narrative.hand.title"</c>), not
/// localized strings. The UI must resolve them.
/// </para>
/// </summary>
public static class FounderNarrativeCatalog
{
    public static IReadOnlyList<FounderNarrativeQuestion> Questions { get; } =
        Array.AsReadOnly(new[]
        {
            Q("hand", Tr.Narrative.HandTitle, Tr.Narrative.HandBody, 0f,
                O("hold", Tr.Narrative.HandOptionHoldLabel, Tr.Narrative.HandOptionHoldConsequence,
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Strength), 2),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Protective), 2),
                    C(FounderScoreAxis.Attachment, "Bond", 2),
                    C(FounderScoreAxis.RiskProfile, "Bold", 1)),
                O("observe", Tr.Narrative.HandOptionObserveLabel, Tr.Narrative.HandOptionObserveConsequence,
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Observation), 3),
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Orientation), 2),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Reflective), 1),
                    C(FounderScoreAxis.Lineage, LineageId.Vaelun.Value, 1)),
                O("stabilise", Tr.Narrative.HandOptionStabiliseLabel, Tr.Narrative.HandOptionStabiliseConsequence,
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.ManualPrecision), 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Disciplined), 2),
                    C(FounderScoreAxis.Control, "Structured", 2),
                    C(FounderScoreAxis.Lineage, LineageId.Kovari.Value, 1)),
                O("call", Tr.Narrative.HandOptionCallLabel, Tr.Narrative.HandOptionCallConsequence,
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Memory), 2),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Tenacious), 2),
                    C(FounderScoreAxis.LeadershipStyle, "Connective", 2),
                    C(FounderScoreAxis.Lineage, LineageId.Theryn.Value, 1))),

            Q("word", Tr.Narrative.WordTitle, Tr.Narrative.WordBody, 0f,
                O("find", Tr.Narrative.WordOptionFindLabel, Tr.Narrative.WordOptionFindConsequence,
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Tenacious), 2),
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Orientation), 2),
                    C(FounderScoreAxis.Lineage, LineageId.Vaelun.Value, 1)),
                O("return", Tr.Narrative.WordOptionReturnLabel, Tr.Narrative.WordOptionReturnConsequence,
                    C(FounderScoreAxis.Attachment, "Return", 2),
                    C(FounderScoreAxis.IdentityContinuity, "Continuity", 2),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Protective), 1)),
                O("remember", Tr.Narrative.WordOptionRememberLabel, Tr.Narrative.WordOptionRememberConsequence,
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Memory), 3),
                    C(FounderScoreAxis.Contemplation, "Reflective", 2),
                    C(FounderScoreAxis.Lineage, LineageId.Caelith.Value, 1)),
                O("continue", Tr.Narrative.WordOptionContinueLabel, Tr.Narrative.WordOptionContinueConsequence,
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Adaptability), 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Independent), 2),
                    C(FounderScoreAxis.Autonomy, "Autonomous", 2))),

            Q("detail", Tr.Narrative.DetailTitle, Tr.Narrative.DetailBody, 0f,
                O("name", Tr.Narrative.DetailOptionNameLabel, Tr.Narrative.DetailOptionNameConsequence,
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Memory), 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Reflective), 2),
                    C(FounderScoreAxis.Lineage, LineageId.Myrven.Value, 1)),
                O("hands", Tr.Narrative.DetailOptionHandsLabel, Tr.Narrative.DetailOptionHandsConsequence,
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.ManualPrecision), 3),
                    C(FounderScoreAxis.Profession, nameof(ProfessionFamilyId.EngineeringManufacturing), 3),
                    C(FounderScoreAxis.Lineage, LineageId.Kovari.Value, 1)),
                O("object", Tr.Narrative.DetailOptionObjectLabel, Tr.Narrative.DetailOptionObjectConsequence,
                    C(FounderScoreAxis.Profession, nameof(ProfessionFamilyId.Logistics), 2),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Cooperative), 2),
                    C(FounderScoreAxis.Lineage, LineageId.Orveth.Value, 1)),
                O("journey", Tr.Narrative.DetailOptionJourneyLabel, Tr.Narrative.DetailOptionJourneyConsequence,
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Adaptability), 3),
                    C(FounderScoreAxis.Profession, nameof(ProfessionFamilyId.ExplorationSurvival), 3),
                    C(FounderScoreAxis.Lineage, LineageId.Vaelun.Value, 1))),

            Q("old-form", Tr.Narrative.OldFormTitle, Tr.Narrative.OldFormBody, 0f,
                O("exact", Tr.Narrative.OldFormOptionExactLabel, Tr.Narrative.OldFormOptionExactConsequence,
                    C(FounderScoreAxis.IdentityContinuity, "Exact", 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Tenacious), 2),
                    C(FounderScoreAxis.SpiritualPosture, nameof(SpiritualPostureId.Devout), 1)),
                O("sensation", Tr.Narrative.OldFormOptionSensationLabel, Tr.Narrative.OldFormOptionSensationConsequence,
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Empathy), 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Compassionate), 2),
                    C(FounderScoreAxis.Lineage, LineageId.Eirune.Value, 1)),
                O("separate", Tr.Narrative.OldFormOptionSeparateLabel, Tr.Narrative.OldFormOptionSeparateConsequence,
                    C(FounderScoreAxis.Autonomy, "Independent", 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Independent), 3),
                    C(FounderScoreAxis.SpiritualPosture, nameof(SpiritualPostureId.Secular), 2)),
                O("release", Tr.Narrative.OldFormOptionReleaseLabel, Tr.Narrative.OldFormOptionReleaseConsequence,
                    C(FounderScoreAxis.TransformationAcceptance, "Open", 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Restless), 3),
                    C(FounderScoreAxis.SpiritualPosture, nameof(SpiritualPostureId.Syncretic), 2))),

            Q("time", Tr.Narrative.TimeTitle, Tr.Narrative.TimeBody, 0f,
                O("cause", Tr.Narrative.TimeOptionCauseLabel, Tr.Narrative.TimeOptionCauseConsequence,
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Observation), 2),
                    C(FounderScoreAxis.Profession, nameof(ProfessionFamilyId.ResearchEducation), 3),
                    C(FounderScoreAxis.Lineage, LineageId.Caelith.Value, 1)),
                O("feeling", Tr.Narrative.TimeOptionFeelingLabel, Tr.Narrative.TimeOptionFeelingConsequence,
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Empathy), 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Compassionate), 2),
                    C(FounderScoreAxis.Lineage, LineageId.Theryn.Value, 1)),
                O("promises", Tr.Narrative.TimeOptionPromisesLabel, Tr.Narrative.TimeOptionPromisesConsequence,
                    C(FounderScoreAxis.Profession, nameof(ProfessionFamilyId.CommerceAdministration), 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Disciplined), 2),
                    C(FounderScoreAxis.Lineage, LineageId.Orveth.Value, 1)),
                O("places", Tr.Narrative.TimeOptionPlacesLabel, Tr.Narrative.TimeOptionPlacesConsequence,
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Orientation), 3),
                    C(FounderScoreAxis.Profession, nameof(ProfessionFamilyId.ExplorationSurvival), 2),
                    C(FounderScoreAxis.Lineage, LineageId.Vaelun.Value, 1))),

            Q("mortality", Tr.Narrative.MortalityTitle, Tr.Narrative.MortalityBody, 0f,
                O("weight", Tr.Narrative.MortalityOptionWeightLabel, Tr.Narrative.MortalityOptionWeightConsequence,
                    C(FounderScoreAxis.MortalityResponse, "Responsible", 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Pragmatic), 2),
                    C(FounderScoreAxis.PoliticalOrientation, nameof(PoliticalOrientationId.Communitarian), 1)),
                O("understand", Tr.Narrative.MortalityOptionUnderstandLabel, Tr.Narrative.MortalityOptionUnderstandConsequence,
                    C(FounderScoreAxis.Contemplation, "Analytical", 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Curious), 3),
                    C(FounderScoreAxis.SpiritualPosture, nameof(SpiritualPostureId.Agnostic), 2)),
                O("others", Tr.Narrative.MortalityOptionOthersLabel, Tr.Narrative.MortalityOptionOthersConsequence,
                    C(FounderScoreAxis.Attachment, "Collective", 3),
                    C(FounderScoreAxis.Profession, nameof(ProfessionFamilyId.SocialRelations), 2),
                    C(FounderScoreAxis.SpiritualPosture, nameof(SpiritualPostureId.Devout), 1)),
                O("new", Tr.Narrative.MortalityOptionNewLabel, Tr.Narrative.MortalityOptionNewConsequence,
                    C(FounderScoreAxis.TransformationAcceptance, "Reinvention", 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Bold), 2),
                    C(FounderScoreAxis.SpiritualPosture, nameof(SpiritualPostureId.Skeptical), 1))),

            Q("world", Tr.Narrative.WorldTitle, Tr.Narrative.WorldBody, 0f,
                O("earth", Tr.Narrative.WorldOptionEarthLabel, Tr.Narrative.WorldOptionEarthConsequence,
                    C(FounderScoreAxis.Element, nameof(ElementalAffinityId.Earth), 3),
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Strength), 1)),
                O("water", Tr.Narrative.WorldOptionWaterLabel, Tr.Narrative.WorldOptionWaterConsequence,
                    C(FounderScoreAxis.Element, nameof(ElementalAffinityId.Water), 3),
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Adaptability), 1)),
                O("fire", Tr.Narrative.WorldOptionFireLabel, Tr.Narrative.WorldOptionFireConsequence,
                    C(FounderScoreAxis.Element, nameof(ElementalAffinityId.Fire), 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Ambitious), 1)),
                O("air", Tr.Narrative.WorldOptionAirLabel, Tr.Narrative.WorldOptionAirConsequence,
                    C(FounderScoreAxis.Element, nameof(ElementalAffinityId.Air), 3),
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Orientation), 1)),
                O("aether", Tr.Narrative.WorldOptionAetherLabel, Tr.Narrative.WorldOptionAetherConsequence,
                    C(FounderScoreAxis.Element, nameof(ElementalAffinityId.Aether), 3),
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Memory), 1)),
                O("none", Tr.Narrative.WorldOptionNoneLabel, Tr.Narrative.WorldOptionNoneConsequence,
                    C(FounderScoreAxis.Element, nameof(ElementalAffinityId.Silence), 3),
                    C(FounderScoreAxis.Autonomy, "Unbound", 1))),

            Q("perception", Tr.Narrative.PerceptionTitle, Tr.Narrative.PerceptionBody, 0f,
                O("ardhen", Tr.Narrative.PerceptionOptionArdhenLabel, Tr.Narrative.PerceptionOptionArdhenConsequence,
                    C(FounderScoreAxis.Lineage, LineageId.Ardhen.Value, 2),
                    C(FounderScoreAxis.Profession, nameof(ProfessionFamilyId.Extraction), 3),
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Strength), 2)),
                O("eirune", Tr.Narrative.PerceptionOptionEiruneLabel, Tr.Narrative.PerceptionOptionEiruneConsequence,
                    C(FounderScoreAxis.Lineage, LineageId.Eirune.Value, 2),
                    C(FounderScoreAxis.Profession, nameof(ProfessionFamilyId.MedicineCare), 3),
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Empathy), 2)),
                O("kovari", Tr.Narrative.PerceptionOptionKovariLabel, Tr.Narrative.PerceptionOptionKovariConsequence,
                    C(FounderScoreAxis.Lineage, LineageId.Kovari.Value, 2),
                    C(FounderScoreAxis.Profession, nameof(ProfessionFamilyId.EngineeringManufacturing), 3),
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.ManualPrecision), 2)),
                O("myrven", Tr.Narrative.PerceptionOptionMyrvenLabel, Tr.Narrative.PerceptionOptionMyrvenConsequence,
                    C(FounderScoreAxis.Lineage, LineageId.Myrven.Value, 2),
                    C(FounderScoreAxis.Profession, nameof(ProfessionFamilyId.SocialRelations), 3),
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Observation), 2))),

            Q("orientation", Tr.Narrative.OrientationTitle, Tr.Narrative.OrientationBody, 0f,
                O("vaelun", Tr.Narrative.OrientationOptionVaelunLabel, Tr.Narrative.OrientationOptionVaelunConsequence,
                    C(FounderScoreAxis.Lineage, LineageId.Vaelun.Value, 2),
                    C(FounderScoreAxis.Profession, nameof(ProfessionFamilyId.ExplorationSurvival), 3),
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Orientation), 2)),
                O("orveth", Tr.Narrative.OrientationOptionOrvethLabel, Tr.Narrative.OrientationOptionOrvethConsequence,
                    C(FounderScoreAxis.Lineage, LineageId.Orveth.Value, 2),
                    C(FounderScoreAxis.Profession, nameof(ProfessionFamilyId.CommerceAdministration), 3),
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Memory), 1)),
                O("caelith", Tr.Narrative.OrientationOptionCaelithLabel, Tr.Narrative.OrientationOptionCaelithConsequence,
                    C(FounderScoreAxis.Lineage, LineageId.Caelith.Value, 2),
                    C(FounderScoreAxis.Profession, nameof(ProfessionFamilyId.ResearchEducation), 3),
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Observation), 2)),
                O("theryn", Tr.Narrative.OrientationOptionTherynLabel, Tr.Narrative.OrientationOptionTherynConsequence,
                    C(FounderScoreAxis.Lineage, LineageId.Theryn.Value, 2),
                    C(FounderScoreAxis.Profession, nameof(ProfessionFamilyId.SocialRelations), 3),
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Empathy), 2))),

            Q("threshold", Tr.Narrative.ThresholdTitle, Tr.Narrative.ThresholdBody, 0f,
                O("support", Tr.Narrative.ThresholdOptionSupportLabel, Tr.Narrative.ThresholdOptionSupportConsequence,
                    C(FounderScoreAxis.CombatStyle, nameof(CombatStyleId.DefensiveSupport), 3),
                    C(FounderScoreAxis.Weapon, nameof(WeaponPreferenceId.Shield), 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Patient), 2),
                    C(FounderScoreAxis.RiskProfile, "Measured", 2)),
                O("control", Tr.Narrative.ThresholdOptionControlLabel, Tr.Narrative.ThresholdOptionControlConsequence,
                    C(FounderScoreAxis.CombatStyle, nameof(CombatStyleId.TerritorialControl), 3),
                    C(FounderScoreAxis.Weapon, nameof(WeaponPreferenceId.Polearm), 3),
                    C(FounderScoreAxis.LeadershipStyle, "Directive", 2),
                    C(FounderScoreAxis.Control, "Bounded", 2)),
                O("mobility", Tr.Narrative.ThresholdOptionMobilityLabel, Tr.Narrative.ThresholdOptionMobilityConsequence,
                    C(FounderScoreAxis.CombatStyle, nameof(CombatStyleId.Mobility), 3),
                    C(FounderScoreAxis.Weapon, nameof(WeaponPreferenceId.Blade), 2),
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Adaptability), 2)),
                O("precision", Tr.Narrative.ThresholdOptionPrecisionLabel, Tr.Narrative.ThresholdOptionPrecisionConsequence,
                    C(FounderScoreAxis.CombatStyle, nameof(CombatStyleId.Precision), 3),
                    C(FounderScoreAxis.Weapon, nameof(WeaponPreferenceId.Ranged), 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Cautious), 2)),
                O("assault", Tr.Narrative.ThresholdOptionAssaultLabel, Tr.Narrative.ThresholdOptionAssaultConsequence,
                    C(FounderScoreAxis.CombatStyle, nameof(CombatStyleId.DirectAssault), 3),
                    C(FounderScoreAxis.Weapon, nameof(WeaponPreferenceId.Heavy), 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Bold), 2),
                    C(FounderScoreAxis.RiskProfile, "Bold", 3))),

            Q("ground", Tr.Narrative.GroundTitle, Tr.Narrative.GroundBody, 0.15f,
                O("clarity", Tr.Narrative.GroundOptionClarityLabel, Tr.Narrative.GroundOptionClarityConsequence,
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.SelfControl), 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Disciplined), 2)),
                O("shared", Tr.Narrative.GroundOptionSharedLabel, Tr.Narrative.GroundOptionSharedConsequence,
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.ManualPrecision), 2),
                    C(FounderScoreAxis.Attachment, "Keepsake", 3)),
                O("move", Tr.Narrative.GroundOptionMoveLabel, Tr.Narrative.GroundOptionMoveConsequence,
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Adaptability), 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Pragmatic), 2)),
                O("mark", Tr.Narrative.GroundOptionMarkLabel, Tr.Narrative.GroundOptionMarkConsequence,
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Orientation), 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Curious), 2),
                    C(FounderScoreAxis.Lineage, LineageId.Vaelun.Value, 1))),

            Q("unchanged", Tr.Narrative.UnchangedTitle, Tr.Narrative.UnchangedBody, 0.45f,
                O("protect", Tr.Narrative.UnchangedOptionProtectLabel, Tr.Narrative.UnchangedOptionProtectConsequence,
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Protective), 3),
                    C(FounderScoreAxis.LeadershipStyle, "Protective", 2),
                    C(FounderScoreAxis.Lineage, LineageId.Ardhen.Value, 1),
                    C(FounderScoreAxis.Lineage, LineageId.Theryn.Value, 1)),
                O("freedom", Tr.Narrative.UnchangedOptionFreedomLabel, Tr.Narrative.UnchangedOptionFreedomConsequence,
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Independent), 3),
                    C(FounderScoreAxis.Autonomy, "OpenFuture", 3),
                    C(FounderScoreAxis.Lineage, LineageId.Myrven.Value, 1),
                    C(FounderScoreAxis.Lineage, LineageId.Kovari.Value, 1)),
                O("understand", Tr.Narrative.UnchangedOptionUnderstandLabel, Tr.Narrative.UnchangedOptionUnderstandConsequence,
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Curious), 3),
                    C(FounderScoreAxis.Openness, "Inquiry", 3),
                    C(FounderScoreAxis.Lineage, LineageId.Caelith.Value, 1),
                    C(FounderScoreAxis.Lineage, LineageId.Eirune.Value, 1)),
                O("paths", Tr.Narrative.UnchangedOptionPathsLabel, Tr.Narrative.UnchangedOptionPathsConsequence,
                    C(FounderScoreAxis.Aptitude, nameof(AptitudeId.Orientation), 3),
                    C(FounderScoreAxis.Trait, nameof(PersonalityTraitId.Tenacious), 2),
                    C(FounderScoreAxis.Lineage, LineageId.Vaelun.Value, 1),
                    C(FounderScoreAxis.Lineage, LineageId.Orveth.Value, 1))),
        });

    public static FounderNarrativeQuestion GetQuestion(string id)
    {
        foreach (FounderNarrativeQuestion question in Questions)
        {
            if (question.Id == id) return question;
        }
        throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown narrative question.");
    }

    private static FounderNarrativeQuestion Q(
        string id,
        string title,
        string text,
        float reveal,
        params FounderNarrativeChoice[] choices)
    {
        var scoredChoices = new FounderNarrativeChoice[choices.Length];
        for (int index = 0; index < choices.Length; index++)
        {
            FounderNarrativeChoice choice = choices[index];
            ScoreContribution[] cube = CubeContributions(id, choice.Id);
            if (cube.Length == 0)
            {
                scoredChoices[index] = choice;
                continue;
            }
            var combined = new ScoreContribution[choice.Contributions.Count + cube.Length];
            for (int score = 0; score < choice.Contributions.Count; score++)
            {
                combined[score] = choice.Contributions[score];
            }
            Array.Copy(cube, 0, combined, choice.Contributions.Count, cube.Length);
            scoredChoices[index] = choice with { Contributions = Array.AsReadOnly(combined) };
        }
        return new FounderNarrativeQuestion(id, title, text, Array.AsReadOnly(scoredChoices), reveal);
    }

    private static FounderNarrativeChoice O(
        string id,
        string text,
        string consequence,
        params ScoreContribution[] scores) =>
        new(id, text, consequence, Array.AsReadOnly(scores), consequence);

    private static FounderNarrativeChoice O(
        string id,
        string text,
        string consequence,
        string prologue,
        params ScoreContribution[] scores) =>
        new(id, text, consequence, Array.AsReadOnly(scores), prologue);

    private static ScoreContribution C(
        FounderScoreAxis axis,
        string value,
        int weight) =>
        new(axis, value, weight);

    private static ScoreContribution[] CubeContributions(string questionId, string choiceId) =>
        (questionId, choiceId) switch
        {
            ("hand", "hold") => Cube(CubeScoring.BodyValueId, CubeScoring.BondValueId),
            ("hand", "observe") => Cube(CubeScoring.ReachValueId),
            ("hand", "stabilise") => Cube(CubeScoring.StabilityValueId, CubeScoring.DomainValueId),
            ("hand", "call") => Cube(CubeScoring.BondValueId, CubeScoring.ReachValueId),
            ("word", "find") => Cube(CubeScoring.ImpulseValueId, CubeScoring.ReachValueId),
            ("word", "return") => Cube(CubeScoring.StabilityValueId, CubeScoring.BondValueId),
            ("word", "remember") => Cube(CubeScoring.DomainValueId),
            ("word", "continue") => Cube(CubeScoring.ImpulseValueId),
            ("detail", "name") => Cube(CubeScoring.BondValueId),
            ("detail", "hands") => Cube(CubeScoring.BodyValueId, CubeScoring.DomainValueId),
            ("detail", "object") => Cube(CubeScoring.StabilityValueId),
            ("detail", "journey") => Cube(CubeScoring.ImpulseValueId, CubeScoring.ReachValueId),
            ("old-form", "exact") => Cube(CubeScoring.StabilityValueId, CubeScoring.DomainValueId),
            ("old-form", "sensation") => Cube(CubeScoring.BodyValueId, CubeScoring.BondValueId),
            ("old-form", "separate") => Cube(CubeScoring.DomainValueId),
            ("old-form", "release") => Cube(CubeScoring.ImpulseValueId, CubeScoring.ReachValueId),
            ("time", "cause") => Cube(CubeScoring.DomainValueId),
            ("time", "feeling") => Cube(CubeScoring.BondValueId),
            ("time", "promises") => Cube(CubeScoring.StabilityValueId),
            ("time", "places") => Cube(CubeScoring.ReachValueId),
            ("mortality", "weight") => Cube(CubeScoring.BodyValueId, CubeScoring.StabilityValueId),
            ("mortality", "understand") => Cube(CubeScoring.DomainValueId),
            ("mortality", "others") => Cube(CubeScoring.BondValueId),
            ("mortality", "new") => Cube(CubeScoring.ImpulseValueId),
            ("perception", "ardhen") => Vertex(true, true, true),
            ("perception", "eirune") => Vertex(true, true, false),
            ("perception", "kovari") => Vertex(true, false, true),
            ("perception", "myrven") => Vertex(false, true, false),
            ("orientation", "vaelun") => Vertex(true, false, false),
            ("orientation", "orveth") => Vertex(false, true, true),
            ("orientation", "caelith") => Vertex(false, false, false),
            ("orientation", "theryn") => Vertex(false, false, true),
            ("threshold", "support") => Cube(CubeScoring.BondValueId, CubeScoring.StabilityValueId),
            ("threshold", "control") => Cube(CubeScoring.StabilityValueId, CubeScoring.DomainValueId),
            ("threshold", "mobility") => Cube(CubeScoring.ImpulseValueId, CubeScoring.ReachValueId),
            ("threshold", "precision") => Cube(CubeScoring.DomainValueId),
            ("threshold", "assault") => Cube(CubeScoring.BodyValueId, CubeScoring.ImpulseValueId),
            ("ground", "clarity") => Cube(CubeScoring.StabilityValueId, CubeScoring.DomainValueId),
            ("ground", "shared") => Cube(CubeScoring.BondValueId),
            ("ground", "move") => Cube(CubeScoring.ImpulseValueId),
            ("ground", "mark") => Cube(CubeScoring.ReachValueId),
            ("unchanged", "protect") => Cube(CubeScoring.BondValueId, CubeScoring.StabilityValueId),
            ("unchanged", "freedom") => Cube(CubeScoring.ImpulseValueId, CubeScoring.ReachValueId),
            ("unchanged", "understand") => Cube(CubeScoring.BondValueId, CubeScoring.DomainValueId),
            ("unchanged", "paths") => Cube(CubeScoring.StabilityValueId, CubeScoring.ReachValueId),
            _ => Array.Empty<ScoreContribution>(),
        };

    private static ScoreContribution[] Cube(params string[] valueIds)
    {
        var scores = new ScoreContribution[valueIds.Length];
        for (int index = 0; index < valueIds.Length; index++)
        {
            scores[index] = C(FounderScoreAxis.Cube, valueIds[index], 1);
        }
        return scores;
    }

    private static ScoreContribution[] Vertex(bool body, bool stability, bool domain) => Cube(
        body ? CubeScoring.BodyValueId : CubeScoring.BondValueId,
        stability ? CubeScoring.StabilityValueId : CubeScoring.ImpulseValueId,
        domain ? CubeScoring.DomainValueId : CubeScoring.ReachValueId);
}
