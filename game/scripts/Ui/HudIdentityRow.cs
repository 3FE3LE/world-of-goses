#nullable enable

using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// An icon beside a two-line identity: a title and the caption under it.
/// The city summary opens with one of these — founding lineage over
/// population — and it is the shape any "this is what you are looking at"
/// header wants.
/// </summary>
/// <remarks>
/// <para>
/// The icon keeps its natural 24 px cell (<see cref="Tokens.IconInline"/>)
/// rather than being asked to shrink. <see cref="TextureRect.StretchModeEnum.Keep"/>
/// draws the source at its own size regardless of the rect, so declaring a
/// smaller cell would not shrink the glyph — it would overflow it. That trap is
/// documented on <see cref="Tokens.IconInline"/> itself.
/// </para>
/// <para>
/// Every label ignores the pointer and the row only passes it, so the identity
/// block never becomes an invisible hit target inside a scrollable body.
/// </para>
/// </remarks>
[GlobalClass]
public partial class HudIdentityRow : HBoxContainer
{
    private readonly TextureRect _icon;
    private readonly Label _title;
    private readonly Label _caption;

    public HudIdentityRow(
        string iconPath,
        string title,
        string caption,
        string iconTooltip = "")
    {
        MouseFilter = MouseFilterEnum.Pass;
        AddThemeConstantOverride("separation", Tokens.SpacingBase);

        _icon = new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(iconPath),
            StretchMode = TextureRect.StretchModeEnum.Keep,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.IconInline),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = LineageThemeRegistry.IconAccent,
            TooltipText = iconTooltip,
        };
        AddChild(_icon);

        var labels = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        // Zero separation: the caption is the second line of one block, not a
        // second row that happens to sit below the first.
        labels.AddThemeConstantOverride("separation", 0);
        AddChild(labels);

        _title = new Label
        {
            Text = title,
            ThemeTypeVariation = "HudHeader",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        labels.AddChild(_title);

        _caption = new Label
        {
            Text = caption,
            ThemeTypeVariation = "HudCaption",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        labels.AddChild(_caption);
    }

    public void SetTitle(string title) => _title.Text = title;

    public void SetCaption(string caption) => _caption.Text = caption;
}
