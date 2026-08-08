#nullable enable

using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// The contextual action surface: a bottom-centre tray that appears only while a
/// mode needs it, carrying that mode's instruction and its confirm/cancel pair.
/// </summary>
/// <remarks>
/// <para>
/// Replaces the placement chrome the macro view built inline: a raw
/// <c>new Button</c> in a bottom-wide <c>HBoxContainer</c> with **no surface at
/// all**, so the actions floated directly on the world, plus a separate
/// instruction label anchored to the top of the screen. Two nodes, two
/// visibility flags, and an instruction sitting as far from its own buttons as
/// the viewport allows.
/// </para>
/// <para>
/// Here the instruction and the actions are one surface with one
/// <see cref="Godot.CanvasItem.Visible"/>, which is also what makes this reusable:
/// an expedition screen wanting a dispatch/recall tray needs the same shape.
/// </para>
/// <para>
/// It is not a permanent toolbar. Nothing shows it except a mode that has an
/// action to offer, and the city is uncovered the moment that mode ends.
/// </para>
/// </remarks>
[GlobalClass]
public partial class ActionDock : PanelContainer
{
    private Label _instruction = null!;
    private Button _confirmButton = null!;
    private IconButton _cancelButton = null!;

    /// <summary>The affirmative action. The owning mode supplies its meaning.</summary>
    public Button ConfirmButton
    {
        get { EnsureBuilt(); return _confirmButton; }
    }

    /// <summary>The way out. Always present, so a mode is never a trap.</summary>
    public IconButton CancelButton
    {
        get { EnsureBuilt(); return _cancelButton; }
    }

    /// <summary>The line telling the player what this mode expects of them.</summary>
    public string InstructionText
    {
        get { EnsureBuilt(); return _instruction.Text; }
        set { EnsureBuilt(); _instruction.Text = value; }
    }

    public override void _Ready()
    {
        // Above the ambient tint like the rest of the HUD, and above the world so
        // a placement overlay cannot draw over its own controls.
        OverlayLayers.Apply(this, OverlayLayers.PlacementOverlay);
        EnsureBuilt();
        Hide();
    }

    /// <summary>
    /// Builds the tray once, on whichever comes first: this node's own
    /// <c>_Ready</c>, or the first caller to reach for a button.
    /// </summary>
    /// <remarks>
    /// The macro view precedes the dock in <c>CityPrototype.tscn</c> and labels
    /// these actions from its own <c>_Ready</c>, which runs first — so building
    /// only in <c>_Ready</c> handed it null buttons and crashed the boot. Same
    /// shape as <c>CityStatusPanel.EnsureBuilt</c>, and idempotent for the same
    /// reason.
    /// </remarks>
    private void EnsureBuilt()
    {
        if (_instruction is not null) return;

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        AddChild(layout);

        _instruction = new Label
        {
            ThemeTypeVariation = "SectionTitle",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        layout.AddChild(_instruction);

        var actions = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        actions.AddThemeConstantOverride("separation", Tokens.SpacingLoose);
        layout.AddChild(actions);

        _confirmButton = new PrimaryActionButton();
        actions.AddChild(_confirmButton);

        _cancelButton = new IconButton { ThemeTypeVariation = "ButtonText" };
        _cancelButton.CustomMinimumSize = new Vector2(0, Tokens.ControlHeight);
        actions.AddChild(_cancelButton);
    }
}
