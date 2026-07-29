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
    private bool _idlePresentation;
    public bool IsExiting { get; private set; }

    // Field initializer so Configure() can set Text before _Ready().
    private readonly Label _nameLabel = new()
    {
        Position = new Vector2(0, PresentationConstants.DetailedCitizenHeight + 4),
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
            VisibleWorkerSlots.SlotHeight);

        _hitArea = new TooltipButton
        {
            Size = new Vector2(
                PresentationConstants.DetailedCitizenWidth,
                VisibleWorkerSlots.SlotHeight),
            Flat = true,
            TooltipText = UiText.Get("Click to remove this worker"),
        };
        _hitArea.Pressed += () => EmitSignal(SignalName.CitizenActivated, CitizenId.Value);
        AddChild(_hitArea);

        _nameLabel.Text = _citizenName;
        AddChild(_nameLabel);
    }

    public void Configure(
        BuildingId buildingId,
        CitizenId citizenId,
        string citizenName,
        bool idlePresentation = false)
    {
        BuildingId = buildingId;
        CitizenId = citizenId;
        _citizenName = citizenName;
        _idlePresentation = idlePresentation;
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

    public void MountCarrier(Node host)
    {
        if (_carrier is not null) CitizenSpriteBank.Instance.Mount(_carrier, host);
    }

    /// <summary>
    /// True once the carrier has actually settled into THIS slot's own
    /// resting pose (<c>Working</c> for a production building, <c>Home</c>
    /// for an idle/resting one) — the only states where nothing needs to be
    /// (re)shown. Any other state (Hidden, Entering, Macro, HeroProfile...)
    /// means the carrier either never arrived here or still belongs to a
    /// different view's context and must be reconciled by
    /// <see cref="ShowAt"/>.
    /// </summary>
    public bool CarrierIsSettledHere => _carrier?.State == (_idlePresentation
        ? CitizenSpriteCarrier.VisualState.Home
        : CitizenSpriteCarrier.VisualState.Working);

    /// <summary>
    /// Walks the carrier from the entry border to the slot center,
    /// then settles into the slash loop. The hit area is enabled only
    /// after the worker arrives so spurious clicks during the entry
    /// animation are ignored. Snaps to the entry border first unless the
    /// carrier is already mid-entrance (so a redundant re-issue of the same
    /// ShowAt does not visibly restart it): a carrier reclaimed from a
    /// completely different context (e.g. the macro view's own ambient
    /// Macro state, or a stale HeroProfile) is starting from a position in
    /// a totally unrelated coordinate space, and without the snap it would
    /// either stay invisible outside this stage's clipped bounds or crawl
    /// there one PixelMotion step at a time, which at typical macro-to-slot
    /// distances reads as "never arrives".
    /// </summary>
    public void ShowAt(Vector2 entryBorder, Vector2 slotCenter, Action? onComplete = null)
    {
        if (_carrier == null) return;
        bool needsSnap = _carrier.State != CitizenSpriteCarrier.VisualState.Entering;
        IsExiting = false;
        _carrier.SetState(CitizenSpriteCarrier.VisualState.Entering);
        _hitArea.Disabled = true;
        if (needsSnap)
        {
            _carrier.SetPositionImmediate(entryBorder);
        }
        _carrier.GoTo(slotCenter, Vector2.Zero, () =>
        {
            if (IsExiting) return;
            _hitArea.Disabled = false;
            SetSettledState();
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// Walks the carrier from the slot center to the border (or
    /// wherever the consumer decides), then hides it.
    /// </summary>
    public void HideTo(Vector2 border, Vector2 facing, Action? onComplete = null)
    {
        if (_carrier == null)
        {
            onComplete?.Invoke();
            return;
        }
        _hitArea.Disabled = true;
        IsExiting = true;
        _carrier.SetState(CitizenSpriteCarrier.VisualState.Exiting);
        _carrier.GoTo(border, facing, () =>
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
    public void ResumeTo(Vector2 slotCenter, Action? onComplete = null)
    {
        if (_carrier == null) return;
        IsExiting = false;
        _carrier.SetState(CitizenSpriteCarrier.VisualState.Entering);
        _hitArea.Disabled = true;
        _carrier.GoTo(slotCenter, Vector2.Zero, () =>
        {
            if (IsExiting) return;
            _hitArea.Disabled = false;
            SetSettledState();
            onComplete?.Invoke();
        });
    }

    private void SetSettledState()
    {
        _carrier?.SetState(_idlePresentation
            ? CitizenSpriteCarrier.VisualState.Home
            : CitizenSpriteCarrier.VisualState.Working);
        if (!_idlePresentation) _carrier?.Slash(Vector2.Down);
    }
}
