#nullable enable
using System;
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Defines the two stable regions of the in-game UI: a persistent status bar
/// and an expanding screen host. Full-screen onboarding and modal overlays are
/// intentionally composed outside this shell.
/// </summary>
public partial class GameUiShell : VBoxContainer
{
    public static readonly StringName StatusSlotName = "CityStatusPanel";
    public static readonly StringName ScreenSlotName = "ScreenContent";
    private static readonly NodePath StatusSlotPath = new(StatusSlotName);
    private static readonly NodePath ScreenSlotPath = new(ScreenSlotName);

    public PanelContainer StatusSlot { get; private set; } = null!;
    public Control ScreenSlot { get; private set; } = null!;

    public override void _Ready()
    {
        StatusSlot = GetNodeOrNull<PanelContainer>(StatusSlotPath)
            ?? throw new InvalidOperationException(
                $"{nameof(GameUiShell)} requires a direct {nameof(PanelContainer)} child named {StatusSlotName}.");
        ScreenSlot = GetNodeOrNull<Control>(ScreenSlotPath)
            ?? throw new InvalidOperationException(
                $"{nameof(GameUiShell)} requires a direct {nameof(Control)} child named {ScreenSlotName}.");

        if (StatusSlot.GetIndex() >= ScreenSlot.GetIndex())
        {
            throw new InvalidOperationException(
                $"{StatusSlotName} must precede {ScreenSlotName} so the HUD reserves space above screen content.");
        }

        ScreenSlot.SizeFlagsVertical = SizeFlags.ExpandFill;
    }
}
