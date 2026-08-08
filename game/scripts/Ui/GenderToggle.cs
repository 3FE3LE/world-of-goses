#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses.Ui;

/// <summary>
/// Compact two-option control for the founder's body presentation.
///
/// <para>
/// It exists because the onboarding used to build this from
/// <see cref="StandardButtons.ChoiceButton"/>, which carries
/// <c>SizeFlagsHorizontal = ExpandFill</c>. Nested inside an expanding
/// column that resolved to 1032 px, the two options ended up roughly 510 px
/// wide each — about 81 % of the viewport for a binary choice. This control
/// is <c>ShrinkCenter</c> and its options are fixed width, so the pair is
/// 304 px regardless of the room available.
/// </para>
///
/// <para>
/// The two options share a <see cref="ButtonGroup"/>, so they announce as a
/// radio pair and keyboard/gamepad traversal is correct, and they carry
/// <see cref="OnboardingChoiceButton"/>'s non-colour selection glyph.
/// </para>
/// </summary>
[GlobalClass]
public partial class GenderToggle : HBoxContainer
{
    private const int OptionWidth = 148;

    private readonly Dictionary<GenderId, OnboardingChoiceButton> _options = new();
    private GenderId? _selected;

    /// <summary>
    /// Raised only on a real player change. The payload is the
    /// <see cref="GenderId"/> as an integer so the signal is usable from
    /// GDScript and the editor; C# consumers should prefer
    /// <see cref="Selected"/> inside the handler.
    /// </summary>
    [Signal]
    public delegate void GenderChangedEventHandler(int gender);

    /// <summary>
    /// The active option, or <c>null</c> while the player has not chosen.
    /// Assigning it updates the visuals without raising
    /// <see cref="GenderChangedEventHandler"/>.
    /// </summary>
    public GenderId? Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            RefreshSelection();
        }
    }

    /// <summary>
    /// Builds the pair. <paramref name="label"/> resolves the displayed text
    /// for an option; the caller owns translation so this widget stays free
    /// of any assumption about which catalog key names a body presentation.
    /// </summary>
    public void Configure(Func<GenderId, string> label, string tooltip)
    {
        foreach (Node child in GetChildren()) child.QueueFree();
        _options.Clear();

        Alignment = AlignmentMode.Center;
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        AddThemeConstantOverride("separation", Tokens.SpacingBase);

        var group = new ButtonGroup();
        foreach (GenderId id in new[] { GenderId.Feminine, GenderId.Masculine })
        {
            var option = new OnboardingChoiceButton
            {
                Text = label(id),
                TooltipText = tooltip,
                ButtonGroup = group,
                Alignment = HorizontalAlignment.Center,
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                CustomMinimumSize =
                    new Vector2(OptionWidth, OnboardingChoiceButton.DefaultHeight),
            };
            GenderId chosen = id;
            option.Pressed += () => OnOptionPressed(chosen);
            AddChild(option);
            _options[id] = option;
        }
        RefreshSelection();
    }

    private void OnOptionPressed(GenderId id)
    {
        if (_selected == id)
        {
            // A ButtonGroup keeps the pressed option pressed when re-clicked;
            // restate the visuals so the glyph cannot drift, but do not tell
            // the view that anything changed.
            RefreshSelection();
            return;
        }
        _selected = id;
        RefreshSelection();
        EmitSignal(SignalName.GenderChanged, (int)id);
    }

    private void RefreshSelection()
    {
        foreach ((GenderId id, OnboardingChoiceButton option) in _options)
        {
            option.Selected = _selected == id;
        }
    }
}
