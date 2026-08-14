using Godot;

namespace WorldofGoses;

/// <summary>
/// Single source of truth for the relative <see cref="NodePath"/>s
/// used by <see cref="CityPrototype"/> and its dependents. Every
/// <c>GetNode&lt;T&gt;</c> call in <c>CityPrototype.cs</c> resolves
/// through one of these constants; moving or renaming a node now
/// requires editing this file rather than grepping 150+ call sites.
///
/// <para>
/// The constants are <see cref="NodePath"/> instances (matching the
/// <see cref="GameUiShell"/> precedent at <c>Ui/GameUiShell.cs</c>
/// lines 14–17) so <c>GetNode&lt;T&gt;(NodePath)</c> resolves
/// directly. <see cref="NodePath"/> has an implicit string
/// conversion which keeps the field initializers readable. The
/// values mirror the controller / modal / status panel / rail /
/// dock / inspector / summary layout that the production scene
/// composes.
/// </para>
///
/// <para>
/// Per the "no magic strings" rule at
/// <c>docs/engineering/conventions.md</c> §5 line 134–135, a literal
/// <see cref="NodePath"/> in a non-allowlisted location is a
/// regression; the guard test
/// <c>CityNodePathConstantsTests</c> enforces that.
/// </para>
/// </summary>
public static class CityNodePaths
{
    public static readonly NodePath Controller = "CityWorldController";
    public static readonly NodePath OnboardingView = "OnboardingView";
    public static readonly NodePath StatusPanel = "GameUiShell/CityStatusPanel";
    public static readonly NodePath PauseMenu = "PauseMenu";
    public static readonly NodePath LocaleManager = "/root/LocaleManager";
    public static readonly NodePath ScreenContent = "GameUiShell/ScreenContent";

    // Children of GameUiShell/ScreenContent.
    public static readonly NodePath MacroLiveView = "GameUiShell/ScreenContent/MacroStreetLiveView";
    public static readonly NodePath CitySummaryPanel = "GameUiShell/ScreenContent/CitySummaryHost/CitySummaryPanel";
    public static readonly NodePath ContextInspector = "GameUiShell/ScreenContent/ContextInspector";
    public static readonly NodePath ActionDock = "GameUiShell/ScreenContent/ActionDock";
    public static readonly NodePath ModalHost = "GameUiShell/ScreenContent/ModalHost";
    public static readonly NodePath MigrantPanel = "GameUiShell/ScreenContent/MigrantPanel";
    public static readonly NodePath ExpeditionRail = "GameUiShell/ScreenContent/ExpeditionRailHost/ExpeditionRail";
    public static readonly NodePath TimeOfDayFilter = "GameUiShell/ScreenContent/TimeOfDayFilter";
    public static readonly NodePath PrimaryNavDock = "GameUiShell/ScreenContent/PrimaryNavDock";
    public static readonly NodePath PoliciesPanel = "GameUiShell/ScreenContent/PoliciesPanel";
    public static readonly NodePath CenterExpeditionPanel = "GameUiShell/ScreenContent/Center/ExpeditionPanel";
    public static readonly NodePath BuildingInspector = "GameUiShell/ScreenContent/BuildingInspector";
    public static readonly NodePath ConstructionPanel = "GameUiShell/ScreenContent/Center/ConstructionPanel";
    public static readonly NodePath HeroProfileView = "GameUiShell/ScreenContent/HeroProfileView";
    public static readonly NodePath ExpeditionLiveView = "GameUiShell/ScreenContent/ExpeditionLiveView";
}
