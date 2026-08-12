using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="ExpeditionEncounterOutcome"/>.
/// Architecture Hardening A7.
/// </summary>
internal static class ExpeditionEncounterOutcomeSaveIds
{
    public const string SetbackId = "Setback";
    public const string PartialSuccessId = "PartialSuccess";
    public const string FullSuccessId = "FullSuccess";

    public static string ToId(ExpeditionEncounterOutcome value) => value switch
    {
        ExpeditionEncounterOutcome.Setback => SetbackId,
        ExpeditionEncounterOutcome.PartialSuccess => PartialSuccessId,
        ExpeditionEncounterOutcome.FullSuccess => FullSuccessId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out ExpeditionEncounterOutcome value)
    {
        switch (id)
        {
            case SetbackId: value = ExpeditionEncounterOutcome.Setback; return true;
            case PartialSuccessId: value = ExpeditionEncounterOutcome.PartialSuccess; return true;
            case FullSuccessId: value = ExpeditionEncounterOutcome.FullSuccess; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
