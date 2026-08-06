#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// Canonical profile vocabulary. The catalog contains descriptions and
/// qualitative relationships only; simulation formulas must not treat these
/// values as automatic production modifiers.
/// </summary>
public static class ProfileCatalog
{
    public static IReadOnlyList<LineageDefinition> Lineages { get; } = Array.AsReadOnly(new[]
    {
        new LineageDefinition(
            LineageId.Ardhen,
            "Ardhen",
            "Body-aware builders, extractors, rescuers and operational coordinators.",
            "Often learn through posture, weight, rhythm, vibration and practical repetition.",
            new[] { ProfessionFamilyId.Extraction, ProfessionFamilyId.ConstructionInfrastructure, ProfessionFamilyId.MedicineCare, ProfessionFamilyId.Logistics },
            new[] { ProfessionFamilyId.EngineeringManufacturing, ProfessionFamilyId.SecurityCombat, ProfessionFamilyId.AgricultureLivingSystems },
            new[] { "Abstract work without visible application", "Long diplomacy without operational feedback", "Institutions that overlook emotional health" }),
        new LineageDefinition(
            LineageId.Eirune,
            "Eirune",
            "Observers of living systems, medicine, cultivation and ecological recovery.",
            "Often learn by connecting organisms, cycles, water, soil and long-term consequences.",
            new[] { ProfessionFamilyId.AgricultureLivingSystems, ProfessionFamilyId.MedicineCare },
            new[] { ProfessionFamilyId.ResearchEducation, ProfessionFamilyId.ExplorationSurvival, ProfessionFamilyId.EngineeringManufacturing },
            new[] { "Extraction without restoration", "Highly mechanised work that removes direct observation", "Decisions that ignore long biological cycles" }),
        new LineageDefinition(
            LineageId.Kovari,
            "Kovari",
            "Engineers, maintainers and makers drawn to systems that can be understood and repaired.",
            "Often learn through components, diagnosis, adaptation, disassembly and iterative repair.",
            new[] { ProfessionFamilyId.EngineeringManufacturing, ProfessionFamilyId.Extraction, ProfessionFamilyId.Logistics },
            new[] { ProfessionFamilyId.ConstructionInfrastructure, ProfessionFamilyId.SecurityCombat, ProfessionFamilyId.MedicineCare },
            new[] { "Opaque institutions", "Systems designed not to be repaired", "Rigid plans that forbid intervention" }),
        new LineageDefinition(
            LineageId.Myrven,
            "Myrven",
            "Interpreters of people, roles, conflict, identity and public life.",
            "Often learn by observing behaviour, changing perspectives and navigating social context.",
            new[] { ProfessionFamilyId.SocialRelations, ProfessionFamilyId.CommerceAdministration, ProfessionFamilyId.MedicineCare },
            new[] { ProfessionFamilyId.ArtsCulture, ProfessionFamilyId.SecurityCombat, ProfessionFamilyId.ResearchEducation },
            new[] { "Institutions without privacy", "Roles that people cannot change", "Repetitive work stripped of social context" }),
        new LineageDefinition(
            LineageId.Vaelun,
            "Vaelun",
            "Explorers, navigators, prospectors and connectors between distant communities.",
            "Often learn through movement, terrain, weather, routes and adaptation away from fixed infrastructure.",
            new[] { ProfessionFamilyId.ExplorationSurvival, ProfessionFamilyId.Logistics, ProfessionFamilyId.Extraction },
            new[] { ProfessionFamilyId.SocialRelations, ProfessionFamilyId.CommerceAdministration, ProfessionFamilyId.ConstructionInfrastructure },
            new[] { "Extremely sedentary work", "Institutions tied to one location", "Plans without alternate routes" }),
        new LineageDefinition(
            LineageId.Orveth,
            "Orveth",
            "Stewards of exchange, reserves, hospitality and the movement of value.",
            "Often learn through records, scarcity, risk, negotiation and service networks.",
            new[] { ProfessionFamilyId.CommerceAdministration, ProfessionFamilyId.Logistics },
            new[] { ProfessionFamilyId.SocialRelations, ProfessionFamilyId.AgricultureLivingSystems, ProfessionFamilyId.MedicineCare },
            new[] { "Work whose value is never recorded", "Distribution based entirely on informal memory", "Projects that ignore reserves and risk" }),
        new LineageDefinition(
            LineageId.Caelith,
            "Caelith",
            "Researchers, educators, diagnosticians and institutional planners.",
            "Often learn through models, comparison, documentation, hypotheses and cross-disciplinary analysis.",
            new[] { ProfessionFamilyId.ResearchEducation, ProfessionFamilyId.MedicineCare, ProfessionFamilyId.CommerceAdministration },
            new[] { ProfessionFamilyId.EngineeringManufacturing, ProfessionFamilyId.ExplorationSurvival, ProfessionFamilyId.SocialRelations },
            new[] { "Urgency without time to analyse", "Knowledge transmitted without explanation", "Institutions that reject revision" }),
        new LineageDefinition(
            LineageId.Theryn,
            "Theryn",
            "Carers, coordinators, artists and protectors of group cohesion.",
            "Often learn through shared practice, emotional communication, crisis response and collective memory.",
            new[] { ProfessionFamilyId.MedicineCare, ProfessionFamilyId.SocialRelations, ProfessionFamilyId.ArtsCulture, ProfessionFamilyId.SecurityCombat },
            new[] { ProfessionFamilyId.ResearchEducation, ProfessionFamilyId.CommerceAdministration, ProfessionFamilyId.Logistics },
            new[] { "Long periods of isolated work", "Institutions that suppress expression", "Systems that ignore group wellbeing" }),
    });

