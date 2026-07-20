#nullable enable
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Single source of truth for building textures. Maps every
/// <see cref="BuildingKind"/> that has art to its `res://` path and
/// exposes the canvas size so layout code can size slots without
/// re-anchoring scenes when a placeholder is replaced.
///
/// The catalog is read once per kind at module load; the textures
/// themselves stay cached by Godot's resource cache, so additional
/// accesses are constant-time. Kinds that have no art yet
/// (<see cref="BuildingKind.Smithy"/>, <see cref="BuildingKind.PotionLab"/>)
/// return <c>null</c> from <see cref="GetTexturePath"/>; render code
/// must handle that case rather than crash.
///
/// When the real Pixelorama sources replace the placeholders, only
/// this file changes. Scenes, scripts, and the `BuildingPlot`
/// `[Export]` defaults keep working as long as the path strings
/// below stay identical.
/// </summary>
public static class BuildingArt
{
    public const string AssetsRoot = "res://assets/buildings/";

    /// <summary>
    /// Logical canvas size for the macro plot texture, in pixels.
    /// Matches the asset canvas (128 × 128 for production buildings,
    /// 64 × 64 for the founding Home) so a `TextureRect.Keep` slot
    /// renders without distortion.
    /// </summary>
    public const int HomeWidth = 64;
    public const int HomeHeight = 64;

    public const int QuarryWidth = 128;
    public const int QuarryHeight = 128;

    public const int FarmWidth = 128;
    public const int FarmHeight = 128;

    /// <summary>
    /// Returns the `res://` path of the placeholder texture for
    /// <paramref name="kind"/>, or <c>null</c> if no art exists for
    /// that kind yet. The path always points inside
    /// <see cref="AssetsRoot"/>.
    /// </summary>
    public static string? GetTexturePath(BuildingKind kind) => kind switch
    {
        BuildingKind.Home => AssetsRoot + "home_idle.png",
        BuildingKind.Quarry => AssetsRoot + "quarry_idle.png",
        BuildingKind.Farm => AssetsRoot + "farm_idle.png",
        _ => null,
    };

    /// <summary>
    /// Returns the canvas size that matches the texture for
    /// <paramref name="kind"/>, or <c>null</c> if no art exists.
    /// Layout code should size slots to this so a placeholder swap
    /// does not need a re-anchor.
    /// </summary>
    public static Vector2? GetCanvasSize(BuildingKind kind) => kind switch
    {
        BuildingKind.Home => new Vector2(HomeWidth, HomeHeight),
        BuildingKind.Quarry => new Vector2(QuarryWidth, QuarryHeight),
        BuildingKind.Farm => new Vector2(FarmWidth, FarmHeight),
        _ => null,
    };
}