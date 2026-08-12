using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="BuildingOrientation"/>.
/// Architecture Hardening A7.
/// </summary>
internal static class BuildingOrientationSaveIds
{
    public const string SouthId = "South";
    public const string WestId = "West";
    public const string NorthId = "North";
    public const string EastId = "East";

    public static string ToId(BuildingOrientation value) => value switch
    {
        BuildingOrientation.South => SouthId,
        BuildingOrientation.West => WestId,
        BuildingOrientation.North => NorthId,
        BuildingOrientation.East => EastId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out BuildingOrientation value)
    {
        switch (id)
        {
            case SouthId: value = BuildingOrientation.South; return true;
            case WestId: value = BuildingOrientation.West; return true;
            case NorthId: value = BuildingOrientation.North; return true;
            case EastId: value = BuildingOrientation.East; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
