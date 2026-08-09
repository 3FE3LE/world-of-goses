#nullable enable
using System.Linq;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

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
    /// Path to the macro street view, whose
    /// <c>GetFoundingArrivalGlobalPosition()</c> resolves the founder's
    /// projected screen position. Optional: when the macro view is
    /// absent (tests, editor-only fixtures) the scene falls back to a
    /// fixed viewport-centred placeholder.
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
    private FirstNightSpeechBubble _bubble = null!;
    private FireSpiritVisual _spirit = null!;
    private FirstNightEmbers _embers = null!;

    /// <summary>
    /// Approximate screen position of the founder at the start of the
    /// night. The macro view calls <see cref="UpdateFounderPosition"/>
    /// whenever the camera or the founder's actual screen position
    /// changes; the spirit visual reads this when it needs to hover
    /// beside the founder before the campfire is built.
    /// </summary>
    private Vector2 _founderScreenPosition = new(640, 360);

    /// <summary>
    /// Approximate screen position of the campfire once it has been
    /// built. The macro view calls <see cref="UpdateCampfirePosition"/>
    /// whenever the campfire is created or its visual anchor shifts.
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

        _modalHost = GetNodeOrNull<ModalHost>(ModalHostPath);
        if (_modalHost is not null)
        {
            _modalHost.Opened += ProjectCurrentStage;
            _modalHost.Closed += ProjectCurrentStage;
        }
        // Project the loaded stage on first frame so a save restored
        // mid-night shows its dialogue immediately, without waiting for
        // the next tick.
        ProjectCurrentStage();
    }

    /// <summary>
    /// Keeps the spirit, its bubble and the embers sitting on the world.
    ///
    /// <para>
    /// <c>MacroStreetLiveView</c> has no camera transform to inherit — it
    /// projects each street by hand from <c>CameraDepthAnchor</c> and
    /// <c>CameraLateral</c> — so moving the camera changes the *projection*,
    /// not a parent transform. Re-parenting would therefore not help; the
    /// position genuinely has to be re-derived. It used to be sampled only on
    /// a stage change, which is why the spirit looked nailed to the viewport:
    /// it stayed wherever it was the last time the night advanced.
    /// </para>
    /// </summary>
    public override void _Process(double delta)
    {
        _ = delta;
        if (_controller is null) return;
        if (!_spirit.Visible && !_embers.Visible && !_bubble.Visible) return;

        RefreshPositionsFromWorld();
        if (_spirit.Visible) _spirit.MoveTo(SpiritAnchor());
        if (_embers.Visible) _embers.PlaceAt(_campfireScreenPosition);
        if (_bubble.Visible) _bubble.FollowSpeaker(SpiritAnchor());
    }

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
                UiText.Get(input.Resource.ToString().ToLowerInvariant())));
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
        FirstNightState? night = _controller?.World.FirstNight;
        // Only move into the fire once there is a fire to move into. Without
        // the anchor check the spirit read `_campfireScreenPosition`, which
        // stays at its constructor default while the founding site is still a
        // project — the middle of the screen. From `CampfireBuilt` onward the
        // spirit teleported there and hovered over nothing.
        bool inTheFlame = night is not null
            && night.Stage >= FirstNightStage.CampfireBuilt
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

    /// <summary>
    /// Updates the cached founder screen position. The macro view
    /// invokes this whenever the founder's projected position shifts
    /// (camera follow, depth change, world resize).
    /// </summary>
    public void UpdateFounderPosition(Vector2 screenPosition)
    {
        _founderScreenPosition = screenPosition;
        ProjectCurrentStage();
    }

    /// <summary>
    /// Updates the cached campfire screen position. The macro view
    /// invokes this when the campfire is first completed and again
    /// whenever its anchor shifts. A null vector clears the cached
    /// position (used before the campfire exists).
    /// </summary>
    public void UpdateCampfirePosition(Vector2 screenPosition)
    {
        _campfireScreenPosition = screenPosition;
        ProjectCurrentStage();
    }

    private void OnFirstNightStageChanged(int _)
    {
        ProjectCurrentStage();
    }

    private void OnStripFollowPressed()
    {
        _controller?.TryCloseFirstNightDialogue();
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
        RefreshPositionsFromWorld();
        FirstNightState? night = _controller.World.FirstNight;
        if (night is null || !night.IsActive)
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
        LineageId lineage = _controller.World.Hero?.Profile.Lineage ?? LineageId.Ardhen;
        IDialogueNode? node = FireSpiritDialogueCatalog.NodeFor(night.Stage, lineage);
        if (node is null && FirstNightRules.WaitsForModule(night.Stage))
        {
            // The two build stages have no authored line on purpose: they wait
            // for the player to make something. Left at that, the spirit fell
            // silent after two sentences and the "organic tutorial" never
            // taught anything. The balloon now carries a directive instead —
            // derived from the real recipe, never a hand-written quantity,
            // per DEC-0014 §4.
            _bubble.Speak(DescribeModuleDirective(night.Stage), string.Empty);
            _bubble.FollowSpeaker(SpiritAnchor());
        }
        else if (node is null)
        {
            _bubble.Vanish();
        }
        else
        {
            bool isSleeping = night.Stage == FirstNightStage.Sleeping;
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
                    && FirstNightRules.SpiritIsPresent(night.Stage));
            _bubble.FollowSpeaker(SpiritAnchor());
        }

        // Spirit visual: present only between Manifested and Sleeping
        // (FirstNightRules.SpiritIsPresent). Position is the founder
        // before the campfire exists, the campfire once it does.
        if (!FirstNightRules.SpiritIsPresent(night.Stage))
        {
            _spirit.Vanish();
            return;
        }
        if (night.Stage >= FirstNightStage.CampfireBuilt)
        {
            _spirit.PlaceOnCampfire(_campfireScreenPosition);
        }
        else
        {
            _spirit.PlaceBesideFounder(_founderScreenPosition);
        }
    }

    /// <summary>
    /// Pulls live founder and campfire screen positions from the macro
    /// view. The founder's position is always available; the campfire
    /// position only resolves once the founding module is complete.
    /// Falls back to the cached placeholder when the macro view is
    /// absent (tests, fixtures).
    /// </summary>
    private void RefreshPositionsFromWorld()
    {
        var macroView = GetNodeOrNull<Node>(MacroViewPath);
        if (macroView is null) return;

        Vector2 founderScreen = InvokePositionGetter(
            macroView, "GetFoundingArrivalGlobalPosition");
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
        int? siteId = _controller?.World.FoundingSiteBuildingId();
        Vector2 siteScreen = siteId is null
            ? Vector2.Zero
            : InvokePositionGetter(macroView, "GetBuildingGlobalPosition", siteId.Value);
        _hasCampfireAnchor = siteScreen != Vector2.Zero;
        if (_hasCampfireAnchor) _campfireScreenPosition = siteScreen;
    }

    private static Vector2 InvokePositionGetter(Node node, string methodName, params Variant[] args)
    {
        if (!node.HasMethod(methodName)) return Vector2.Zero;
        Variant result = node.Call(methodName, args);
        return result.VariantType == Variant.Type.Vector2
            ? (Vector2)result
            : Vector2.Zero;
    }

    private bool HasEmbersAfterDeparture()
    {
        if (_controller is null) return false;
        CityWorld world = _controller.World;
        if (!world.HasFoundingSiteModule(FoundingSiteModule.Campfire)) return false;
        return world.Log.Events.Any(evt => evt.Kind == WorldEventKind.SpiritDeparted);
    }
}