    public static IReadOnlyList<ProfileOption<AptitudeId>> Aptitudes { get; } = Array.AsReadOnly(new[]
    {
        new ProfileOption<AptitudeId>(AptitudeId.Observation, "Observation", "Noticing small changes, patterns and inconsistencies."),
        new ProfileOption<AptitudeId>(AptitudeId.Empathy, "Empathy", "Recognising and responding to another person's state."),
        new ProfileOption<AptitudeId>(AptitudeId.ManualPrecision, "Manual precision", "Controlling fine movements and tools reliably."),
        new ProfileOption<AptitudeId>(AptitudeId.Strength, "Strength", "Applying and sustaining physical force."),
        new ProfileOption<AptitudeId>(AptitudeId.Orientation, "Orientation", "Building a stable sense of position, route and direction."),
        new ProfileOption<AptitudeId>(AptitudeId.Memory, "Memory", "Retaining details and recalling useful relationships."),
        new ProfileOption<AptitudeId>(AptitudeId.Creativity, "Creativity", "Combining ideas and finding unconventional approaches."),
        new ProfileOption<AptitudeId>(AptitudeId.SelfControl, "Self-control", "Maintaining deliberate action under pressure."),
        new ProfileOption<AptitudeId>(AptitudeId.RiskTolerance, "Risk tolerance", "Acting effectively while outcomes remain uncertain."),
        new ProfileOption<AptitudeId>(AptitudeId.Adaptability, "Adaptability", "Changing methods when conditions invalidate a plan."),
    });

