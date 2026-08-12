#nullable enable
using System;
using System.Linq;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;
using WorldofGoses.Prototypes;

namespace WorldofGoses;

/// <summary>
/// Presentation host for the authored first night
/// (<c>docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md</c>). Owns the
/// non-modal <see cref="FirstNightSpeechBubble"/> and the
/// <see cref="FireSpiritVisual"/>, both rendered at
/// <see cref="OverlayLayers.WorldDialogue"/> so they remain above the
/// day/night tint while every HUD surface and modal can occlude them.
///
/// <para>
/// Subscribes to <see cref="CityWorldController.FirstNightStageChanged"/>
/// and projects the current <see cref="FirstNightStage"/> into
/// show/hide + position decisions for the strip and the visual.
/// The scene is created once by <see cref="CityPrototype"/> in its
/// <c>_Ready</c> and lives for the lifetime of the prototype.
/// </para>
///
/// <para>
/// The scene resolves body text via <see cref="UiText.Get"/> so the
/// player reads the night in their active locale. The localisation
/// happens here — not in <see cref="FirstNightSpeechBubble"/>
/// itself — so the balloon stays a pure layout primitive and tests
/// for it stay Godot-free.
/// </para>
/// </summary>
public partial class FirstNightScene : Node
{
    // FirstNightScene is a direct child of CityPrototype (created in
    // CityPrototype._Ready). The controller and the macro view are
    // siblings under CityPrototype, so the relative path is `../`.
    [Export] public NodePath ControllerPath { get; set; } = "../CityWorldController";

    /// <summary>
    /// Path to the macro street view. The typed
    /// <see cref="Prototypes.MacroStreetLiveView.GetFoundingArrivalGlobalPosition"/>
    /// and <see cref="Prototypes.MacroStreetLiveView.GetBuildingGlobalPosition"/>
    /// methods resolve the founder and campfire anchors, and the
    /// <c>WorldDialogueAnchorsChanged</c> signal notifies when the
    /// projection moves. Optional: when the macro view is absent
    /// (tests, editor-only fixtures) the scene falls back to a fixed
    /// viewport-centred placeholder.
    /// </summary>
    [Export] public NodePath MacroViewPath { get; set; } = "../GameUiShell/ScreenContent/MacroStreetLiveView";

    /// <summary>
    /// The shared modal host. Construction, expeditions, policies and the
    /// citizens roster all open through it, and none of them changes
    /// <c>CityWorldController.Selection</c> — so watching the selection alone
    /// left the balloon floating over an open panel.
    /// </summary>
    [Export] public NodePath ModalHostPath { get; set; } = "../GameUiShell/ScreenContent/ModalHost";

    private CityWorldController? _controller;
    private MacroStreetLiveView? _macroView;
    private FirstNightSpeechBubble _bubble = null!;
    private FireSpiritVisual _spirit = null!;
    private FirstNightEmbers _embers = null!;

    /// <summary>
    /// Approximate screen position of the founder. Refreshed by
    /// <see cref="RefreshAnchorsFromMacro"/> whenever the macro view
    /// emits <c>WorldDialogueAnchorsChanged</c>; the spirit visual
    /// reads it when it needs to hover beside the founder before the
    /// campfire is built.
    /// </summary>
    private Vector2 _founderScreenPosition = new(640, 360);

    /// <summary>
    /// Approximate screen position of the campfire once it has been
    /// built. Refreshed by the same handler; the spirit and embers
    /// settle into it once the founding module completes.
    /// </summary>
    private Vector2 _campfireScreenPosition = new(640, 380);

    /// <summary>
    /// Whether <see cref="_campfireScreenPosition"/> came from a real
    /// structure this frame. False while the founding site is still a project.
    /// </summary>
    private bool _hasCampfireAnchor;

    private CityWorldController.Selection _selection = CityWorldController.Selection.MacroView;
    private ModalHost? _modalHost;

