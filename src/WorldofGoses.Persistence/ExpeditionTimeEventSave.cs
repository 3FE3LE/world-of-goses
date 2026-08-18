namespace WorldofGoses.Persistence;

/// <summary>
/// One entry of an expedition's time ledger. Added in v37, when an
/// expedition's return stopped being fixed at dispatch.
/// </summary>
public sealed class ExpeditionTimeEventSave
{
    public string Kind { get; set; } = string.Empty;
    public int Ticks { get; set; }
    public int AtTick { get; set; }
}