    public static IReadOnlyList<ProfileOption<ProfessionFamilyId>> ProfessionFamilies { get; } = Array.AsReadOnly(new[]
    {
        new ProfileOption<ProfessionFamilyId>(ProfessionFamilyId.Extraction, "Extraction", "Mining, quarrying, logging, gathering, excavation and prospecting."),
        new ProfileOption<ProfessionFamilyId>(ProfessionFamilyId.ConstructionInfrastructure, "Construction & infrastructure", "Masonry, structural carpentry, roads, bridges, waterworks, fortification and maintenance."),
        new ProfileOption<ProfessionFamilyId>(ProfessionFamilyId.AgricultureLivingSystems, "Agriculture & living systems", "Cultivation, livestock, forestry, breeding, soil management and water treatment."),
        new ProfileOption<ProfessionFamilyId>(ProfessionFamilyId.MedicineCare, "Medicine & care", "First aid, surgery, pharmacology, rehabilitation, mental health and community care."),
        new ProfileOption<ProfessionFamilyId>(ProfessionFamilyId.EngineeringManufacturing, "Engineering & manufacturing", "Mechanics, smithing, automation, industrial alchemy, fabrication, demolition and repair."),
        new ProfileOption<ProfessionFamilyId>(ProfessionFamilyId.ExplorationSurvival, "Exploration & survival", "Cartography, navigation, tracking, hunting, camps, reconnaissance and survival."),
        new ProfileOption<ProfessionFamilyId>(ProfessionFamilyId.Logistics, "Logistics", "Transport, storage, inventories, distribution, expeditions and supplies."),
        new ProfileOption<ProfessionFamilyId>(ProfessionFamilyId.CommerceAdministration, "Commerce & administration", "Accounting, negotiation, valuation, insurance, contracts and public management."),
        new ProfileOption<ProfessionFamilyId>(ProfessionFamilyId.ResearchEducation, "Research & education", "Research, diagnosis, statistics, history, astronomy, teaching and institutional design."),
        new ProfileOption<ProfessionFamilyId>(ProfessionFamilyId.SocialRelations, "Social relations", "Diplomacy, mediation, psychology, performance, translation, intelligence and communication."),
        new ProfileOption<ProfessionFamilyId>(ProfessionFamilyId.SecurityCombat, "Security & combat", "Defence, vigilance, tactics, escort, combat, rescue and threat handling."),
        new ProfileOption<ProfessionFamilyId>(ProfessionFamilyId.ArtsCulture, "Arts & culture", "Music, narrative, design, ceremony, visual arts and cultural preservation."),
    });

    public static IReadOnlyList<ProfileOption<ElementalAffinityId>> ElementalAffinities { get; } = Array.AsReadOnly(new[]
    {
        new ProfileOption<ElementalAffinityId>(ElementalAffinityId.Water, "Water", "Affinity with flow, patience and adaptation."),
        new ProfileOption<ElementalAffinityId>(ElementalAffinityId.Fire, "Fire", "Affinity with transformation, intensity and initiative."),
        new ProfileOption<ElementalAffinityId>(ElementalAffinityId.Earth, "Earth", "Affinity with stability, endurance and structure."),
        new ProfileOption<ElementalAffinityId>(ElementalAffinityId.Air, "Air", "Affinity with movement, distance and changing perspective."),
        new ProfileOption<ElementalAffinityId>(ElementalAffinityId.Aether, "Aether", "Affinity with connections that are not purely material."),
        new ProfileOption<ElementalAffinityId>(ElementalAffinityId.Silence, "Silence", "Affinity with isolation, stabilisation and controlled neutralisation."),
    });

    public static IReadOnlyList<ProfileOption<CombatStyleId>> CombatStyles { get; } = Array.AsReadOnly(new[]
    {
        new ProfileOption<CombatStyleId>(CombatStyleId.DefensiveSupport, "Defensive support", "Protect allies and preserve a stable line."),
        new ProfileOption<CombatStyleId>(CombatStyleId.TerritorialControl, "Territorial control", "Shape movement and deny dangerous space."),
        new ProfileOption<CombatStyleId>(CombatStyleId.Mobility, "Mobility", "Reposition quickly and choose when to engage."),
        new ProfileOption<CombatStyleId>(CombatStyleId.Precision, "Precision", "Commit only when timing and placement are favourable."),
        new ProfileOption<CombatStyleId>(CombatStyleId.DirectAssault, "Direct assault", "Break resistance through concentrated pressure."),
    });

    public static IReadOnlyList<ProfileOption<WeaponPreferenceId>> WeaponPreferences { get; } = Array.AsReadOnly(new[]
    {
        new ProfileOption<WeaponPreferenceId>(WeaponPreferenceId.Polearm, "Polearm", "Reach, formation work and controlled distance."),
        new ProfileOption<WeaponPreferenceId>(WeaponPreferenceId.Heavy, "Heavy weapons", "Momentum, impact and commitment."),
        new ProfileOption<WeaponPreferenceId>(WeaponPreferenceId.Blade, "Blades", "Versatile close-range cutting weapons."),
        new ProfileOption<WeaponPreferenceId>(WeaponPreferenceId.Ranged, "Ranged weapons", "Threaten targets while maintaining distance."),
        new ProfileOption<WeaponPreferenceId>(WeaponPreferenceId.Shield, "Shield", "Interception, cover and protection of others."),
        new ProfileOption<WeaponPreferenceId>(WeaponPreferenceId.Unarmed, "Unarmed", "Grappling, restraint and fighting without a weapon."),
    });

