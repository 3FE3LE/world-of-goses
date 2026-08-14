#nullable enable
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Persistence;

using WorldofGoses.Ui;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Read-only preview: renders the REAL city's buildings and construction
/// projects through the pseudo-3D street-perspective projection validated in
/// <see cref="MacroStreetWorld"/>, using actual <see cref="CityMacroSnapshot"/>
/// data instead of placeholders. Never mutates or saves the primary slot —
/// hydrates a throwaway <see cref="CityWorld"/> directly instead of going
/// through <see cref="CityWorldController"/>, which can write back a
/// migrated save during its normal load path. Not interactive: no building
/// clicks, no construction, no assignment — see
/// docs/engineering/architecture.md,
/// "Cámara y mundo caminable".
/// </summary>
public partial class RealCityStreetPreview : Node2D
{
    private const float CenterX = 320f;
    private const float BaseY = 380f;
    private const float LotUnitPx = 130f; // logical px per lot, lateral and depth
    private const float RoadHeightPx = 24f;
    private const float AvatarSize = 24f;
    private const int WorldParcelColumns = 4;
    private const int WorldParcelRows = 2;

    // Same cadence discipline as MacroStreetWorld (design bible §08,
    // "Pixel-motion grammar"): no continuous tweening.
    private const int TransitionSteps = 5;
    private const float DepthStepSize = 1f / TransitionSteps;

    private static readonly Color RoadColor = new("#5c5442");
    private static readonly Color BuildingColor = new("#8a7a54");
    private static readonly Color ProjectColor = new("#6d6148");
    private static readonly Color AvatarColor = new("#d9a24e");

    private readonly List<PlotBox> _plots = new();

    private int _streetCount = 1;
    private float _lateralHalfWidthPx = LotUnitPx;

    private int _currentStreet;
    private float _depthAnchor;
    private float? _depthTarget;
    private float _lateralPosition;
    private float _lateralAccumulator;
    private float _transitionAccumulator;

    private readonly record struct PlotBox(
        int Street,
        float LateralOffset,
        float Width,
        float Height,
        bool IsProject);

    public override void _Ready()
    {
        _streetCount = WorldParcelRows * ParcelGrid.LotsPerAxis;
        _lateralHalfWidthPx =
            WorldParcelColumns * ParcelGrid.LotsPerAxis * LotUnitPx * 0.5f;
        LoadRealPlots();
        QueueRedraw();
    }

    /// <summary>
    /// Hydrates the primary slot in memory only — mirrors the
    /// <c>world.Restore(save)</c> + <c>CityMacroSnapshot.From(world)</c>
    /// pattern <c>CityWorldController</c> uses internally, but skips its
    /// migration save-back step so this preview can never write the real
    /// slot. Leaves the preview empty (no crash) if no save exists yet.
    /// </summary>
    private void LoadRealPlots()
    {
        if (!WorldPersistence.SlotExists(WorldPersistence.PrimarySaveSlot)) return;
        WorldSave save = WorldPersistence.LoadFromSlot(WorldPersistence.PrimarySaveSlot);
        save = WorldPersistence.MigrateToCurrent(save);
        WorldPersistence.Validate(save);
        var world = new CityWorld();
        WorldPersistence.ApplyTo(world, save);
        CityMacroSnapshot snapshot = CityMacroSnapshot.From(world);

        AddPlots(snapshot.Buildings, isProject: false);
        AddPlots(snapshot.Projects, isProject: true);
    }

    /// <summary>
    /// Calle = lot-row (design bible §08, "Ciudad macro (perspectiva por
    /// calles)"): <c>ParcelRow * ParcelGrid.LotsPerAxis + LotRow</c>. Lots
    /// spanning more than one row (<c>LotHeight &gt; 1</c>) anchor to their
    /// nearest-to-viewer row — a known simplification for this read-only
    /// pass, not the final interactive placement.
    /// </summary>
    private void AddPlots(IReadOnlyList<CityMacroSnapshot.PlotItem> items, bool isProject)
    {
        float totalLotColumns = WorldParcelColumns * ParcelGrid.LotsPerAxis;
        foreach (CityMacroSnapshot.PlotItem item in items)
        {
            int street = item.ParcelRow * ParcelGrid.LotsPerAxis + item.LotRow;
            float lotCenterColumn = item.ParcelColumn * ParcelGrid.LotsPerAxis
                + item.LotColumn
                + item.LotWidth * 0.5f;
            float lateralOffset = (lotCenterColumn - totalLotColumns * 0.5f) * LotUnitPx;
            _plots.Add(new PlotBox(
                street,
                lateralOffset,
                item.LotWidth * LotUnitPx,
                item.LotHeight * LotUnitPx,
                isProject));
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        AdvanceLateralMovement(delta);
        AdvanceStreetTransition(delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } keyEvent) return;
        if (keyEvent.Keycode is Key.Up or Key.W) StepStreet(1);
        else if (keyEvent.Keycode is Key.Down or Key.S) StepStreet(-1);
    }

