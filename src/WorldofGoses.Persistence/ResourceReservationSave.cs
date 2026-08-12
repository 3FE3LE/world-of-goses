using WorldofGoses.Domain;
namespace WorldofGoses.Persistence;

public sealed class ResourceReservationSave
{
    public int Id { get; set; }
    public string Resource { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string OwnerKind { get; set; } = string.Empty;
    public int OwnerEntityId { get; set; }
}
