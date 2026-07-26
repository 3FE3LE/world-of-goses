#nullable enable
using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Top-right macro action strip (View hero, Construction, Menu, Recon,
/// Citizens). Applies the OS display safe area as direct <c>Offset*</c>
/// deltas in <c>_Ready</c> and on every viewport resize, so the action
/// buttons stay inside notches and rounded corners without introducing
/// a wrapping <c>MarginContainer</c> — the previous attempt rendered as
/// a visible grey band above the HUD and was reverted (TO_DO.md 2026-07-22).
/// </summary>
[GlobalClass]
public partial class MacroActions : PanelContainer
{
    public override void _Ready()
    {
        ApplySafeArea();
        GetViewport().SizeChanged += ApplySafeArea;
    }

    public override void _ExitTree()
    {
        if (GetViewport() is { } viewport)
        {
            viewport.SizeChanged -= ApplySafeArea;
        }
    }

    private void ApplySafeArea()
    {
        SafeArea.ApplyOffsets(this, minimumInsetPx: 16);
    }
}