    public override void _Ready()
    {
        // No CanvasLayer. OverlayLayers is a ZIndex catalogue, and a
        // CanvasLayer sits on a different axis that outranks every ZIndex in
        // the project: hosting the night on `CanvasLayer.Layer = 50` drew the
        // strip and the spirit over the onboarding (ZIndex 80), the pause menu
        // (100) and the Notifier, which is the opposite of what
        // `OverlayLayers.WorldDialogue` promises. The surfaces are canvas roots
        // here — FirstNightScene is a plain Node — so their ZIndex is
        // comparable with every other overlay, exactly like
        // AstralOnboardingView.
        _spirit = new FireSpiritVisual { ZIndex = OverlayLayers.WorldDialogue };
        AddChild(_spirit);

        _bubble = new FirstNightSpeechBubble();
        AddChild(_bubble);
        _bubble.Confirmed += OnStripFollowPressed;

        _embers = new FirstNightEmbers { ZIndex = OverlayLayers.WorldDialogue };
        AddChild(_embers);

        _controller = GetNodeOrNull<CityWorldController>(ControllerPath);
        if (_controller is null)
        {
            GD.PushWarning(
                "FirstNightScene could not resolve CityWorldController at " +
                $"{ControllerPath}; the night will render but stay inert.");
            return;
        }
        _controller.FirstNightStageChanged += OnFirstNightStageChanged;
        _controller.SelectionChanged += OnSelectionChanged;

        _macroView = GetNodeOrNull<MacroStreetLiveView>(MacroViewPath);
        if (_macroView is not null)
        {
            _macroView.WorldDialogueAnchorsChanged += RefreshAnchorsFromMacro;
        }

        _modalHost = GetNodeOrNull<ModalHost>(ModalHostPath);
        if (_modalHost is not null)
        {
            _modalHost.Opened += ProjectCurrentStage;
            _modalHost.Closed += ProjectCurrentStage;
        }
        // Project the loaded stage on first frame so a save restored
        // mid-night shows its dialogue immediately, without waiting for
        // the next tick.
        RefreshAnchorsFromMacro();
        ProjectCurrentStage();
    }

    /// <summary>
    /// Architecture Hardening A9 removed the per-frame anchor
    /// polling. The spirit and bubble now follow
    /// <see cref="RefreshAnchorsFromMacro"/>, which only fires when
    /// the macro view's projection actually moves (camera pan, zoom,
    /// follow toggle). When nothing is visible there is no work to
    /// do — the visuals vanish and stay vanished until the next
    /// stage transition respawns them.
    ///
    /// <para>
    /// Visual flicker and animation stay where they were: the
    /// <see cref="FireSpiritVisual"/> owns its 12 Hz flicker inside its
    /// own <c>_Process</c>, and the <see cref="FirstNightSpeechBubble"/>
    /// is layout-only. Nothing at this scene layer needs a per-frame
    /// tick for world anchors anymore, so <c>_Process</c> is gone.</para>
    /// </summary>

    /// <summary>
    /// Where the spirit currently belongs: beside the founder until the
    /// campfire exists, over the flame afterwards.
    /// </summary>
    /// <summary>
    /// What the spirit is asking for at a build stage, with the quantities
    /// read from <see cref="FoundingSiteRules.InputsFor"/> at display time so
    /// a recipe change can never leave the tutorial lying to the player.
    /// </summary>
    private static string DescribeModuleDirective(FirstNightStage stage)
    {
        FoundingSiteModule module = FirstNightRules.ModuleFor(stage);
        var parts = new System.Collections.Generic.List<string>();
        foreach (RecipeInput input in FoundingSiteRules.InputsFor(module))
        {
            parts.Add(UiText.Format(
                "firstnight.directive.amount",
                input.Amount,
                ResourceTypeLocalizer.Label(input.Resource)));
        }

        string needed = parts.Count switch
        {
            0 => string.Empty,
            1 => parts[0],
            _ => UiText.Format(
                "firstnight.directive.join",
                string.Join(", ", parts.GetRange(0, parts.Count - 1)),
                parts[^1]),
        };

        return UiText.Format(
            module == FoundingSiteModule.Campfire
                ? "firstnight.directive.campfire"
                : "firstnight.directive.bedroll",
            needed);
    }

