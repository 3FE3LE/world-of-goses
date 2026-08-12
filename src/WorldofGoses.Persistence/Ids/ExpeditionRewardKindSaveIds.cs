using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="ExpeditionRewardKind"/>.
/// Architecture Hardening A7.
/// </summary>
internal static class ExpeditionRewardKindSaveIds
{
    public const string SuppliesId = "Supplies";
    public const string MigrantId = "Migrant";
    public const string DiscoveryId = "Discovery";

    public static string ToId(ExpeditionRewardKind value) => value switch
    {
        ExpeditionRewardKind.Supplies => SuppliesId,
        ExpeditionRewardKind.Migrant => MigrantId,
        ExpeditionRewardKind.Discovery => DiscoveryId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out ExpeditionRewardKind value)
    {
        switch (id)
        {
            case SuppliesId: value = ExpeditionRewardKind.Supplies; return true;
            case MigrantId: value = ExpeditionRewardKind.Migrant; return true;
            case DiscoveryId: value = ExpeditionRewardKind.Discovery; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
