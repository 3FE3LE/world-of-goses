#nullable enable

using System;
using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Developer-only visual fixture for the reusable ExpeditionLiveView building
/// blocks. It contains no combat, expedition controller or domain state.
/// </summary>
public partial class ExpeditionHudComponentShowcase : Control
{
    private const string FixturePrefix = "--wog-visual-fixture=";
    private const string SkillSlotScenePath =
        "res://scenes/Components/OctagonalSkillSlot.tscn";
    private const string SquadSlotScenePath =
        "res://scenes/Components/ExpeditionSquadSlot.tscn";

    private ExpeditionSkillStrip _skillStrip = null!;

    public override void _Ready()
    {
        ExpeditionSquadStrip squadStrip = GetNode<ExpeditionSquadStrip>(
            "SafeArea/Layout/Columns/Primary/SquadStrip");
        _skillStrip = GetNode<ExpeditionSkillStrip>(
            "SafeArea/Layout/Columns/Primary/SkillStrip");

        Texture2D? founderPortrait = ResourceLoader.Load<Texture2D>(IconPaths.User);
        squadStrip.ConfigureFounderFixture(
            founderPortrait,
            "Founder",
            hpRatio: 0.82,
            secondaryName: "ST",
            secondaryRatio: 0.64);

        BuildSkillStateGallery();
        BuildSquadStateGallery(founderPortrait);
        _skillStrip.GrabDefaultFocus();

        string fixture = SelectedFixture();
        if (!string.IsNullOrEmpty(fixture))
        {
            CallDeferred(MethodName.ApplyFocusFixture, fixture);
        }
    }

    private void BuildSkillStateGallery()
    {
        var gallery = GetNode<HBoxContainer>(
            "SafeArea/Layout/Columns/States/SkillStates");
        PackedScene scene = ResourceLoader.Load<PackedScene>(SkillSlotScenePath);
        Texture2D? skillIcon = ResourceLoader.Load<Texture2D>(IconPaths.Fire);
        OctagonalSkillSlot.SlotState[] states =
        {
            OctagonalSkillSlot.SlotState.Empty,
            OctagonalSkillSlot.SlotState.Locked,
            OctagonalSkillSlot.SlotState.Ready,
            OctagonalSkillSlot.SlotState.Cooldown,
            OctagonalSkillSlot.SlotState.Disabled,
        };

        for (int i = 0; i < states.Length; i++)
        {
            OctagonalSkillSlot slot = scene.Instantiate<OctagonalSkillSlot>();
            slot.Configure(
                Math.Min(i + 1, 4),
                states[i],
                skillIcon,
                cooldownRemaining: states[i] == OctagonalSkillSlot.SlotState.Cooldown ? 3.4 : 0,
                cooldownDuration: states[i] == OctagonalSkillSlot.SlotState.Cooldown ? 8 : 0);
            gallery.AddChild(slot);
        }
    }

    private void BuildSquadStateGallery(Texture2D? portrait)
    {
        var gallery = GetNode<HBoxContainer>(
            "SafeArea/Layout/Columns/States/SquadStates");
        PackedScene scene = ResourceLoader.Load<PackedScene>(SquadSlotScenePath);

        ExpeditionSquadSlot active = scene.Instantiate<ExpeditionSquadSlot>();
        active.Configure(
            1,
            ExpeditionSquadSlot.SlotState.Active,
            portrait,
            "Founder",
            hpRatio: 0.38,
            secondaryName: "ST",
            secondaryRatio: 0.24,
            criticalState: "LOW HP");
        gallery.AddChild(active);

        ExpeditionSquadSlot empty = scene.Instantiate<ExpeditionSquadSlot>();
        empty.Configure(2, ExpeditionSquadSlot.SlotState.Empty);
        gallery.AddChild(empty);

        ExpeditionSquadSlot locked = scene.Instantiate<ExpeditionSquadSlot>();
        locked.Configure(3, ExpeditionSquadSlot.SlotState.Locked);
        gallery.AddChild(locked);
    }

    private void ApplyFocusFixture(string fixture)
    {
        _skillStrip.GrabDefaultFocus();
        if (fixture == "expedition-components-focus-keyboard")
        {
            CallDeferred(MethodName.SendKeyboardRight);
        }
        else if (fixture == "expedition-components-focus-gamepad")
        {
            CallDeferred(MethodName.SendGamepadRight);
        }
    }

    private static void SendKeyboardRight()
    {
        Input.ParseInputEvent(new InputEventKey
        {
            Keycode = Key.Right,
            PhysicalKeycode = Key.Right,
            Pressed = true,
        });
        Input.ParseInputEvent(new InputEventKey
        {
            Keycode = Key.Right,
            PhysicalKeycode = Key.Right,
            Pressed = false,
        });
    }

    private static void SendGamepadRight()
    {
        Input.ParseInputEvent(new InputEventJoypadButton
        {
            ButtonIndex = JoyButton.DpadRight,
            Pressed = true,
        });
        Input.ParseInputEvent(new InputEventJoypadButton
        {
            ButtonIndex = JoyButton.DpadRight,
            Pressed = false,
        });
    }

    private static string SelectedFixture()
    {
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (argument.StartsWith(FixturePrefix, StringComparison.Ordinal))
            {
                return argument[FixturePrefix.Length..];
            }
        }
        return string.Empty;
    }
}