    private Vector2 SpiritAnchor()
    {
        FirstNightStage? stage = _controller?.GetFirstNightStage();
        // Only move into the fire once there is a fire to move into. Without
        // the anchor check the spirit read `_campfireScreenPosition`, which
        // stays at its constructor default while the founding site is still a
        // project — the middle of the screen. From `CampfireBuilt` onward the
        // spirit teleported there and hovered over nothing.
        bool inTheFlame = stage is not null
            && stage.Value >= FirstNightStage.CampfireBuilt
            && _hasCampfireAnchor;
        return inTheFlame
            ? _campfireScreenPosition
            : _founderScreenPosition + FireSpiritVisual.SpiritHoverOffset;
    }

    public override void _ExitTree()
    {
        if (_controller is not null)
        {
            _controller.FirstNightStageChanged -= OnFirstNightStageChanged;
            _controller.SelectionChanged -= OnSelectionChanged;
        }
        if (_macroView is not null)
        {
            _macroView.WorldDialogueAnchorsChanged -= RefreshAnchorsFromMacro;
        }
        if (_modalHost is not null)
        {
            _modalHost.Opened -= ProjectCurrentStage;
            _modalHost.Closed -= ProjectCurrentStage;
        }
    }

    /// <summary>
    /// The night speaks about the world, so it only speaks while the player is
    /// looking at it. Inside a building detail view or the hero profile the
    /// balloon and the spirit would float over a surface they have nothing to
    /// do with. They remain hidden there even though the world-dialogue layer
    /// now sits below every HUD-layer view.
    /// </summary>
    private void OnSelectionChanged(int selection)
    {
        _selection = (CityWorldController.Selection)selection;
        ProjectCurrentStage();
    }

    private bool IsWorldVisible =>
        _selection == CityWorldController.Selection.MacroView
        && _modalHost?.IsOpen != true;

    private void OnFirstNightStageChanged(int _)
    {
        ProjectCurrentStage();
    }

    private void OnStripFollowPressed()
    {
        _controller?.TryCloseFirstNightDialogue();
    }

    /// <summary>
    /// Re-reads the founder and campfire projected screen positions
    /// from the macro view through typed C# methods, and pushes the
    /// updated values into the visuals. Replaces the previous
    /// <c>HasMethod</c> + <c>Node.Call</c> + <c>_Process</c>-per-frame
    /// polling seam: the macro view now raises a typed
    /// <c>WorldDialogueAnchorsChanged</c> signal whenever the camera or
    /// projection moves, so this handler is the single refresh path.
    /// </summary>
    private void RefreshAnchorsFromMacro()
    {
        var macroView = _macroView;
        if (macroView is null) return;

        Vector2 founderScreen = macroView.GetFoundingArrivalGlobalPosition();
        if (founderScreen != Vector2.Zero)
        {
            _founderScreenPosition = founderScreen;
        }

        // Anchor the campfire on the structure that actually holds it. This
        // used to be the founder's own projected spot minus 32 px, which drew
        // the fire on top of the citizen — invisible while the embers were a
        // faint wireframe, an obvious bonfire standing in front of him once
        // they became a sprite.
        //
        // When the founding site is still a construction project there is no
        // Building to point at, and the macro view exposes no anchor for a
        // project. In that case we draw nothing: a fire in an invented place
        // is worse than no fire, because the player reads it as world state.
        int? siteId = _controller?.GetFoundingSiteBuildingId();
        if (siteId is int id)
        {
            Vector2 siteScreen = macroView.GetBuildingGlobalPosition(id);
            if (siteScreen != Vector2.Zero)
            {
                _campfireScreenPosition = siteScreen;
                _hasCampfireAnchor = true;
                ProjectCurrentStage();
                return;
            }
        }
        _hasCampfireAnchor = false;
        ProjectCurrentStage();
    }

