#nullable enable
using Godot;

namespace WorldofGoses;

/// <summary>
/// Macro-only building representation. Its root is the reserved lot, while
/// art, interaction, and routing use a separate solid footprint.
/// </summary>
public partial class MacroBuildingView : Control
{
    [Signal] public delegate void ActivatedEventHandler(int entityId);

    public int EntityId { get; private set; }
    public bool IsUnderConstruction { get; private set; }
    public Rect2 SolidLocalRect { get; private set; }

    private CityMacroSnapshot.PlotItem? _item;
    private TextureRect _art = null!;
    private ColorRect _placeholder = null!;
    private Label _nameLabel = null!;
    private Label _statusLabel = null!;
    private Button _button = null!;
    private Panel _outline = null!;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ClipContents = false;

        _art = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_art);

        _placeholder = new ColorRect
        {
            Color = new Color("#6b4529"),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_placeholder);

        _nameLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ThemeTypeVariation = "BodySmall",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_nameLabel);

        _statusLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ThemeTypeVariation = "BodySmall",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_statusLabel);

        _outline = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        var outlineStyle = new StyleBoxFlat
        {
            BgColor = Colors.Transparent,
            BorderColor = LineageThemeRegistry.IconAccent,
        };
        outlineStyle.SetBorderWidthAll(2);
        _outline.AddThemeStyleboxOverride("panel", outlineStyle);
        _outline.Hide();
        AddChild(_outline);

        _button = new Button
        {
            Flat = true,
            FocusMode = FocusModeEnum.All,
        };
        _button.MouseEntered += _outline.Show;
        _button.MouseExited += _outline.Hide;
        _button.FocusEntered += _outline.Show;
        _button.FocusExited += _outline.Hide;
        _button.Pressed += () => EmitSignal(SignalName.Activated, EntityId);
        AddChild(_button);

        ApplyItem();
        ApplyGeometry();
    }

    public void Configure(CityMacroSnapshot.PlotItem item)
    {
        _item = item;
        EntityId = item.Id.Value;
        IsUnderConstruction = item.IsUnderConstruction;
        if (_art is not null) ApplyItem();
    }

    public void SetPlacement(Rect2 reservedRect, Rect2 solidLocalRect)
    {
        Position = reservedRect.Position.Floor();
        Size = reservedRect.Size.Floor();
        SolidLocalRect = new Rect2(
            solidLocalRect.Position.Floor(),
            solidLocalRect.Size.Floor());
        if (_art is not null) ApplyGeometry();
    }

    public Rect2 GetSolidGlobalRect()
    {
        Vector2 globalStart = GetGlobalTransform() * SolidLocalRect.Position;
        return new Rect2(globalStart, SolidLocalRect.Size);
    }

    private void ApplyItem()
    {
        if (_item is null) return;
        string? texturePath = BuildingArt.GetTexturePath(_item.Kind);
        _art.Texture = texturePath is null
            ? null
            : ResourceLoader.Load<Texture2D>(texturePath);
        _art.Visible = _art.Texture is not null;
        _placeholder.Visible = _art.Texture is null;
        _nameLabel.Text = _item.DisplayName;
        _statusLabel.Text = _item.IsUnderConstruction
            ? _item.Enabled
                ? BuildingPlot.ConstructionProgressLabel(
                    _item.Progress,
                    _item.RequiredWork)
                : "Paused"
            : string.Empty;
        _statusLabel.Visible = _item.IsUnderConstruction;
        _button.TooltipText = _item.IsUnderConstruction
            ? "Open construction progress"
            : $"Enter {_item.DisplayName}";
        _button.Disabled = !_item.Enabled && !_item.IsUnderConstruction;
        _art.Modulate = _item.IsUnderConstruction
            ? new Color(1f, 1f, 1f, 0.72f)
            : Colors.White;
    }

    private void ApplyGeometry()
    {
        _art.Position = SolidLocalRect.Position;
        _art.Size = SolidLocalRect.Size;
        _placeholder.Position = SolidLocalRect.Position;
        _placeholder.Size = SolidLocalRect.Size;
        _button.Position = SolidLocalRect.Position;
        _button.Size = SolidLocalRect.Size;
        _outline.Position = SolidLocalRect.Position;
        _outline.Size = SolidLocalRect.Size;

        float labelHeight = 18f;
        _nameLabel.Position = new Vector2(0, Mathf.Max(0, SolidLocalRect.Position.Y));
        _nameLabel.Size = new Vector2(Size.X, labelHeight);
        _statusLabel.Position = new Vector2(
            0,
            Mathf.Max(0, Size.Y - labelHeight));
        _statusLabel.Size = new Vector2(Size.X, labelHeight);
    }
}
