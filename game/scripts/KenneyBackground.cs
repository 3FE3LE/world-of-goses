using Godot;

namespace WorldofGoses;

/// <summary>
/// Full-screen tileable Kenney 9-slice background. Replaces the
/// flat <see cref="ColorRect"/> backgrounds that previously fought
/// with the pixel-art concept; a textured stone/parchment surface
/// reads as part of the world rather than a high-contrast overlay.
///
/// The texture is loaded from <see cref="TexturePath"/>, tinted with
/// the active linaje's panel accent via <see cref="CanvasItem.Modulate"/>,
/// and tiled across the screen via <see cref="TextureRect.StretchModeEnum.Tile"/>.
/// The Kenney 9-slice border is intentionally not used here: the
/// texture fills the rect, and the corners only matter if the rect
/// is small enough that the border becomes visible — full-screen
/// coverage means only the centre tile matters in practice.
/// </summary>
public partial class KenneyBackground : TextureRect
{
    [Export] public string TexturePath { get; set; } =
        "res://assets/ui/kenney/9-slice/ancient_grey.png";

    public override void _Ready()
    {
        ExpandMode = ExpandModeEnum.IgnoreSize;
        StretchMode = StretchModeEnum.Tile;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        Texture = ResourceLoader.Load<Texture2D>(TexturePath);
        Modulate = LineageThemeRegistry.IconAccent * 0.85f;

        LineageThemeRegistry.ActiveLineageChanged += OnLineageChanged;
    }

    public override void _ExitTree()
    {
        LineageThemeRegistry.ActiveLineageChanged -= OnLineageChanged;
    }

    private void OnLineageChanged(string lineage) =>
        Modulate = LineageThemeRegistry.IconAccent * 0.85f;
}