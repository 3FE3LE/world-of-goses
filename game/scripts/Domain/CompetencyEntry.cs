namespace WorldofGoses.Domain;

/// <summary>
/// A competency a citizen has developed together with the experience
/// accumulated in it. Composition is used so that a citizen may hold any
/// number of competencies without being bound to a fixed schema.
/// </summary>
public sealed record CompetencyEntry(CompetencyId Id, int Experience)
{
    public CompetencyEntry WithExperience(int newExperience) =>
        new(Id, newExperience);
}