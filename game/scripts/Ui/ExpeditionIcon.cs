#nullable enable

using Godot;
using WorldofGoses;
using WorldofGoses.Domain;

namespace WorldofGoses.Ui;

/// <summary>
/// Resolves the leading identity glyph for an at-a-glance expedition card.
/// </summary>
/// <remarks>
/// <para>
/// The card used to render a generic leaf on every expedition because no
/// expedition-specific icon existed and inventing a per-expedition asset
/// is art direction the bible hasn't signed off. This helper picks the
/// strongest available signal from the existing read-only projection so
/// different expeditions read differently without adding new domain data:
/// </para>
/// <list type="number">
///   <item>
///     <description>
///       The expedition's <c>SupplyResource</c>, rendered with the
///       project-owned <see cref="ResourceIcon"/> so the player recognises
///       the resource everywhere it appears (top ticker, city summary,
///       city inventory rows).
///     </description>
///   </item>
///   <item>
///     <description>
///       Fallback to a generic backpack glyph so an expedition with no
///       supply reservation is never invisible. The
///       <see cref="ExpeditionRailSnapshot"/> does not expose the
///       opportunity kind or the reward resource, and expanding it would
///       touch a domain file and is explicitly out of scope.
///     </description>
///   </item>
/// </list>
/// </remarks>
public static class ExpeditionIcon
{
    /// <summary>
    /// Builds the leading identity <see cref="Control"/> for an expedition card.
    /// </summary>
    /// <remarks>
    /// Returns a <see cref="ResourceIcon"/> or a <see cref="TextureRect"/>
    /// sized to the standard 24 px inline cell so the caller can drop the
    /// result into a <c>MarginContainer</c> without further measurement.
    /// </remarks>
    public static Control Leading(ExpeditionRailSnapshot.Item item)
    {
        return item.SupplyResource is ResourceType resource
            ? ResourceIconCell(resource)
            : TextureIconCell(IconPaths.Backpack);
    }

    private static Control ResourceIconCell(ResourceType resourceType) => new ResourceIcon(resourceType)
    {
        CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.IconInline),
        SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
    };

    private static Control TextureIconCell(string iconPath) => new TextureRect
    {
        Texture = ResourceLoader.Load<Texture2D>(iconPath),
        StretchMode = TextureRect.StretchModeEnum.Keep,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.IconInline),
        SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        MouseFilter = Control.MouseFilterEnum.Ignore,
        Modulate = LineageThemeRegistry.IconAccent,
    };
}
