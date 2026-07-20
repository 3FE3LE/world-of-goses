namespace WorldofGoses.Domain;

/// <summary>Stable identifier for a broad family of related professions.</summary>
public readonly record struct ProfessionFamilyId(string Value)
{
    public static ProfessionFamilyId Extraction { get; } = new("extraction");
    public static ProfessionFamilyId ConstructionInfrastructure { get; } = new("construction_infrastructure");
    public static ProfessionFamilyId AgricultureLivingSystems { get; } = new("agriculture_living_systems");
    public static ProfessionFamilyId MedicineCare { get; } = new("medicine_care");
    public static ProfessionFamilyId EngineeringManufacturing { get; } = new("engineering_manufacturing");
    public static ProfessionFamilyId ExplorationSurvival { get; } = new("exploration_survival");
    public static ProfessionFamilyId Logistics { get; } = new("logistics");
    public static ProfessionFamilyId CommerceAdministration { get; } = new("commerce_administration");
    public static ProfessionFamilyId ResearchEducation { get; } = new("research_education");
    public static ProfessionFamilyId SocialRelations { get; } = new("social_relations");
    public static ProfessionFamilyId SecurityCombat { get; } = new("security_combat");
    public static ProfessionFamilyId ArtsCulture { get; } = new("arts_culture");

    public override string ToString() => Value;
}
