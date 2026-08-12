using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="CitizenCommitmentKind"/>.
/// Architecture Hardening A7.
/// </summary>
internal static class CitizenCommitmentKindSaveIds
{
    public const string NoneId = "None";
    public const string BuildingWorkId = "BuildingWork";
    public const string ConstructionId = "Construction";
    public const string ExpeditionId = "Expedition";
    public const string RecoveryId = "Recovery";

    public static string ToId(CitizenCommitmentKind value) => value switch
    {
        CitizenCommitmentKind.None => NoneId,
        CitizenCommitmentKind.BuildingWork => BuildingWorkId,
        CitizenCommitmentKind.Construction => ConstructionId,
        CitizenCommitmentKind.Expedition => ExpeditionId,
        CitizenCommitmentKind.Recovery => RecoveryId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out CitizenCommitmentKind value)
    {
        switch (id)
        {
            case NoneId: value = CitizenCommitmentKind.None; return true;
            case BuildingWorkId: value = CitizenCommitmentKind.BuildingWork; return true;
            case ConstructionId: value = CitizenCommitmentKind.Construction; return true;
            case ExpeditionId: value = CitizenCommitmentKind.Expedition; return true;
            case RecoveryId: value = CitizenCommitmentKind.Recovery; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
