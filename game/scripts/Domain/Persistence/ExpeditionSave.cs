namespace WorldofGoses.Domain.Persistence;

public sealed class ExpeditionSave
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int LeadCitizenId { get; set; }
    public int StartTick { get; set; }
    public int EndTick { get; set; }
    public string SupplyResource { get; set; } = string.Empty;
    public int SupplyAmount { get; set; }
    public string RewardResource { get; set; } = string.Empty;
    public int RewardAmount { get; set; }
    public int ReservationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ReturnedAmount { get; set; }
    public string RewardKind { get; set; } = string.Empty;
    public int? DeliveredMigrantId { get; set; }
}
