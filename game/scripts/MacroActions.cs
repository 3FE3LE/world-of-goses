#nullable enable
using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Compact, edge-to-edge navigation strip directly below the city status bar.
/// Its centered action row stays comfortably away from desktop edges while
/// the dark surface itself spans the complete viewport width.
/// </summary>
[GlobalClass]
public partial class MacroActions : PanelContainer
{
    private const float StripHeight = 42f;

    public override void _Ready()
    {
        // HUD chrome: the navigation buttons must read identically at
        // 03:00 and at noon, so they sit above the ambient tint.
        OverlayLayers.Apply(this, OverlayLayers.Hud);
        OffsetLeft = 0f;
        OffsetTop = 0f;
        OffsetRight = 0f;
        OffsetBottom = StripHeight;
    }
}
