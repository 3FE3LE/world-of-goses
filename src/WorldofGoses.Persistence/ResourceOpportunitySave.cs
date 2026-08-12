#nullable enable

using WorldofGoses.Domain;
namespace WorldofGoses.Persistence;

public sealed class ResourceOpportunitySave
{
    public int Id { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public int? ReservedByExpeditionId { get; set; }
}
