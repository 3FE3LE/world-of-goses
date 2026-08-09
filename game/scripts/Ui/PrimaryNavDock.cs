#nullable enable

using System;
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Reusable compact, icon-only bottom-centre surface for the city's connected
/// primary navigation.
/// It owns layout, focus and chrome while the macro view owns what each action
/// opens.
/// </summary>
[GlobalClass]
public partial class PrimaryNavDock : PanelContainer
{
    public static readonly StringName ActionsName = "Actions";

    private BoxContainer? _actions;

    public IconButton HeroButton => RequireButton("HeroAccessButton");
    public IconButton ConstructionButton => RequireButton("ConstructionMenuButton");
    public IconButton MenuButton => RequireButton("GameMenuButton");
    public IconButton ExpeditionButton => RequireButton("ExpeditionMenuButton");
    public IconButton PoliciesButton => RequireButton("PoliciesButton");
    public IconButton CitizensButton => RequireButton("CitizensButton");

    public override void _Ready()
    {
        OverlayLayers.Apply(this, OverlayLayers.Hud);
        ThemeTypeVariation = "HudDock";
        MouseFilter = MouseFilterEnum.Stop;
        ApplyLayout();
    }

    public void GrabDefaultFocus() => HeroButton.GrabFocus();

    private IconButton RequireButton(string name)
    {
        _actions ??= GetNodeOrNull<BoxContainer>(new NodePath(ActionsName))
            ?? throw new InvalidOperationException(
                $"{nameof(PrimaryNavDock)} requires a {nameof(BoxContainer)} child named {ActionsName}.");

        return _actions.GetNodeOrNull<IconButton>(new NodePath(name))
            ?? throw new InvalidOperationException(
                $"{nameof(PrimaryNavDock)} requires an {nameof(IconButton)} named {name} under {ActionsName}.");
    }

    private void ApplyLayout()
    {
        IconButton[] buttons =
        {
            HeroButton, ConstructionButton, MenuButton, ExpeditionButton,
            PoliciesButton, CitizensButton,
        };

        foreach (IconButton button in buttons)
        {
            button.ShowLabel = false;
            button.ThemeTypeVariation = "HudButton";
            button.FocusMode = FocusModeEnum.All;
            button.ClipText = false;
            button.CustomMinimumSize = new Vector2(Tokens.ControlHeight, Tokens.ControlHeight);
            button.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        }

        HeroButton.SetIconAndLabel(IconPaths.User, UiText.Get("ui.nav.hero_short"));
        ConstructionButton.SetIconAndLabel(
            IconPaths.Building, UiText.Get("ui.nav.build_short"));
        MenuButton.SetIconAndLabel(IconPaths.Menu, UiText.Get("ui.nav.menu_short"));
        ExpeditionButton.SetIconAndLabel(
            IconPaths.Backpack, UiText.Get("ui.nav.expedition_short"));
        PoliciesButton.SetIconAndLabel(
            IconPaths.ClipboardNote, UiText.Get("ui.nav.policies_short"));
        CitizensButton.SetIconAndLabel(
            IconPaths.Users, UiText.Get("ui.nav.citizens_short"));

        WireHorizontalFocus(buttons);
    }

    private static void WireHorizontalFocus(Control[] controls)
    {
        for (int i = 0; i < controls.Length; i++)
        {
            Control current = controls[i];
            Control previous = controls[(i + controls.Length - 1) % controls.Length];
            Control next = controls[(i + 1) % controls.Length];
            current.FocusNeighborLeft = current.GetPathTo(previous);
            current.FocusNeighborRight = current.GetPathTo(next);
            current.FocusPrevious = current.GetPathTo(previous);
            current.FocusNext = current.GetPathTo(next);
        }
    }
}
