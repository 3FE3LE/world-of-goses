using Godot;

namespace WorldofGoses;

/// <summary>
/// Permanent top-right button that opens the hero profile. Lives as
/// a sibling of the macro city, <c>BuildingDetailView</c>, and
/// <c>CityStatusPanel</c> under <c>CityPrototype</c>, so it remains
/// visible across every view — the macro view, the building detail,
/// and any future view. Clicking it routes through the controller,
/// which emits <c>SelectionChanged(HeroProfile)</c>; the hero
/// profile view listens for that signal and shows itself.
///
/// The button is hidden during onboarding (no hero yet) because the
/// hero profile button would target no existing citizen. The
/// controller exposes <c>NeedsOnboarding()</c> for that check.
///
/// Inherits from <see cref="IconButton"/> for two reasons:
/// - the persistent instance in <c>CityPrototype.tscn</c> needs the
///   Pixelify tooltip override that <see cref="IconButton"/> provides,
/// - the inner Label needs the same ButtonText (Jersey 10) variation
///   so it does not fall back to the engine default font for typography
///   for the icon+label row.
/// </summary>
[GlobalClass]
public partial class HeroAccessButton : IconButton
{
    [Export] public NodePath ControllerPath { get; set; } = "../../../CityWorldController";

    private CityWorldController _controller = null!;
    private CityWorldController.Selection _selection = CityWorldController.Selection.MacroView;

    public override void _Ready()
    {
        base._Ready();
        _controller = GetNodeOrNull<CityWorldController>(ControllerPath);
        if (_controller is null)
        {
            GD.PushError($"HeroAccessButton: cannot resolve controller at '{ControllerPath}'.");
            return;
        }

        Pressed += OnPressed;
        _controller.HeroCreated += OnHeroStateChanged;
        _controller.SelectionChanged += OnSelectionChanged;
        ApplyVisibility();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (_controller is null) return;
        _controller.HeroCreated -= OnHeroStateChanged;
        _controller.SelectionChanged -= OnSelectionChanged;
    }

    private void OnPressed() => _controller.SelectHero();

    private void OnHeroStateChanged(int _) => ApplyVisibility();

    private void OnSelectionChanged(int selectionState)
    {
        _selection = (CityWorldController.Selection)selectionState;
        ApplyVisibility();
    }

    private void ApplyVisibility()
    {
        if (_controller is null) return;
        bool hasHero = _controller.HasHero();
        bool inOnboarding = _controller.NeedsOnboarding();
        Visible = hasHero
            && !inOnboarding
            && _selection == CityWorldController.Selection.MacroView;
    }
}
