#nullable enable
using Godot;

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
        OffsetLeft = 0f;
        OffsetTop = 0f;
        OffsetRight = 0f;
        OffsetBottom = StripHeight;
    }
}
