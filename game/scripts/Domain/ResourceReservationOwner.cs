namespace WorldofGoses.Domain;

public readonly record struct ResourceReservationOwner(
    ResourceReservationOwnerKind Kind,
    int EntityId);
