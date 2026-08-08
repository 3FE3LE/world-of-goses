#nullable enable
using System.Collections.Generic;
using Godot;
using WorldofGoses.Ui;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using WorldofGoses.Presentation;

namespace WorldofGoses;

/// <summary>
/// Developer-facing view of one combat expedition. It runs the domain, prints the
/// domain's own telemetry, and displays a per-member summary — it computes no
/// combat value itself and never writes to a citizen except through
/// <see cref="CombatExpeditionService"/>.
///
/// <para>
/// Placeholder presentation on purpose: legibility of states, health, techniques
/// and the physical/elemental split matters here, final art does not. The panel is
/// built in code so the slice needs no new .tscn.
/// </para>
/// </summary>
[GlobalClass]
public partial class CombatDebugPanel : Control
{
    private RichTextLabel _output = null!;
    private OptionButton _route = null!;
    private Button _run = null!;
    private Button _close = null!;
    private CityWorldController? _controller;

    [Export] public NodePath ControllerPath { get; set; } = "../../../CityWorldController";

    public override void _Ready()
    {
        _controller = GetNodeOrNull<CityWorldController>(ControllerPath);
        Visible = false;
        SetAnchorsPreset(LayoutPreset.FullRect);
        // The panel may be hosted under a non-Control parent in debug flows, where
        // anchors alone would collapse it to its content size.
        CustomMinimumSize = new Vector2(900, 620);
        MouseFilter = MouseFilterEnum.Stop;

        var surface = new PanelContainer();
        surface.SetAnchorsPreset(LayoutPreset.FullRect);
        surface.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.04f, 0.05f, 0.09f, 0.98f),
            BorderColor = new Color(0.88f, 0.7f, 0.25f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
        });
        AddChild(surface);
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "top", "right", "bottom" })
        {
            margin.AddThemeConstantOverride($"margin_{side}", 16);
        }
        surface.AddChild(margin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        margin.AddChild(layout);

        var title = new Label { Text = "Combat expedition — debug", ThemeTypeVariation = "ScreenTitle" };
        layout.AddChild(title);

        var controls = new HBoxContainer();
        controls.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        layout.AddChild(controls);

        _route = new OptionButton();
        _route.AddItem(ExpeditionRoute.SafeRoute.ToString(), (int)ExpeditionRoute.SafeRoute);
        _route.AddItem(ExpeditionRoute.ShortRoute.ToString(), (int)ExpeditionRoute.ShortRoute);
        controls.AddChild(_route);

        _run = new Button { Text = "Run expedition", ThemeTypeVariation = "ButtonPrimary" };
        _run.Pressed += OnRunPressed;
        controls.AddChild(_run);

        _close = new Button { Text = "Close", ThemeTypeVariation = "ButtonText" };
        _close.Pressed += () => Visible = false;
        controls.AddChild(_close);

        _output = new RichTextLabel
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ScrollActive = true,
            SelectionEnabled = true,
        };
        layout.AddChild(_output);
    }

    /// <summary>Opens the panel. Used by the visual-regression fixture and by debug input.</summary>
    public void Open()
    {
        Visible = true;
        _output.Text = "Pick a route and run. Members need an equipped weapon.";
    }

    /// <summary>Drives the same path the Run button does, for a capture.</summary>
    public void RunForVisualRegression() => OnRunPressed();

    private void OnRunPressed()
    {
        if (_controller is null)
        {
            _output.Text = "No CityWorldController resolved.";
            return;
        }

        List<Citizen> party = SelectParty(_controller.World);
        if (party.Count == 0)
        {
            _output.Text =
                "No citizen carries a weapon yet. Equipment has no production source in this "
                + "slice, so the debug run equips the party itself when it can.";
            return;
        }

        var route = (ExpeditionRoute)_route.GetSelectedId();
        var service = new CombatExpeditionService();
        var plans = new Dictionary<string, CombatantPlan>();
        foreach (Citizen citizen in party)
        {
            plans[$"citizen.{citizen.Id.Value}"] = new CombatantPlan(
                Position: 0,
                TechniquePriority: System.Array.Empty<string>(),
                PreferredTargetId: null,
                RetreatWhenBelowThreshold: true);
        }

        var plan = new ExpeditionRunPlan(
            party.ConvertAll(citizen => citizen.Id),
            plans,
            route,
            Supplies: 6,
            // Stable seed so a debug run is reproducible and comparable.
            Seed: (ulong)(_controller.World.CurrentTick + 1));

        ExpeditionRunResult result = service.Run(party, plan);
        service.ApplyResult(party, result);

        var text = new System.Text.StringBuilder();
        text.AppendLine(CombatTelemetryText.Describe(result));
        for (int index = 0; index < result.CombatLogs.Count; index++)
        {
            text.AppendLine($"=== Encounter {index + 1} ===");
            text.AppendLine(CombatTelemetryText.DescribeEncounter(result.CombatLogs[index]));
        }
        _output.Text = text.ToString();
    }

    /// <summary>
    /// Takes up to three citizens and, because there is no equipment economy yet,
    /// arms any unarmed member with a placeholder weapon so the slice is runnable.
    /// This is debug-only: the real preparation screen belongs to a later slice.
    /// </summary>
    private static List<Citizen> SelectParty(CityWorld world)
    {
        var party = new List<Citizen>();
        WeaponFamily[] families =
        {
            WeaponFamily.Spear,
            WeaponFamily.Mace,
            WeaponFamily.Orb,
        };
        foreach (Citizen citizen in world.Citizens.Values)
        {
            if (party.Count == 3) break;
            if (citizen.EquipmentLoadout.Weapon is null)
            {
                citizen.SetEquipmentLoadout(new EquipmentLoadout(
                    new WeaponChannelProfile(families[party.Count], 1.10, 1.05),
                    GearSupportProfile.None,
                    GearSupportProfile.None,
                    GearSupportProfile.None,
                    GearSupportProfile.None,
                    GearSupportProfile.None));
            }
            party.Add(citizen);
        }
        return party;
    }
}
