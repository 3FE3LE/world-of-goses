using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// One visible citizen inside the building detail view. It resolves
/// the citizen's imported LPC scene through <see cref="CharacterVisualRegistry"/>
/// and plays presentation-only entry, idle, and exit motion.
///
/// Visual proportions match <see cref="PresentationConstants"/> so the
/// imported LPC art uses one unscaled 128×128 cell.
///
/// Initialization-order note: slots are created by code in
/// <see cref="VisibleWorkerSlots.Render"/> via <c>new VisibleWorkerSlot()</c>.
/// <see cref="Configure"/> runs BEFORE <c>AddChild</c> and therefore
/// BEFORE this slot's <c>_Ready()</c> — so any field that
/// <see cref="Configure"/> touches must be created via a field
/// initializer, not inside <c>_Ready()</c>. The label is the only such
/// field today; everything else (_sprite, _hitArea, _animationPlayer,
/// _library) is only touched after the slot is in the tree.
/// </summary>
public partial class VisibleWorkerSlot : Control
{
    [Signal] public delegate void CitizenActivatedEventHandler(int citizenId);

    private const string AnimEntry = "entry";
    private const string AnimExit = "exit";

    // Field initializer so Configure() can set Text before _Ready().
    private readonly Label _nameLabel = new()
    {
        Position = new Vector2(0, 2),
        Size = new Vector2(PresentationConstants.DetailedCitizenWidth, 18),
        HorizontalAlignment = HorizontalAlignment.Center,
        MouseFilter = Control.MouseFilterEnum.Ignore,
        ThemeTypeVariation = "BodySmall",
    };

    private LineageId _lineage = LineageId.Ardhen;
    private CharacterBodyVariant _bodyVariant;
    private LineageSpritePlayer _sprite = null!;
    private TooltipButton _hitArea = null!;
    private AnimationPlayer _animationPlayer = null!;
    private AnimationLibrary _library = null!;
    private bool _exiting;

    public CitizenId CitizenId { get; private set; }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(
            PresentationConstants.DetailedCitizenWidth,
            PresentationConstants.DetailedCitizenHeight);

        PackedScene visualScene = CharacterVisualRegistry.LoadScene(_lineage, _bodyVariant);
        _sprite = visualScene.Instantiate<LineageSpritePlayer>();
        _sprite.Position = new Vector2(
            PresentationConstants.DetailedCitizenWidth / 2,
            126);
        AddChild(_sprite);
        // The imported scene has autoplay metadata; select the resting
        // animation only after it enters the tree so autoplay cannot
        // replace this explicit building-state choice.
        _sprite.PlayIdle(Vector2.Down);

        AddChild(_nameLabel);

        _hitArea = new TooltipButton
        {
            Size = new Vector2(
                PresentationConstants.DetailedCitizenWidth,
                PresentationConstants.DetailedCitizenHeight),
            Flat = true,
            TooltipText = "Click to remove this worker",
        };
        _hitArea.Pressed += () =>
        {
            if (!_exiting)
            {
                EmitSignal(SignalName.CitizenActivated, CitizenId.Value);
            }
        };
        AddChild(_hitArea);

        _animationPlayer = new AnimationPlayer();
        AddChild(_animationPlayer);
        _library = new AnimationLibrary();
        _library.AddAnimation(AnimEntry, BuildEntryAnimation());
        _library.AddAnimation(AnimExit, BuildExitAnimation());
        _animationPlayer.AddAnimationLibrary("", _library);

        _animationPlayer.AnimationFinished += OnAnimationFinished;
        _animationPlayer.Play(AnimEntry);
    }

    public void Configure(BuildingDetailSnapshot.CitizenItem citizen)
    {
        CitizenId = citizen.Id;
        _lineage = citizen.Lineage;
        _bodyVariant = CharacterVisualRegistry.ResolveBodyVariant(citizen.Gender);
        _nameLabel.Text = citizen.Name;
    }

    /// <summary>
    /// Plays the exit transition then frees the node. Called when
    /// the worker is removed from the building.
    /// </summary>
    public void PlayExitAndFree()
    {
        if (_exiting)
        {
            return;
        }

        _exiting = true;
        _hitArea.Disabled = true;
        _animationPlayer.Stop();
        _animationPlayer.Play(AnimExit);
    }

    private void OnAnimationFinished(StringName name)
    {
        if (_exiting)
        {
            if (name == AnimExit)
            {
                QueueFree();
            }
            return;
        }

        // Entry ends at the resting position. The LPC SpriteFrames
        // continue their own idle loop without procedural locomotion.
    }

    private static Animation BuildEntryAnimation()
    {
        var entry = new Animation
        {
            Length = 0.4f,
            Step = 0.05f,
        };

        int posTrack = entry.AddTrack(Animation.TrackType.Value);
        entry.TrackSetPath(posTrack, ".:position");
        entry.TrackInsertKey(posTrack, 0.0, new Vector2(0, 24), 0);
        entry.TrackInsertKey(posTrack, 0.4, new Vector2(0, 0), 0);
        entry.TrackSetInterpolationType(posTrack, Animation.InterpolationType.Cubic);

        return entry;
    }

    private static Animation BuildExitAnimation()
    {
        var exit = new Animation
        {
            Length = 0.35f,
            Step = 0.05f,
        };

        int posTrack = exit.AddTrack(Animation.TrackType.Value);
        exit.TrackSetPath(posTrack, ".:position");
        exit.TrackInsertKey(posTrack, 0.0, new Vector2(0, 0), 0);
        exit.TrackInsertKey(posTrack, 0.35, new Vector2(0, 24), 0);
        exit.TrackSetInterpolationType(posTrack, Animation.InterpolationType.Cubic);

        return exit;
    }
}
