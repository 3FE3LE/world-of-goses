#nullable enable
using System;

namespace WorldofGoses.Domain;

public sealed class Expedition
{
    public Expedition(
        ExpeditionId id,
        string displayName,
        CitizenId leadCitizenId,
        int startTick,
        int endTick,
        ResourceType supplyResource,
        int supplyAmount,
        ResourceType rewardResource,
        int rewardAmount,
        ExpeditionRewardKind rewardKind,
        ResourceReservationId reservationId,
        ExpeditionStatus status = ExpeditionStatus.Active)
    {
        if (id.Value <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.", nameof(displayName));
        if (leadCitizenId.Value <= 0) throw new ArgumentOutOfRangeException(nameof(leadCitizenId));
        if (startTick < 0 || endTick < startTick) throw new ArgumentOutOfRangeException(nameof(endTick));
        if (supplyAmount <= 0) throw new ArgumentOutOfRangeException(nameof(supplyAmount));
        if (rewardKind == ExpeditionRewardKind.Supplies && rewardAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rewardAmount));
        }

        Id = id;
        DisplayName = displayName;
        LeadCitizenId = leadCitizenId;
        StartTick = startTick;
        EndTick = endTick;
        SupplyResource = supplyResource;
        SupplyAmount = supplyAmount;
        RewardResource = rewardResource;
        RewardAmount = rewardAmount;
        RewardKind = rewardKind;
        ReservationId = reservationId;
        Status = status;
    }

    public ExpeditionId Id { get; }
    public string DisplayName { get; }
    public CitizenId LeadCitizenId { get; }
    public int StartTick { get; }
    public int EndTick { get; }
    public ResourceType SupplyResource { get; }
    public int SupplyAmount { get; }
    public ResourceType RewardResource { get; }
    public int RewardAmount { get; }
    public ExpeditionRewardKind RewardKind { get; }
    public ResourceReservationId ReservationId { get; }
    public ExpeditionStatus Status { get; private set; }
    public int? ReturnedAmount { get; private set; }
    public WorldEventId? DispatchEventId { get; private set; }
    public CitizenId? DeliveredMigrantId { get; private set; }

    public bool IsComplete(int currentTick) =>
        Status == ExpeditionStatus.Active && currentTick >= EndTick;

    internal void SetDispatchEventId(WorldEventId id) => DispatchEventId = id;

    internal void MarkReturnedSupplies(int amount)
    {
        ReturnedAmount = amount;
        Status = ExpeditionStatus.Returned;
    }

    internal void MarkReturnedMigrant(CitizenId migrantId, int carried)
    {
        DeliveredMigrantId = migrantId;
        ReturnedAmount = carried;
        Status = ExpeditionStatus.Returned;
    }

    internal void MarkFailed()
    {
        ReturnedAmount = 0;
        Status = ExpeditionStatus.Failed;
    }

    internal void MarkCancelled()
    {
        ReturnedAmount = 0;
        Status = ExpeditionStatus.Cancelled;
    }
}

public sealed class ExpeditionChangedEventArgs : EventArgs
{
    public ExpeditionChangedEventArgs(ExpeditionId expeditionId, ExpeditionStatus status)
    {
        ExpeditionId = expeditionId;
        Status = status;
    }

    public ExpeditionId ExpeditionId { get; }
    public ExpeditionStatus Status { get; }
}
