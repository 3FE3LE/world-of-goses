using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="ElementalAffinityId"/>.
/// Architecture Hardening A7. Elemental affinity has historically
/// been persisted as the lowercased identifier (legacy onboarding
/// path) and is rewritten on schema v29 to canonicalise
/// None→Silence. This mapper preserves the lowercased wire format
/// while making the contract explicit.
/// </summary>
internal static class ElementalAffinityIdSaveIds
{
    /// <summary>
    /// Reads the identifier's own <see cref="ElementalAffinityId.Value"/>
    /// rather than <c>ToString()</c>. The two happen to agree today, but
    /// the wire format must not be a hostage to a display override:
    /// <c>ToString()</c> is a presentation concern and a future change to
    /// it would silently rewrite every save.
    /// </summary>
    public static string ToId(ElementalAffinityId value) => value.Value;

    public static bool TryParse(string id, out ElementalAffinityId value)
    {
        // `None` is deliberately [Obsolete] in the domain (DEC-0013: the
        // canonical "no affinity" is Silence). Persistence is the one
        // layer that must still name it, because pre-v29 saves on disk
        // carry "none" and the migration cannot canonicalise a value it
        // cannot parse. The suppression is scoped to this method and to
        // this reason; it must not grow to cover writes.
#pragma warning disable CS0618 // Read-only migration compatibility, see above.
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
#pragma warning restore CS0618
    }
}
