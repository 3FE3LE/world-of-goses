using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="ElementalAffinityId"/>.
/// Architecture Hardening A7. Elemental affinity has historically
/// been persisted as the lowercased enum name (legacy onboarding
/// path) and is rewritten on schema v29 to canonicalise
/// None→Silence. This mapper preserves the lowercased wire format
/// while making the contract explicit.
/// </summary>
internal static class ElementalAffinityIdSaveIds
{
    public static string ToId(ElementalAffinityId value) => value.ToString().ToLowerInvariant();

    public static bool TryParse(string id, out ElementalAffinityId value)
    {
        if (id is null)
        {
            value = ElementalAffinityId.None;
            return false;
        }
        string canonical = id.ToUpperInvariant();
        switch (canonical)
        {
            case "WATER": value = ElementalAffinityId.Water; return true;
            case "FIRE": value = ElementalAffinityId.Fire; return true;
            case "EARTH": value = ElementalAffinityId.Earth; return true;
            case "AIR": value = ElementalAffinityId.Air; return true;
            case "AETHER": value = ElementalAffinityId.Aether; return true;
            case "SILENCE": value = ElementalAffinityId.Silence; return true;
            case "NONE": value = ElementalAffinityId.None; return true;
            default:
                value = ElementalAffinityId.None;
                return false;
        }
    }
}