    public static IReadOnlyList<ProfileOption<PersonalityTraitId>> PersonalityTraits { get; } = Array.AsReadOnly(new[]
    {
        new ProfileOption<PersonalityTraitId>(PersonalityTraitId.Patient, "Patient", "Comfortable allowing understanding or opportunity to develop."),
        new ProfileOption<PersonalityTraitId>(PersonalityTraitId.Protective, "Protective", "Quick to place another person's safety at the centre of a choice."),
        new ProfileOption<PersonalityTraitId>(PersonalityTraitId.Reflective, "Reflective", "Inclined to revisit experiences before drawing conclusions."),
        new ProfileOption<PersonalityTraitId>(PersonalityTraitId.Curious, "Curious", "Drawn toward unanswered questions and unfamiliar situations."),
        new ProfileOption<PersonalityTraitId>(PersonalityTraitId.Disciplined, "Disciplined", "Able to sustain deliberate routines and standards."),
        new ProfileOption<PersonalityTraitId>(PersonalityTraitId.Cooperative, "Cooperative", "Prefers to build solutions with other people."),
        new ProfileOption<PersonalityTraitId>(PersonalityTraitId.Ambitious, "Ambitious", "Seeks demanding goals and visible change."),
        new ProfileOption<PersonalityTraitId>(PersonalityTraitId.Cautious, "Cautious", "Looks for avoidable harm before committing."),
        new ProfileOption<PersonalityTraitId>(PersonalityTraitId.Bold, "Bold", "Willing to act before certainty is available."),
        new ProfileOption<PersonalityTraitId>(PersonalityTraitId.Reserved, "Reserved", "Shares thoughts and feelings selectively."),
        new ProfileOption<PersonalityTraitId>(PersonalityTraitId.Compassionate, "Compassionate", "Responds strongly to suffering and vulnerability."),
        new ProfileOption<PersonalityTraitId>(PersonalityTraitId.Pragmatic, "Pragmatic", "Prioritises approaches that work under present conditions."),
        new ProfileOption<PersonalityTraitId>(PersonalityTraitId.Independent, "Independent", "Prefers to retain personal agency and responsibility."),
        new ProfileOption<PersonalityTraitId>(PersonalityTraitId.Diplomatic, "Diplomatic", "Looks for language and process that keep dialogue possible."),
        new ProfileOption<PersonalityTraitId>(PersonalityTraitId.Tenacious, "Tenacious", "Continues despite resistance and repeated setbacks."),
        new ProfileOption<PersonalityTraitId>(PersonalityTraitId.Restless, "Restless", "Needs movement, novelty or changing challenges."),
    });

    public static IReadOnlyList<ProfileOption<PoliticalOrientationId>> PoliticalOrientations { get; } = Array.AsReadOnly(new[]
    {
        new ProfileOption<PoliticalOrientationId>(PoliticalOrientationId.Communitarian, "Communitarian", "Emphasises shared obligations and collective wellbeing."),
        new ProfileOption<PoliticalOrientationId>(PoliticalOrientationId.Autonomist, "Autonomist", "Emphasises local choice and personal agency."),
        new ProfileOption<PoliticalOrientationId>(PoliticalOrientationId.Institutional, "Institutional", "Emphasises durable rules, offices and accountable procedure."),
        new ProfileOption<PoliticalOrientationId>(PoliticalOrientationId.Traditionalist, "Traditionalist", "Emphasises inherited practices and cultural continuity."),
        new ProfileOption<PoliticalOrientationId>(PoliticalOrientationId.Reformist, "Reformist", "Emphasises revision when existing systems fail their purpose."),
        new ProfileOption<PoliticalOrientationId>(PoliticalOrientationId.Mercantile, "Mercantile", "Emphasises exchange, contracts and material circulation."),
        new ProfileOption<PoliticalOrientationId>(PoliticalOrientationId.Ecological, "Ecological", "Emphasises long-term relations between society and living systems."),
        new ProfileOption<PoliticalOrientationId>(PoliticalOrientationId.SecurityOriented, "Security-oriented", "Emphasises preparedness, continuity and protection from threats."),
    });

