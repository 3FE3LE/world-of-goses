using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="CitizenLocation"/>.
/// Architecture Hardening A7.
/// </summary>
internal static class CitizenLocationSaveIds
{
    public const string AtHomeId = "AtHome";
    public const string InTransitId = "InTransit";
    public const string AtWorkId = "AtWork";

    public static string ToId(CitizenLocation value) => value switch
    {
        CitizenLocation.AtHome => AtHomeId,
        CitizenLocation.InTransit => InTransitId,
        CitizenLocation.AtWork => AtWorkId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out CitizenLocation value)
    {
        switch (id)
        {
            case AtHomeId: value = CitizenLocation.AtHome; return true;
            case InTransitId: value = CitizenLocation.InTransit; return true;
            case AtWorkId: value = CitizenLocation.AtWork; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
