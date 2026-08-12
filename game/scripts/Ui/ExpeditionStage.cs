#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain.Combat;

namespace WorldofGoses.Ui;

/// <summary>
/// Lateral battlefield projection. It maps authoritative one-dimensional session
/// positions onto the authored HUD bounds and interpolates views without writing
/// anything back into combat.
/// </summary>
public partial class ExpeditionStage : Control
{
    private const int HorizontalPadding = 44;
    private const int CombatantWidth = 64;
    private const int CombatantHeight = 96;
    private const int GroundRatioPercent = 68;
    private const string CombatantScenePath = "res://scenes/Components/CombatantView.tscn";

    private readonly Dictionary<string, CombatantView> _views = new();
    private PackedScene _combatantScene = null!;
    private int _lastPresentedStep = -1;
    private double _domainMinimumX;
    private double _domainMaximumX = 1000;
    private bool _objectiveVisible;
    private bool _objectiveReached;
    private double _objectivePositionX;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
        ClipContents = true;
        _combatantScene = ResourceLoader.Load<PackedScene>(CombatantScenePath);
        QueueRedraw();
    }

    public void Configure(
        IReadOnlyList<CombatParticipantState> party,
        IReadOnlyList<CombatParticipantState> enemies,
        IReadOnlyList<CombatLogEntry> log,
        int step,
        double domainMinimumX,
        double domainMaximumX)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(enemies);
        ArgumentNullException.ThrowIfNull(log);
        if (domainMaximumX <= domainMinimumX)
            throw new ArgumentOutOfRangeException(nameof(domainMaximumX));
        _domainMinimumX = domainMinimumX;
        _domainMaximumX = domainMaximumX;
        _objectiveVisible = false;
        _objectiveReached = false;

        var activeIds = new HashSet<string>(StringComparer.Ordinal);
        ApplyParticipants(party, CombatSide.Party, activeIds, log, step);
        ApplyParticipants(enemies, CombatSide.Enemy, activeIds, log, step);
        RemoveMissing(activeIds);
        _lastPresentedStep = Math.Max(_lastPresentedStep, step);
        QueueRedraw();
    }

    public void ConfigureTravel(
        ExpeditionLiveSnapshot.Travel travel,
        string displayName,
        double? healthRatio,
        int worldTick)
    {
        double maximumHealth = 100;
        double currentHealth = maximumHealth * Math.Clamp(healthRatio ?? 1, 0, 1);
        var founder = new CombatParticipantState(
            "travel.founder",
            null,
            displayName,
            currentHealth,
            maximumHealth,
            false,
            travel.PositionX,
            0,
            12,
            travel.Facing,
            travel.Activity,
            0,
            CombatStature.Standard);
        Configure(
            [founder],
            Array.Empty<CombatParticipantState>(),
            Array.Empty<CombatLogEntry>(),
            worldTick,
            travel.BattlefieldMinimumX,
            travel.BattlefieldMaximumX);
        _objectiveVisible = travel.ObjectiveVisible;
        _objectiveReached = travel.ObjectiveReached;
        _objectivePositionX = travel.ObjectivePositionX;
        QueueRedraw();
    }

    internal void ShowEarlyFixture()
    {
        if (!WorldofGoses.Testing.VisualRegressionHarness.IsActive) return;
        var party = new[]
        {
            FixtureParticipant("fixture.founder", "Founder", 180, CombatFacing.Right),
        };
        var enemies = new[]
        {
            FixtureParticipant("fixture.enemy0", "Enemy", 760, CombatFacing.Left),
            FixtureParticipant("fixture.enemy1", "Enemy", 840, CombatFacing.Left),
        };
        Configure(party, enemies, Array.Empty<CombatLogEntry>(), 0, 0, 1000);
    }

    public void ClearCombatants()
    {
        foreach (CombatantView view in _views.Values) view.QueueFree();
        _views.Clear();
        _lastPresentedStep = -1;
        _objectiveVisible = false;
        _objectiveReached = false;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2I logicalSize = new(Mathf.RoundToInt(Size.X), Mathf.RoundToInt(Size.Y));
        if (logicalSize.X <= 0 || logicalSize.Y <= 0) return;

        Color sky = GetThemeColor("fill_empty");
        Color distance = GetThemeColor("fill_cooldown");
        Color ground = GetThemeColor("border_disabled");
        Color outline = GetThemeColor("border_locked");
        DrawRect(new Rect2I(0, 0, logicalSize.X, logicalSize.Y), sky);
        int horizon = logicalSize.Y * GroundRatioPercent / 100;
        DrawRect(new Rect2I(0, horizon - 48, logicalSize.X, 48), distance);
        DrawRect(new Rect2I(0, horizon, logicalSize.X, logicalSize.Y - horizon), ground);
        DrawLandscapeSilhouette(logicalSize, horizon, outline);
        if (_objectiveVisible) DrawSpiritTrailManifestation(horizon);
    }

    private void ApplyParticipants(
        IReadOnlyList<CombatParticipantState> participants,
        CombatSide side,
        HashSet<string> activeIds,
        IReadOnlyList<CombatLogEntry> log,
        int step)
    {
        for (int index = 0; index < participants.Count; index++)
        {
            CombatParticipantState participant = participants[index];
            activeIds.Add(participant.Id);
            if (!_views.TryGetValue(participant.Id, out CombatantView? view))
            {
                view = _combatantScene.Instantiate<CombatantView>();
                view.Name = SafeNodeName(participant.Id);
                AddChild(view);
                _views.Add(participant.Id, view);
            }

            int baselineOffset = ((index % 3) - 1) * 4;
            Vector2I target = new(
                ProjectPosition(participant.PositionX) - CombatantWidth / 2,
                GroundY() - CombatantHeight + baselineOffset);
            bool animate = _lastPresentedStep >= 0 && step > _lastPresentedStep;
            view.ApplySnapshot(
                participant,
                side,
                index,
                target,
                animate,
                step > _lastPresentedStep ? EventsFor(participant.Id, log, step) : Array.Empty<CombatLogEntry>());
        }
    }

    private int ProjectPosition(double domainX)
    {
        double ratio = Math.Clamp(
            (domainX - _domainMinimumX) / (_domainMaximumX - _domainMinimumX),
            0,
            1);
        int width = Math.Max(1, Mathf.RoundToInt(Size.X) - HorizontalPadding * 2);
        return HorizontalPadding + Mathf.RoundToInt((float)(ratio * width));
    }

    private int GroundY() => Mathf.RoundToInt(Size.Y) * GroundRatioPercent / 100;

    private static IReadOnlyList<CombatLogEntry> EventsFor(
        string participantId,
        IReadOnlyList<CombatLogEntry> log,
        int step)
    {
        var events = new List<CombatLogEntry>();
        foreach (CombatLogEntry entry in log)
        {
            if (entry.Step == step
                && (entry.ActorId == participantId || entry.TargetId == participantId))
            {
                events.Add(entry);
            }
        }
        return events;
    }

    private void RemoveMissing(HashSet<string> activeIds)
    {
        var missing = new List<string>();
        foreach ((string id, CombatantView view) in _views)
        {
            if (activeIds.Contains(id)) continue;
            view.QueueFree();
            missing.Add(id);
        }
        foreach (string id in missing) _views.Remove(id);
    }

    private void DrawLandscapeSilhouette(Vector2I logicalSize, int horizon, Color color)
    {
        for (int x = 20; x < logicalSize.X; x += 112)
        {
            int height = 10 + (x / 112 % 3) * 6;
            DrawRect(new Rect2I(x, horizon - height, 32, height), color.Darkened(0.25f));
            DrawRect(new Rect2I(x + 8, horizon - height - 6, 16, 6), color.Darkened(0.25f));
        }
        DrawLine(
            new Vector2I(0, horizon),
            new Vector2I(logicalSize.X, horizon),
            color,
            width: 2,
            antialiased: false);
    }

    private void DrawSpiritTrailManifestation(int horizon)
    {
        int centerX = ProjectPosition(_objectivePositionX);
        int centerY = horizon - 54;
        Color border = GetThemeColor(_objectiveReached ? "border_ready" : "border_locked");
        var octagon = new Vector2[]
        {
            new(centerX - 12, centerY - 20),
            new(centerX + 12, centerY - 20),
            new(centerX + 20, centerY - 12),
            new(centerX + 20, centerY + 12),
            new(centerX + 12, centerY + 20),
            new(centerX - 12, centerY + 20),
            new(centerX - 20, centerY + 12),
            new(centerX - 20, centerY - 12),
        };
        DrawPolyline(
            [.. octagon, octagon[0]],
            border,
            width: 2,
            antialiased: false);
        DrawLine(
            new Vector2I(centerX - 8, centerY + 7),
            new Vector2I(centerX + 8, centerY - 7),
            border,
            width: 2,
            antialiased: false);
        DrawLine(
            new Vector2I(centerX - 4, centerY - 8),
            new Vector2I(centerX + 7, centerY + 3),
            border,
            width: 2,
            antialiased: false);
    }

    private static CombatParticipantState FixtureParticipant(
        string id,
        string name,
        double positionX,
        CombatFacing facing) => new(
            id, null, name, 100, 100, false, positionX, 48, 12, facing,
            CombatSpatialActivity.Idle, 0, CombatStature.Standard);

    private static string SafeNodeName(string id) => id
        .Replace(".", "_", StringComparison.Ordinal)
        .Replace(":", "_", StringComparison.Ordinal);
}
