namespace WorldofGoses.Domain;

/// <summary>Semantic lifecycle of the single EG-3 cultivation plot.</summary>
public enum CultivationPlotState
{
    Prepared = 0,
    Sown = 1,
    Growing = 2,
    Ready = 3,
    Spent = 4,
}
