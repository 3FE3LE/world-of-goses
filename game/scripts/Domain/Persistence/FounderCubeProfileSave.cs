namespace WorldofGoses.Domain.Persistence;

/// <summary>Serializable form of the founder's canonical cube profile.</summary>
public sealed class FounderCubeProfileSave
{
    public int Body { get; set; }
    public int Bond { get; set; }
    public int Stability { get; set; }
    public int Impulse { get; set; }
    public int Mastery { get; set; }
    public int Reach { get; set; }
}
