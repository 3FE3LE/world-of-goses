namespace WorldofGoses.Domain;

/// <summary>
/// Player-selectable standard lot for a construction blueprint.
/// Parcel coordinates are projected for presentation; the stable
/// <see cref="ParcelId"/> remains the persisted identity.
/// </summary>
public readonly record struct ConstructionLot(
    ParcelId ParcelId,
    int ParcelColumn,
    int ParcelRow,
    int LotColumn,
    int LotRow);
