#nullable enable
using System;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// One slot in the building-detail view. Holds the position, name
/// label, and click hit area for a citizen. The visual sprite lives
/// in the <see cref="CitizenSpriteBank"/>'s carrier so the same
/// citizen never appears twice when the player navigates between
/// buildings.
/// </summary>
public partial class VisibleWorkerSlot : Control
{
    [Signal] public delegate void CitizenActivatedEventHandler(int citizenId);

    public CitizenId CitizenId { get; private set; } = default;
    public BuildingId BuildingId { get; private set; } = default;

    private CitizenSpriteCarrier? _carrier;
    private string _citizenName = string.Empty;
    public bool IsExiting { get; private set; }

    // Field initializer so Configure() can set Text before _Ready().
    private readonly Label _nameLabel = new()
    {
        Position = new Vector2(0, 2),
        Size = new Vector2(PresentationConstants.DetailedCitizenWidth, 18),
        HorizontalAlignment = HorizontalAlignment.Center,
        MouseFilter = Control.MouseFilterEnum.Ignore,
        ThemeTypeVariation = "BodySmall",
    };

    private TooltipButton _hitArea = null!;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(
            PresentationConstants.DetailedCitizenWidth,
            PresentationConstants.DetailedCitizenHeight);

        _hitArea = new TooltipButton
        {
            Size = new Vector2(
                PresentationConstants.DetailedCitizenWidth,
                PresentationConstants.DetailedCitizenHeight),
            Flat = true,
            TooltipText = "Click to remove this worker",
        };
        _hitArea.Pressed += () => EmitSignal(SignalName.CitizenActivated, CitizenId.Value);
        AddChild(_hitArea);

        _nameLabel.Text = _citizenName;
        AddChild(_nameLabel);
    }

    public void Configure(BuildingId buildingId, CitizenId citizenId, string citizenName)
    {
        BuildingId = buildingId;
        CitizenId = citizenId;
        _citizenName = citizenName;
        if (_nameLabel != null)
        {
            _nameLabel.Text = citizenName;
        }
    }

    /// <summary>
    /// Binds the carrier for this slot. The carrier is owned by the
    /// bank; the slot is just a position marker.
    /// </summary>
    public void AttachCarrier(CitizenSpriteCarrier carrier)
    {
        _carrier = carrier;
    }

    /// <summary>
    /// Walks the carrier from the entry border to the slot center,
    /// then settles into the slash loop. The hit area is enabled only
    /// after the worker arrives so spurious clicks during the entry
    /// animation are ignored.
    /// </summary>
    public void ShowAt(Vector2 entryBorderViewport, Vector2 slotCenterViewport, Action? onComplete = null)
    {
        if (_carrier == null) return;
        bool wasHidden = _carrier.State == CitizenSpriteCarrier.VisualState.Hidden;
        IsExiting = false;
        _carrier.SetState(CitizenSpriteCarrier.VisualState.Entering);
        _hitArea.Disabled = true;
        if (wasHidden)
        {
            _carrier.SetPositionImmediate(entryBorderViewport);
        }
        _carrier.GoTo(slotCenterViewport, Vector2.Zero, () =>
        {
            if (IsExiting) return;
            _hitArea.Disabled = false;
            _carrier?.SetState(CitizenSpriteCarrier.VisualState.Working);
            _carrier?.Slash(Vector2.Down);
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// Walks the carrier from the slot center to the border (or
    /// wherever the consumer decides), then hides it.
    /// </summary>
    public void HideTo(Vector2 borderViewport, Vector2 facing, Action? onComplete = null)
    {
        if (_carrier == null)
        {
            onComplete?.Invoke();
            return;
        }
        _hitArea.Disabled = true;
        IsExiting = true;
        _carrier.SetState(CitizenSpriteCarrier.VisualState.Exiting);
        _carrier.GoTo(borderViewport, facing, () =>
        {
            if (!IsExiting) return;
            _carrier?.SetState(CitizenSpriteCarrier.VisualState.Hidden);
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// Hides the carrier immediately without animation. Used when the
    /// context changes (different building) so the carrier is
    /// available for the next show without lingering in the slot.
    /// </summary>
    public void HideImmediate()
    {
        _hitArea.Disabled = true;
        IsExiting = false;
        _carrier?.SetState(CitizenSpriteCarrier.VisualState.Hidden);
    }

    /// <summary>
    /// Returns the carrier to its entry border and starts the entry
    /// walk. Used when the player re-assigns a worker during the
    /// exit animation — the carrier turns around and walks back to
    /// the slot instead of being recreated from the other side.
    /// </summary>
    public void ResumeTo(Vector2 slotCenterViewport, Action? onComplete = null)
    {
        if (_carrier == null) return;
        IsExiting = false;
        _carrier.SetState(CitizenSpriteCarrier.VisualState.Entering);
        _hitArea.Disabled = true;
        _carrier.GoTo(slotCenterViewport, Vector2.Zero, () =>
        {
            if (IsExiting) return;
            _hitArea.Disabled = false;
            _carrier?.SetState(CitizenSpriteCarrier.VisualState.Working);
            _carrier?.Slash(Vector2.Down);
            onComplete?.Invoke();
        });
    }
}
