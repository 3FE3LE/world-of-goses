using Godot;

namespace WorldofGoses.Ui;

[GlobalClass]
public partial class SafeAreaMarginContainer : MarginContainer
{
    [Export] public int MinimumInset { get; set; } = 16;
    [Export] public int MinimumTopInset { get; set; } = -1;

    public override void _Ready()
    {
        GetViewport().SizeChanged += ApplySafeArea;
        ApplySafeArea();
    }

    public override void _ExitTree()
    {
        GetViewport().SizeChanged -= ApplySafeArea;
    }

    private void ApplySafeArea()
    {
        Vector2I windowSize = DisplayServer.WindowGetSize();
        Rect2I safeArea = DisplayServer.GetDisplaySafeArea();
        Vector2 viewportSize = GetViewportRect().Size;
        if (windowSize.X <= 0 || windowSize.Y <= 0) return;

        float scaleX = viewportSize.X / windowSize.X;
        float scaleY = viewportSize.Y / windowSize.Y;
        int left = Mathf.Max(MinimumInset, Mathf.RoundToInt(safeArea.Position.X * scaleX));
        int minimumTop = MinimumTopInset >= 0 ? MinimumTopInset : MinimumInset;
        int top = Mathf.Max(minimumTop, Mathf.RoundToInt(safeArea.Position.Y * scaleY));
        int right = Mathf.Max(MinimumInset,
            Mathf.RoundToInt((windowSize.X - safeArea.End.X) * scaleX));
        int bottom = Mathf.Max(MinimumInset,
            Mathf.RoundToInt((windowSize.Y - safeArea.End.Y) * scaleY));

        AddThemeConstantOverride("margin_left", left);
        AddThemeConstantOverride("margin_top", top);
        AddThemeConstantOverride("margin_right", right);
        AddThemeConstantOverride("margin_bottom", bottom);
    }
}
