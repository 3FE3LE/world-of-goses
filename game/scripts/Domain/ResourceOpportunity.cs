using System;

namespace WorldofGoses.Domain;

public sealed class ResourceOpportunity
{
    public ResourceOpportunity(
        ResourceOpportunityId id,
        ResourceOpportunityKind kind,
        ResourceOpportunityState state = ResourceOpportunityState.Available,
        ExpeditionId? reservedByExpeditionId = null)
    {
        if (id.Value <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (!Enum.IsDefined(state)) throw new ArgumentOutOfRangeException(nameof(state));
        if ((state == ResourceOpportunityState.Reserved) != reservedByExpeditionId.HasValue)
        {
            throw new ArgumentException(
                "Only a reserved opportunity may identify its expedition.",
                nameof(reservedByExpeditionId));
        }
        Id = id;
        Kind = kind;
        State = state;
        ReservedByExpeditionId = reservedByExpeditionId;
    }

    public ResourceOpportunityId Id { get; }
    public ResourceOpportunityKind Kind { get; }
    public ResourceOpportunityState State { get; private set; }
    public ExpeditionId? ReservedByExpeditionId { get; private set; }

    internal bool TryReserve(ExpeditionId expeditionId)
    {
        if (State != ResourceOpportunityState.Available || expeditionId.Value <= 0) return false;
        State = ResourceOpportunityState.Reserved;
        ReservedByExpeditionId = expeditionId;
        return true;
    }

    internal bool Release(ExpeditionId expeditionId)
    {
        if (State != ResourceOpportunityState.Reserved
            || ReservedByExpeditionId != expeditionId) return false;
        State = ResourceOpportunityState.Available;
        ReservedByExpeditionId = null;
        return true;
    }

    internal bool Deplete(ExpeditionId expeditionId)
    {
        if (State != ResourceOpportunityState.Reserved
            || ReservedByExpeditionId != expeditionId) return false;
        State = ResourceOpportunityState.Depleted;
        ReservedByExpeditionId = null;
        return true;
    }
}
