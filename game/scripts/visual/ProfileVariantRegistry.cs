using System.Collections.Generic;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>Static enumeration of the canonical appearance variant ids the visual registry knows about.</summary>
public static class ProfileVariantRegistry
{
    public static IReadOnlyList<AppearanceVariantId> VariantIds { get; } = new AppearanceVariantId[]
    {
        AppearanceVariantId.Standard,
        AppearanceVariantId.Extraction,
        AppearanceVariantId.Construction,
        AppearanceVariantId.Agriculture,
        AppearanceVariantId.Care,
        AppearanceVariantId.Engineering,
        AppearanceVariantId.Exploration,
        AppearanceVariantId.Logistics,
        AppearanceVariantId.Commerce,
        AppearanceVariantId.Research,
        AppearanceVariantId.Social,
        AppearanceVariantId.Security,
        AppearanceVariantId.Arts,
    };
}
