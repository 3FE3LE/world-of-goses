using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="CultivationPlotState"/>.
/// Architecture Hardening A7. Schema v24 introduced the Cultivation
/// Site lifecycle.
/// </summary>
internal static class CultivationPlotStateSaveIds
{
    public const string PreparedId = "Prepared";
    public const string SownId = "Sown";
    public const string GrowingId = "Growing";
    public const string ReadyId = "Ready";
    public const string SpentId = "Spent";

    public static string ToId(CultivationPlotState value) => value switch
    {
        CultivationPlotState.Prepared => PreparedId,
        CultivationPlotState.Sown => SownId,
        CultivationPlotState.Growing => GrowingId,
        CultivationPlotState.Ready => ReadyId,
        CultivationPlotState.Spent => SpentId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out CultivationPlotState value)
    {
        switch (id)
        {
            case PreparedId: value = CultivationPlotState.Prepared; return true;
            case SownId: value = CultivationPlotState.Sown; return true;
            case GrowingId: value = CultivationPlotState.Growing; return true;
            case ReadyId: value = CultivationPlotState.Ready; return true;
            case SpentId: value = CultivationPlotState.Spent; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
