#nullable enable
using System.Linq;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Presentation host for the authored first night
/// (<c>docs/19_FIRST_NIGHT_AND_FIRE_SPIRIT.md</c>). Owns the
/// non-modal <see cref="FirstNightDialogueStrip"/> and the
/// <see cref="FireSpiritVisual"/>, both rendered on a private
/// <see cref="CanvasLayer"/> at <c>Layer=50</c> so they occlude
/// the construction and expedition modals without hiding the
/// pause menu or <see cref="Notifier"/> toasts (mirroring the
/// comment on <see cref="OverlayLayers.Tutorial"/>).
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
/// happens here — not in <see cref="FirstNightDialogueStrip"/>
/// itself — so the strip stays a pure layout primitive and tests
/// for it stay Godot-free.
/// </para>
/// </summary>
public partial class FirstNightScene : Node
{
    [Export] public NodePath ControllerPath { get; set; } = "../../../CityWorldController";

    private CityWorldController? _controller;
    private CanvasLayer _layer = null!;
    private FirstNightDialogueStrip _strip = null!;
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

    public override void _Ready()
    {
        _layer = new CanvasLayer { Layer = OverlayLayers.Tutorial };
        AddChild(_layer);

        _strip = new FirstNightDialogueStrip();
        _strip.SetActionLabels(
            UiText.Get(WorldofGoses.Domain.Tr.FirstNight.FollowButton),
            UiText.Get(WorldofGoses.Domain.Tr.FirstNight.SleepButton));
        _layer.AddChild(_strip);
        _strip.FollowPressed += OnStripFollowPressed;

        _spirit = new FireSpiritVisual();
        _layer.AddChild(_spirit);

        _embers = new FirstNightEmbers();
        _layer.AddChild(_embers);

        _controller = GetNodeOrNull<CityWorldController>(ControllerPath);
        if (_controller is null)
        {
            GD.PushWarning(
                "FirstNightScene could not resolve CityWorldController at " +
                $"{ControllerPath}; the night will render but stay inert.");
            return;
        }
        _controller.FirstNightStageChanged += OnFirstNightStageChanged;
        // Project the loaded stage on first frame so a save restored
        // mid-night shows its dialogue immediately, without waiting for
        // the next tick.
        ProjectCurrentStage();
    }

    public override void _ExitTree()
    {
        if (_controller is not null)
        {
            _controller.FirstNightStageChanged -= OnFirstNightStageChanged;
        }
    }

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
        FirstNightState? night = _controller.World.FirstNight;
        if (night is null || !night.IsActive)
        {
            _strip.Vanish();
            _spirit.Vanish();

            // The night may be over but the embers still sit on the
            // campfire until the player tears down the camp — the
            // chronicle marks the spirit's exit, the world shows its
            // trace.
            if (HasEmbersAfterDeparture())
            {
                _embers.PlaceAt(_campfireScreenPosition);
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
        // exactly when the strip should hide.
        LineageId lineage = _controller.World.Hero?.Profile.Lineage ?? LineageId.Ardhen;
        IDialogueNode? node = FireSpiritDialogueCatalog.NodeFor(night.Stage, lineage);
        if (node is null)
        {
            _strip.Vanish();
        }
        else
        {
            bool isSleeping = night.Stage == FirstNightStage.Sleeping;
            _strip.ShowNode(UiText.Get(node.BodyKey), isSleeping);
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

    private bool HasEmbersAfterDeparture()
    {
        if (_controller is null) return false;
        CityWorld world = _controller.World;
        if (!world.HasFoundingSiteModule(FoundingSiteModule.Campfire)) return false;
        return world.Log.Events.Any(evt => evt.Kind == WorldEventKind.SpiritDeparted);
    }
}
