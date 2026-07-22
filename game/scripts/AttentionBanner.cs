#nullable enable
using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Pulsing banner shown at the bottom of the macro view when the
/// city has at least one building or project that needs attention
/// (no workers, exhausted, missing inputs). The banner drives a
/// gentle pulse so the player notices it without losing focus from
/// the rest of the world.
/// </summary>
public partial class AttentionBanner : PanelContainer
{
    private const float PulseSeconds = 0.9f;
    private const float ToastExclusionHeight = 88f;
    private const float HorizontalInset = 32f;

    private Label _label = null!;
    private Tween? _pulse;
    private int _lastAttentionCount = -1;

    public override void _Ready()
    {
        EnsureBuilt();
    }

    private void EnsureBuilt()
    {
        if (_label is not null) return;
        SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        OffsetLeft = HorizontalInset;
        OffsetRight = -HorizontalInset;
        OffsetBottom = -ToastExclusionHeight;
        AddThemeStyleboxOverride("panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));
        MouseFilter = Control.MouseFilterEnum.Ignore;
        Visible = false;

        var align = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        align.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(align);

        _label = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            ThemeTypeVariation = "SectionTitle",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _label.AddThemeColorOverride("font_color", new Color(0.95f, 0.78f, 0.38f));
        align.AddChild(_label);
    }

    /// <summary>
    /// Updates the banner with the latest attention count. The first
    /// call also kicks off the pulse animation if any attention is
    /// required.
    /// </summary>
    public void Update(int attentionCount)
    {
        EnsureBuilt();
        if (attentionCount == _lastAttentionCount) return;
        _lastAttentionCount = attentionCount;
        if (attentionCount <= 0)
        {
            Visible = false;
            _pulse?.Kill();
            _pulse = null;
            return;
        }
        _label.Text = attentionCount == 1
            ? "1 building needs attention — open the chronicle."
            : $"{attentionCount} buildings need attention — open the chronicle.";
        Visible = true;
        StartPulse();
    }

    private void StartPulse()
    {
        _pulse?.Kill();
        Modulate = new Color(1f, 1f, 1f, 1f);
        _pulse = CreateTween().SetLoops();
        _pulse.TweenProperty(this, "modulate:a", 0.55f, PulseSeconds);
        _pulse.TweenProperty(this, "modulate:a", 1f, PulseSeconds);
    }
}
