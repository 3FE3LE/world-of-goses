#nullable enable
using System;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses.Ui;

/// <summary>Reusable at-a-glance expedition card; planning remains in ExpeditionPanel.</summary>
[GlobalClass]
public partial class ExpeditionCompactCard : PanelContainer
{
    public event Action<ExpeditionId>? DetailsRequested;
    public event Action<ExpeditionId>? CancelRequested;

    private readonly ExpeditionRailSnapshot.Item _item;
    public IconButton DetailsButton { get; }
    public IconButton? CancelButton { get; }

    public ExpeditionCompactCard(ExpeditionRailSnapshot.Item item, int currentTick)
    {
        _item = item;
        string localizedDisplayName = UiText.Get(item.DisplayName);
        ThemeTypeVariation = "HudCard";
        MouseFilter = MouseFilterEnum.Stop;

        var content = new VBoxContainer { MouseFilter = MouseFilterEnum.Pass };
        content.AddThemeConstantOverride("separation", Tokens.SpacingTight);
        AddChild(content);

        // Identity row: leading glyph (resource or backpack fallback) +
        // destination name as the action surface. The card stays click-
        // through to the planning panel; the action button is the only
        // interactive cell, so the rest of the card is informational.
        var identity = new HBoxContainer { MouseFilter = MouseFilterEnum.Pass };
        identity.AddThemeConstantOverride("separation", Tokens.SpacingTight);
        content.AddChild(identity);

        var iconCell = new MarginContainer
        {
            CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.HudRowHeight),
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        iconCell.AddThemeConstantOverride("margin_top", 1);
        identity.AddChild(iconCell);
        iconCell.AddChild(ExpeditionIcon.Leading(item));

        DetailsButton = new IconButton
        {
            ButtonText = localizedDisplayName,
            ShowLabel = true,
            ThemeTypeVariation = "HudButton",
            TooltipText = UiText.Format(
                "ui.expedition_rail.members_tooltip",
                item.MemberCount,
                item.MemberNames.Count == 0
                    ? UiText.Get("ui.expedition_rail.members_unknown")
                    : string.Join(", ", item.MemberNames)),
            FocusMode = FocusModeEnum.All,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ClipText = true,
        };
        DetailsButton.Pressed += OnDetailsPressed;
        identity.AddChild(DetailsButton);

        var facts = new HBoxContainer { MouseFilter = MouseFilterEnum.Pass };
        facts.AddThemeConstantOverride("separation", Tokens.SpacingComfortable);
        facts.AddChild(StatChip.HudIconValue(
            IconPaths.Clock,
            SimulationTimeText.FormatDurationLocalized(item.RemainingTicks(currentTick))));
        facts.AddChild(StatChip.HudIconValue(item.SupplyResource, item.SupplyAmount.ToString()));
        content.AddChild(facts);

        content.AddChild(new HudStateBadge(
            HudStateBadge.IconFor(item.Phase),
            PhaseText(item.Phase)));
        content.AddChild(new HudProgressBar(item.Progress(currentTick), showPercent: true));

        if (item.CanCancel)
        {
            CancelButton = new IconButton
            {
                IconPath = IconPaths.Close,
                ButtonText = UiText.Get("ui.expedition_rail.cancel"),
                ShowLabel = true,
                ThemeTypeVariation = "HudButtonDanger",
                TooltipText = UiText.Get("ui.expedition_rail.cancel_tooltip"),
                FocusMode = FocusModeEnum.All,
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            };
            CancelButton.Pressed += OnCancelPressed;
            content.AddChild(CancelButton);
        }
    }

    public override void _ExitTree()
    {
        DetailsButton.Pressed -= OnDetailsPressed;
        if (CancelButton is not null) CancelButton.Pressed -= OnCancelPressed;
    }

    private void OnDetailsPressed() => DetailsRequested?.Invoke(_item.Id);
    private void OnCancelPressed() => CancelRequested?.Invoke(_item.Id);

    public static string PhaseText(ExpeditionPhase phase) => UiText.Get(phase switch
    {
        ExpeditionPhase.Outbound => "ui.expedition_rail.phase.outbound",
        ExpeditionPhase.Encounter => "ui.expedition_rail.phase.encounter",
        ExpeditionPhase.Objective => "ui.expedition_rail.phase.objective",
        ExpeditionPhase.Returning => "ui.expedition_rail.phase.returning",
        ExpeditionPhase.Retreating => "ui.expedition_rail.phase.retreating",
        _ => "ui.expedition_rail.phase.resolved",
    });
}
