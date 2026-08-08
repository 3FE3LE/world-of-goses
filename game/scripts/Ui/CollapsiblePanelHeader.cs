#nullable enable

using System;
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// A section header the player can fold: the whole strip is the hit target, and a
/// chevron on its right says which way it will go.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="Button"/> rather than a panel with a button inside it. The strip is
/// the affordance, so making the strip the control is what gives it a real hover
/// state, a real focus ring and keyboard activation for free. Nesting a transparent
/// button inside a <c>PanelContainer</c> would have meant overriding four styleboxes
/// to <c>StyleBoxEmpty</c> at the call site, which is the inline theming
/// <c>UI_PATTERNS.md</c> §5 rules out; the <c>HudCollapsibleHeader</c> variation keeps
/// the look in <c>default_theme.tres</c> where the authority belongs.
/// </para>
/// <para>
/// The chevron is not ornament. Expanded and collapsed differ by surface tint alone
/// otherwise, and the invariants forbid a state carried by colour only — so the glyph
/// is the state, and its cell is reserved at a fixed
/// <see cref="Tokens.HudGlyphCell"/> so the title cannot jog sideways when it swaps.
/// </para>
/// </remarks>
[GlobalClass]
public partial class CollapsiblePanelHeader : Button
{
    /// <summary>
    /// Raised after <see cref="Expanded"/> changes, with the new state.
    /// </summary>
    /// <remarks>
    /// Named for the property rather than the gesture because <c>Toggled</c> is
    /// already a <see cref="BaseButton"/> signal. A C# event of that name compiles,
    /// shadows the inherited one, and leaves a caller writing <c>header.Toggled +=</c>
    /// with no way to tell which of the two they subscribed to.
    /// </remarks>
    public event Action<bool>? ExpandedChanged;

    private readonly TextureRect _chevron;
    private bool _expanded = true;

    public CollapsiblePanelHeader(string title, bool expanded = true)
    {
        ThemeTypeVariation = "HudCollapsibleHeader";
        Text = title;
        Alignment = HorizontalAlignment.Left;
        CustomMinimumSize = new Vector2(0, Tokens.HudHeaderHeight);
        FocusMode = FocusModeEnum.All;
        _expanded = expanded;

        // The chevron rides in its own right-anchored cell rather than as the
        // Button's own Icon, because Godot draws a Button icon to the *left* of
        // its text and this glyph belongs on the far right.
        var glyphCell = new MarginContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            AnchorLeft = 1.0f,
            AnchorRight = 1.0f,
            AnchorTop = 0.0f,
            AnchorBottom = 1.0f,
            OffsetLeft = -(Tokens.HudGlyphCell + Tokens.SpacingBase),
            OffsetRight = -Tokens.SpacingBase,
            GrowHorizontal = GrowDirection.Begin,
        };
        AddChild(glyphCell);

        _chevron = new TextureRect
        {
            StretchMode = TextureRect.StretchModeEnum.KeepCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = LineageThemeRegistry.IconAccent,
        };
        glyphCell.AddChild(_chevron);

        ApplyChevron();
        Pressed += () => Expanded = !Expanded;
    }

    /// <summary>Whether the section this header owns is open.</summary>
    public bool Expanded
    {
        get => _expanded;
        set
        {
            if (_expanded == value) return;
            _expanded = value;
            ApplyChevron();
            ExpandedChanged?.Invoke(_expanded);
        }
    }

    private void ApplyChevron() =>
        _chevron.Texture = ResourceLoader.Load<Texture2D>(
            _expanded ? IconPaths.ChevronUp : IconPaths.ChevronDown);
}
