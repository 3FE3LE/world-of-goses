namespace WorldofGoses.Domain.Persistence;

public sealed class CorridorReservationSave
{
    public int Id { get; set; }
    public int RowId { get; set; }
    public int StartColumn { get; set; }
    public int FrontageColumns { get; set; }
}
