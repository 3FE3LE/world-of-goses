#nullable enable
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Reusable header for in-panel navigation: title on the left, close X
/// icon on the right, both readable on the linaje panel stylebox.
///
/// Used by <see cref="ConstructionPanel"/> (and any future modal)
/// inside a <see cref="ModalHost"/>. The close button's
/// <see cref="CloseRequested"/> signal is wired by the parent to the
/// host's <see cref="ModalHost.Close"/> method, completing the
/// "X → close" affordance promised by the stabilisation slice without
/// each modal having to recreate the layout.
/// </summary>
[GlobalClass]
public partial class PanelHeader : HBoxContainer
{
    /// <summary>Emitted when the player clicks the close X.</summary>
    [Signal] public delegate void CloseRequestedEventHandler();

    /// <summary>Title text shown on the left of the row.</summary>
    [Export] public string Title { get; set; } = string.Empty;

    /// <summary>Optional path to a custom icon for the close button; defaults to <see cref="IconPaths.Close"/>.</summary>
    [Export] public string CloseIconPath { get; set; } = IconPaths.Close;

    private Label _title = null!;
    private IconButton _closeButton = null!;

    public override void _Ready()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddThemeConstantOverride("separation", Tokens.SpacingComfortable);

        _title = new Label
        {
            Text = Title,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _title.ThemeTypeVariation = "PanelTitle";
        AddChild(_title);

        _closeButton = new IconButton
        {
            Name = "CloseButton",
            IconPath = CloseIconPath,
            ButtonText = string.Empty,
            CustomMinimumSize = new Vector2(40, 40),
            FocusMode = Control.FocusModeEnum.All,
            TooltipText = UiText.Get("Close (ESC)"),
        };
        _closeButton.Pressed += () => EmitSignal(SignalName.CloseRequested);
        AddChild(_closeButton);
    }

    internal void PressCloseForVisualRegression()
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        _closeButton.EmitSignal(BaseButton.SignalName.Pressed);
    }

    /// <summary>Updates the title text after construction.</summary>
    public void SetTitle(string title)
    {
        Title = title;
        if (_title is not null) _title.Text = title;
    }
}
