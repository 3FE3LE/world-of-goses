#nullable enable

using System;
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Reusable compact, labelled (icon + short label) bottom-centre surface for
/// the city's connected primary navigation. The dock widens at 1280×720 so
/// every destination reads as <c>[ icon ] Construir</c> rather than icon-only,
/// aligning with the bible's "etiquetado" wording and Proposal 06.
/// It owns layout, focus and chrome while the macro view owns what each action
/// opens.
/// </summary>
[GlobalClass]
public partial class PrimaryNavDock : PanelContainer
{
    public static readonly StringName ActionsName = "Actions";

    /// <summary>
    /// Per-button minimum width in the labelled profile. Sized so the
    /// Spanish word <c>Construir</c> and the English word
    /// <c>Construction</c> fit without ellipsis at the default
    /// <c>HudButton</c> content margins. Re-tune via visual review only.
    /// </summary>
    private const float PerButtonWidth = 88f;

    private BoxContainer? _actions;

    public IconButton HeroButton => RequireButton("HeroAccessButton");
    public IconButton ConstructionButton => RequireButton("ConstructionMenuButton");
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
            HeroButton, ConstructionButton, ExpeditionButton,
            PoliciesButton, CitizensButton,
        };

        foreach (IconButton button in buttons)
        {
            button.ShowLabel = true;
            button.ThemeTypeVariation = "HudButton";
            button.FocusMode = FocusModeEnum.All;
            button.ClipText = false;
            button.CustomMinimumSize = new Vector2(PerButtonWidth, Tokens.ControlHeight);
            button.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        }

        HeroButton.SetIconAndLabel(IconPaths.User, UiText.Get("ui.nav.hero_short"));
        ConstructionButton.SetIconAndLabel(
            IconPaths.Building, UiText.Get("ui.nav.build_short"));
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
        // Wiring now delegated to the shared FocusRing helper; this
        // thin wrapper keeps the call site unchanged but routes
        // through the helper so renames and orientation changes
        // (e.g. vertical cycle) don't fork across surfaces. Close #52.
        FocusRing.WireHorizontal(controls);
    }
}
