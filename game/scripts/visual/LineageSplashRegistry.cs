#nullable enable
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses.Visual;

/// <summary>
/// Resolves the splash illustration for a lineage and body variant.
///
/// <para>There are sixteen images — eight lineages × two body variants — so a
/// splash identifies a <b>kind of person</b>, never an individual. Two Caelith
/// citizens of the same body variant share one illustration. That is fine for
/// the founder, who is the only hero, but it is the reason this registry must
/// not be wired into anything that presents citizens as distinguishable
/// individuals without first solving that collision.</para>
///
/// <para>Sizing: the art is authored portrait and displayed at the full 720 px
/// logical height of the base canvas. See
/// <c>docs/world-of-goses-design-bible/08_VISUAL_UI_AND_ASSET_GUIDELINES.md</c>
/// and the note in <see cref="SplashLogicalHeight"/>.</para>
/// </summary>
public static class LineageSplashRegistry
{
    private const string BasePath = "res://assets/characters/splash";

    /// <summary>
    /// Height, in logical canvas units, at which a splash fills the screen.
    /// The project renders a 1280×720 canvas with <c>canvas_items</c> stretch,
    /// so this is the full height at every window size; at 1920×1080 the
    /// canvas scales ×1.5 and the same control covers 1080 physical pixels.
    /// Source art should therefore be authored 1080 px tall to land
    /// pixel-exact there rather than being upscaled.
    /// </summary>
    public const int SplashLogicalHeight = 720;

    /// <summary>
    /// Path to the splash for this lineage and body variant. The file is
    /// expected to exist for all eight lineages; a missing one is a broken
    /// asset import, not a runtime condition to design around.
    /// </summary>
    public static string GetTexturePath(LineageId lineage, CharacterBodyVariant bodyVariant) =>
        $"{BasePath}/{lineage.ToString().ToLowerInvariant()}_"
        + $"{(bodyVariant == CharacterBodyVariant.Female ? "female" : "male")}.png";

    /// <summary>
    /// Loads the splash, or null when the asset is absent. Callers must handle
    /// null by omitting the illustration: a profile that cannot show a picture
    /// still has to show its text.
    /// </summary>
    public static Texture2D? Load(LineageId lineage, CharacterBodyVariant bodyVariant)
    {
        string path = GetTexturePath(lineage, bodyVariant);
        return ResourceLoader.Exists(path)
            ? ResourceLoader.Load<Texture2D>(path)
            : null;
    }

    /// <summary>Convenience overload resolving the body variant from gender.</summary>
    public static Texture2D? Load(LineageId lineage, GenderId gender) =>
        Load(lineage, CharacterVisualRegistry.ResolveBodyVariant(gender));
}