    private void ProjectCurrentStage()
    {
        if (_controller is null) return;
        if (!IsWorldVisible)
        {
            _bubble.Vanish();
            _spirit.Vanish();
            _embers.Vanish();
            return;
        }
        // Deliberately does NOT refresh the anchors: RefreshAnchorsFromMacro
        // ends by calling this method, so re-entering it here made the two
        // mutually recursive and overflowed the stack on the first _Ready
        // where the world was visible — the scene never booted. The anchors
        // stay current because the macro view raises
        // WorldDialogueAnchorsChanged on every camera or projection move and
        // that handler runs even while the world is hidden, so a stage
        // projection only ever repaints with values already up to date.
        FirstNightStage? stage = _controller.GetFirstNightStage();
        bool isActive = _controller.IsFirstNightActive();
        if (stage is null || !isActive)
        {
            _bubble.Vanish();
            _spirit.Vanish();

            // The night may be over but the embers still sit on the
            // campfire until the player tears down the camp — the
            // chronicle marks the spirit's exit, the world shows its
            // trace.
            if (HasEmbersAfterDeparture())
            {
                if (_hasCampfireAnchor) _embers.PlaceAt(_campfireScreenPosition);
                else _embers.Vanish();
            }
            else
            {
                _embers.Vanish();
            }
            return;
        }

        // An active night never shows embers; the spirit is in the
        // flame instead.
        _embers.Vanish();

        // Resolve the body text from the catalogue; the catalogue
        // returns null for stages that wait on a module, which is
        // exactly when the balloon should hide — the player is being
        // asked to build something, not to read.
        LineageId lineage = _controller.GetHeroLineageId() ?? LineageId.Ardhen;
        FirstNightStage activeStage = stage.Value;
        IDialogueNode? node = FireSpiritDialogueCatalog.NodeFor(activeStage, lineage);
        if (node is null && FirstNightRules.WaitsForModule(activeStage))
        {
            // The two build stages have no authored line on purpose: they wait
            // for the player to make something. Left at that, the spirit fell
            // silent after two sentences and the "organic tutorial" never
            // taught anything. The balloon now carries a directive instead —
            // derived from the real recipe, never a hand-written quantity,
            // per DEC-0014 §4.
            _bubble.Speak(DescribeModuleDirective(activeStage), string.Empty);
            _bubble.FollowSpeaker(SpiritAnchor());
        }
        else if (node is null)
        {
            _bubble.Vanish();
        }
        else
        {
            bool isSleeping = activeStage == FirstNightStage.Sleeping;
            _bubble.Speak(
                UiText.Get(node.BodyKey),
                UiText.Get(isSleeping
                    ? WorldofGoses.Domain.Tr.FirstNight.SleepButton
                    : WorldofGoses.Domain.Tr.FirstNight.FollowButton),
                // Attribution comes from the node, not from whether the spirit
                // happens to be on screen: five of the six authored bodies are
                // narration about the spirit, and a balloon with a tail
                // presented them as the spirit narrating itself.
                hasSpeaker: node.SpeakerId == FireSpiritDialogueCatalog.FireSpiritSpeakerId
                    && FirstNightRules.SpiritIsPresent(activeStage));
            _bubble.FollowSpeaker(SpiritAnchor());
        }

        // Spirit visual: present only between Manifested and Sleeping
        // (FirstNightRules.SpiritIsPresent). Position is the founder
        // before the campfire exists, the campfire once it does.
        if (!FirstNightRules.SpiritIsPresent(activeStage))
        {
            _spirit.Vanish();
            return;
        }
        if (activeStage >= FirstNightStage.CampfireBuilt)
        {
            _spirit.PlaceOnCampfire(_campfireScreenPosition);
        }
        else
        {
            _spirit.PlaceBesideFounder(_founderScreenPosition);
        }
    }

    /// <summary>
    /// Architecture Hardening A9 closes the legacy <c>HasMethod</c> +
    /// <c>Node.Call</c> seam. Anchor refresh lives in
    /// <see cref="RefreshAnchorsFromMacro"/> (typed call path) and
    /// runs from the macro view's <c>WorldDialogueAnchorsChanged</c>
    /// signal — no per-frame polling, no string-based dispatch.
    /// </summary>
    private bool HasEmbersAfterDeparture()
    {
        if (_controller is null) return false;
        if (!_controller.HasFoundingSiteModule(FoundingSiteModule.Campfire)) return false;
        return _controller.HasSpiritDepartedEvent();
    }
}
