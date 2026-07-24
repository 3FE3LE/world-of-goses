namespace WorldofGoses.Domain;

public sealed record ResourceReservation(
    ResourceReservationId Id,
    ResourceType Resource,
    int Amount,
    ResourceReservationOwner Owner);
