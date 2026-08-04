namespace WorldofGoses.Domain;

public enum FrontageCellState
{
    Available = 0,
    ReservedByBuilding = 1,
    ReservedAsCorridor = 2,
    Infrastructure = 3,
    Unavailable = 4,
    NaturalResource = 5,
}
