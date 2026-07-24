#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

public partial class ConstructionPlacementOverlay : Control
{
    [Signal]
    public delegate void PlacementConfirmedEventHandler(
        int constructionKind,
        int parcelId,
        int parcelColumn,
        int parcelRow,
        int lotColumn,
        int lotRow);

    [Signal] public delegate void PlacementCancelledEventHandler();

    private readonly List<(ConstructionLot Lot, Button Button)> _lotButtons = new();
    private readonly StyleBoxFlat _availableStyle = NewLotStyle(new Color("#2f8f5b99"));
    private readonly StyleBoxFlat _selectedStyle = NewLotStyle(new Color("#f2c94ccc"));
    private Control _lotLayer = null!;
    private Label _instruction = null!;
    private Button _confirmButton = null!;
    private Button _cancelButton = null!;
    private ConstructionKind _kind;
    private ConstructionLot? _selectedLot;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = 40;

        var scrim = new ColorRect
        {
            Color = new Color(0.03f, 0.05f, 0.05f, 0.38f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        scrim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(scrim);

        _lotLayer = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _lotLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_lotLayer);

        _instruction = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            ThemeTypeVariation = "SectionTitle",
            MouseFilter = MouseFilterEnum.Ignore,
            OffsetTop = 12,
            OffsetBottom = 48,
        };
        _instruction.SetAnchorsPreset(LayoutPreset.TopWide);
        AddChild(_instruction);

        var footer = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            OffsetTop = -64,
            OffsetBottom = -16,
        };
        footer.SetAnchorsPreset(LayoutPreset.BottomWide);
        footer.AddThemeConstantOverride("separation", 12);
        AddChild(footer);

        _confirmButton = new Button
        {
            Text = "Confirm placement",
            ThemeTypeVariation = "ButtonPrimary",
            Disabled = true,
        };
        _cancelButton = new IconButton
        {
            ThemeTypeVariation = "ButtonSecondary",
        };
        ((IconButton)_cancelButton).SetIconAndLabel(IconPaths.Close, "Cancel");
        footer.AddChild(_confirmButton);
        footer.AddChild(_cancelButton);

        _confirmButton.Pressed += ConfirmSelection;
        _cancelButton.Pressed += CancelSelection;
        Resized += RepositionLots;
        Hide();
    }

    public override void _ExitTree()
    {
        if (_confirmButton is not null) _confirmButton.Pressed -= ConfirmSelection;
        if (_cancelButton is not null) _cancelButton.Pressed -= CancelSelection;
        Resized -= RepositionLots;
        ClearLots();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible || !@event.IsActionPressed("ui_cancel")) return;
        CancelSelection();
        GetViewport().SetInputAsHandled();
    }

    public void Begin(ConstructionKind kind, IReadOnlyList<ConstructionLot> lots)
    {
        _kind = kind;
        _selectedLot = null;
        _confirmButton.Disabled = true;
        _instruction.Text = $"Choose a lot for {ConstructionRules.DisplayNameFor(kind)}";
        ClearLots();
        foreach (ConstructionLot lot in lots)
        {
            var button = new Button
            {
                Text = string.Empty,
                TooltipText =
                    $"Parcel {lot.ParcelId.Value} · lot {lot.LotColumn + 1},{lot.LotRow + 1}",
                FocusMode = FocusModeEnum.All,
                MouseDefaultCursorShape = CursorShape.PointingHand,
            };
            button.AddThemeStyleboxOverride("normal", _availableStyle);
            button.AddThemeStyleboxOverride("hover", _selectedStyle);
            button.AddThemeStyleboxOverride("focus", _selectedStyle);
            button.Pressed += () => SelectLot(lot);
            _lotLayer.AddChild(button);
            _lotButtons.Add((lot, button));
        }
        Show();
        RepositionLots();
        if (_lotButtons.Count > 0) _lotButtons[0].Button.GrabFocus();
        else _cancelButton.GrabFocus();
    }

    private void SelectLot(ConstructionLot lot)
    {
        _selectedLot = lot;
        _confirmButton.Disabled = false;
        foreach ((ConstructionLot candidate, Button button) in _lotButtons)
        {
            button.AddThemeStyleboxOverride(
                "normal",
                candidate == lot ? _selectedStyle : _availableStyle);
        }
        _confirmButton.GrabFocus();
    }

    private void ConfirmSelection()
    {
        if (_selectedLot is not ConstructionLot lot) return;
        EmitSignal(
            SignalName.PlacementConfirmed,
            (int)_kind,
            lot.ParcelId.Value,
            lot.ParcelColumn,
            lot.ParcelRow,
            lot.LotColumn,
            lot.LotRow);
    }

    private void CancelSelection()
    {
        Hide();
        EmitSignal(SignalName.PlacementCancelled);
    }

    private void RepositionLots()
    {
        foreach ((ConstructionLot lot, Button button) in _lotButtons)
        {
            Rect2 parcel = OrthogonalParcelTerrain.CalculateParcelRect(
                Size,
                lot.ParcelColumn,
                lot.ParcelRow);
            Vector2 lotSize = parcel.Size / ParcelGrid.LotsPerAxis;
            button.Position = parcel.Position
                + new Vector2(lot.LotColumn * lotSize.X, lot.LotRow * lotSize.Y)
                + new Vector2(3, 3);
            button.Size = lotSize - new Vector2(6, 6);
        }
    }

    private void ClearLots()
    {
        foreach ((ConstructionLot _, Button button) in _lotButtons)
        {
            button.QueueFree();
        }
        _lotButtons.Clear();
    }

    private static StyleBoxFlat NewLotStyle(Color color) =>
        new()
        {
            BgColor = color,
            BorderColor = new Color("#f4e7b2"),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        };
}
