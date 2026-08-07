#nullable enable
using System;
using Godot;

namespace WorldofGoses.Prototypes;

/// <summary>
/// One street's obstacles — buildings and natural resources — as their own
/// canvas item, so they can be ordered against the citizen sprites.
///
/// <para>
/// <b>Why this exists.</b> The macro view painted the whole world with
/// immediate-mode commands in its own <c>_Draw()</c>, while citizens are
/// <c>CitizenSpriteCarrier</c> nodes reparented into it. In Godot a
/// <c>CanvasItem</c> emits its own commands first and then draws its children,
/// so every citizen was painted after the entire world regardless of depth: a
/// founder standing behind a nearer tree was drawn on top of it. The two were
/// never being compared — they sat on different ordering axes.
/// </para>
///
/// <para>
/// Splitting the obstacles into one layer per street puts them on the same axis
/// as the carriers: both are children with a <c>ZIndex</c> derived from depth.
/// The ground stays in the parent's own <c>_Draw()</c>, which is exactly right
/// — terrain is always behind everything, and leaving it there keeps the
/// perspective floor code untouched.
/// </para>
/// </summary>
public partial class StreetBandLayer : Node2D
{
    /// <summary>The street this layer paints.</summary>
    public int Street { get; set; }

    /// <summary>
    /// Paints the band. The layer passes itself as the canvas so the view's
    /// draw helpers emit onto this node rather than onto the view.
    /// </summary>
    public Action<CanvasItem, int>? Painter { get; set; }

    public override void _Draw()
    {
        Painter?.Invoke(this, Street);
    }
}
