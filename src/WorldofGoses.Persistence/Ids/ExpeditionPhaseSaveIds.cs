using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="ExpeditionPhase"/>.
/// Architecture Hardening A7.
/// </summary>
internal static class ExpeditionPhaseSaveIds
{
    public const string OutboundId = "Outbound";
    public const string EncounterId = "Encounter";
    public const string ObjectiveId = "Objective";
    public const string ReturningId = "Returning";
    public const string ResolvedId = "Resolved";
    public const string RetreatingId = "Retreating";

    public static string ToId(ExpeditionPhase value) => value switch
    {
        ExpeditionPhase.Outbound => OutboundId,
        ExpeditionPhase.Encounter => EncounterId,
        ExpeditionPhase.Objective => ObjectiveId,
        ExpeditionPhase.Returning => ReturningId,
        ExpeditionPhase.Resolved => ResolvedId,
        ExpeditionPhase.Retreating => RetreatingId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out ExpeditionPhase value)
    {
        switch (id)
        {
            case OutboundId: value = ExpeditionPhase.Outbound; return true;
            case EncounterId: value = ExpeditionPhase.Encounter; return true;
            case ObjectiveId: value = ExpeditionPhase.Objective; return true;
            case ReturningId: value = ExpeditionPhase.Returning; return true;
            case ResolvedId: value = ExpeditionPhase.Resolved; return true;
            case RetreatingId: value = ExpeditionPhase.Retreating; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
