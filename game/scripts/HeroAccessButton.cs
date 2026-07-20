using Godot;

namespace WorldofGoses;

/// <summary>
/// Permanent top-right button that opens the hero profile. Lives as
/// a sibling of <c>CityMacroView</c>, <c>BuildingDetailView</c>, and
/// <c>CityStatusPanel</c> under <c>CityPrototype</c>, so it remains
/// visible across every view — the macro view, the building detail,
/// and any future view. Clicking it routes through the controller,
/// which emits <c>SelectionChanged(HeroProfile)</c>; the hero
/// profile view listens for that signal and shows itself.
///
/// The button is hidden during onboarding (no hero yet) because the
/// hero profile button would target no existing citizen. The
/// controller exposes <c>NeedsOnboarding()</c> for that check.
/// </summary>
public partial class HeroAccessButton : Button
{
    [Export] public NodePath ControllerPath { get; set; } = "../CityWorldController";

    private CityWorldController _controller = null!;

    public override void _Ready()
    {
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
        if (_controller is null) return;
        _controller.HeroCreated -= OnHeroStateChanged;
        _controller.SelectionChanged -= OnSelectionChanged;
    }

    private void OnPressed() => _controller.SelectHero();

    private void OnHeroStateChanged(int _) => ApplyVisibility();

    private void OnSelectionChanged(int selectionState) => ApplyVisibility();

    private void ApplyVisibility()
    {
        if (_controller is null) return;
        bool hasHero = _controller.HeroOrNull() is not null;
        bool inOnboarding = _controller.NeedsOnboarding();
        Visible = hasHero && !inOnboarding;
    }
}