    public static IReadOnlyList<ProfileOption<SpiritualPostureId>> SpiritualPostures { get; } = Array.AsReadOnly(new[]
    {
        new ProfileOption<SpiritualPostureId>(SpiritualPostureId.Devout, "Devout", "Committed to an inherited or chosen spiritual tradition."),
        new ProfileOption<SpiritualPostureId>(SpiritualPostureId.Contemplative, "Contemplative", "Seeks meaning through reflection, practice and attention."),
        new ProfileOption<SpiritualPostureId>(SpiritualPostureId.Syncretic, "Syncretic", "Draws meaning from more than one spiritual tradition."),
        new ProfileOption<SpiritualPostureId>(SpiritualPostureId.Agnostic, "Agnostic", "Leaves ultimate spiritual claims open or unresolved."),
        new ProfileOption<SpiritualPostureId>(SpiritualPostureId.Skeptical, "Skeptical", "Subjects spiritual claims and institutions to deliberate doubt."),
        new ProfileOption<SpiritualPostureId>(SpiritualPostureId.Secular, "Secular", "Does not make spiritual belief central to public or personal identity."),
    });

    public static bool Contains(LineageId id) => FindLineage(id) is not null;
    public static bool Contains(AptitudeId id) => ContainsOption(Aptitudes, id);
    public static bool Contains(ProfessionFamilyId id) => ContainsOption(ProfessionFamilies, id);
    public static bool Contains(ElementalAffinityId id) => ContainsOption(ElementalAffinities, id);
    public static bool Contains(CombatStyleId id) => ContainsOption(CombatStyles, id);
    public static bool Contains(WeaponPreferenceId id) => ContainsOption(WeaponPreferences, id);
    public static bool Contains(PersonalityTraitId id) => ContainsOption(PersonalityTraits, id);
    public static bool Contains(PoliticalOrientationId id) => ContainsOption(PoliticalOrientations, id);
    public static bool Contains(SpiritualPostureId id) => ContainsOption(SpiritualPostures, id);

    public static LineageDefinition Get(LineageId id) =>
        FindLineage(id) ?? throw new ArgumentOutOfRangeException(nameof(id), $"Unknown lineage '{id.Value}'.");

    public static string DisplayName(AptitudeId id) => GetOption(Aptitudes, id).DisplayName;
    public static string DisplayName(ProfessionFamilyId id) => GetOption(ProfessionFamilies, id).DisplayName;
    public static string DisplayName(ElementalAffinityId id) => GetOption(ElementalAffinities, id).DisplayName;
    public static string DisplayName(ElementalAffinity affinity) => ElementalAffinityDisplay.DisplayName(affinity);
    public static string DisplayName(PhysicalExpression expression) => PhysicalExpressionDisplay.DisplayName(expression);
    public static string DisplayName(WeaponFamily family) => WeaponFamilyDisplay.DisplayName(family);
    public static string DisplayName(CombatStyleId id) => GetOption(CombatStyles, id).DisplayName;
    public static string DisplayName(WeaponPreferenceId id) => GetOption(WeaponPreferences, id).DisplayName;
    public static string DisplayName(PersonalityTraitId id) => GetOption(PersonalityTraits, id).DisplayName;
    public static string DisplayName(PoliticalOrientationId id) => GetOption(PoliticalOrientations, id).DisplayName;
    public static string DisplayName(SpiritualPostureId id) => GetOption(SpiritualPostures, id).DisplayName;

    private static LineageDefinition? FindLineage(LineageId id)
    {
        foreach (var lineage in Lineages)
        {
            if (lineage.Id == id) return lineage;
        }
        return null;
    }

    private static bool ContainsOption<TId>(IReadOnlyList<ProfileOption<TId>> options, TId id)
        where TId : struct
    {
        foreach (var option in options)
        {
            if (EqualityComparer<TId>.Default.Equals(option.Id, id)) return true;
        }
        return false;
    }

    private static ProfileOption<TId> GetOption<TId>(IReadOnlyList<ProfileOption<TId>> options, TId id)
        where TId : struct
    {
        foreach (var option in options)
        {
            if (EqualityComparer<TId>.Default.Equals(option.Id, id)) return option;
        }
        throw new ArgumentOutOfRangeException(nameof(id), $"Unknown profile option '{id}'.");
    }
}