    private void StepStreet(int direction)
    {
        if (_depthTarget.HasValue) return;
        int nextStreet = Mathf.Clamp(_currentStreet + direction, 0, _streetCount - 1);
        if (nextStreet == _currentStreet) return;
        _currentStreet = nextStreet;
        _depthTarget = _currentStreet;
    }

    private void AdvanceStreetTransition(double delta)
    {
        if (!_depthTarget.HasValue) return;
        _transitionAccumulator += (float)delta;
        while (_transitionAccumulator >= PixelMotion.CadenceSeconds && _depthTarget.HasValue)
        {
            _transitionAccumulator -= PixelMotion.CadenceSeconds;
            float target = _depthTarget.Value;
            if (Mathf.Abs(target - _depthAnchor) <= DepthStepSize)
            {
                _depthAnchor = target;
                _depthTarget = null;
            }
            else
            {
                _depthAnchor += Mathf.Sign(target - _depthAnchor) * DepthStepSize;
            }
            QueueRedraw();
        }
    }

    private void AdvanceLateralMovement(double delta)
    {
        _lateralAccumulator += (float)delta;
        while (_lateralAccumulator >= PixelMotion.CadenceSeconds)
        {
            _lateralAccumulator -= PixelMotion.CadenceSeconds;
            TryStepLateral();
        }
    }

    private void TryStepLateral()
    {
        float direction = ReadLateralDirection();
        if (direction == 0f) return;
        float next = Mathf.Clamp(
            _lateralPosition + direction * PixelMotion.StepPixels,
            -_lateralHalfWidthPx,
            _lateralHalfWidthPx);
        if (next == _lateralPosition) return;
        _lateralPosition = next;
        QueueRedraw();
    }

    private static float ReadLateralDirection()
    {
        if (Input.IsActionPressed(UiInputActions.Left)) return -1f;
        if (Input.IsActionPressed(UiInputActions.Right)) return 1f;
        return 0f;
    }

    public override void _Draw()
    {
        for (int street = _streetCount - 1; street >= 0; street--)
        {
            if (!StreetDepthProjection.IsVisibleDepth(street - _depthAnchor)) continue;
            DrawStreetRow(street);
        }
        DrawAvatar();
    }

    /// <summary>
    /// Every lateral offset is relative to the avatar's own
    /// <see cref="_lateralPosition"/> — the vanishing point follows the
    /// viewer, matching the fix validated in <see cref="MacroStreetWorld"/>.
    /// </summary>
    private void DrawStreetRow(int street)
    {
        float depth = street - _depthAnchor;
        (Vector2 roadPosition, Vector2 roadScale) =
            StreetDepthProjection.Project(depth, -_lateralPosition, CenterX, BaseY);
        var roadSize = new Vector2(
            2f * _lateralHalfWidthPx * roadScale.X,
            RoadHeightPx * roadScale.Y);
        DrawRect(new Rect2(roadPosition - roadSize * 0.5f, roadSize), RoadColor);

        foreach (PlotBox plot in _plots)
        {
            if (plot.Street != street) continue;
            float relativeOffset = plot.LateralOffset - _lateralPosition;
            (Vector2 position, Vector2 scale) =
                StreetDepthProjection.Project(depth, relativeOffset, CenterX, BaseY);
            var size = new Vector2(plot.Width * scale.X, plot.Height * scale.Y);
            DrawRect(
                new Rect2(position - size * 0.5f, size),
                plot.IsProject ? ProjectColor : BuildingColor);
        }
    }

    private void DrawAvatar()
    {
        float depth = _currentStreet - _depthAnchor;
        (Vector2 position, Vector2 scale) = StreetDepthProjection.Project(depth, 0f, CenterX, BaseY);
        var size = new Vector2(AvatarSize * scale.X, AvatarSize * scale.Y);
        DrawRect(new Rect2(position - size * 0.5f, size), AvatarColor);
    }
}
