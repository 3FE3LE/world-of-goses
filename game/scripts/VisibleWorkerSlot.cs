using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Single worker placeholder inside the building detail view. The
/// slot has no domain knowledge: it holds a citizen identifier and
/// a display name and plays a short entry or exit animation when
/// configured.
///
/// Visual proportions match <see cref="PresentationConstants"/> so the
/// final art replaces a 64×96 canvas without re-anchoring the layout.
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
    private const string AnimWork = "work";
    private const string AnimExit = "exit";

    /// <summary>
    /// Path to the placeholder worker sprite. Real art lands here as
    /// side-facing frames; the slot's animations stay on the container
    /// Control so the sprite can be swapped without re-authoring them.
    /// </summary>
    [Export] public string WorkerSpritePath { get; set; } =
        "res://assets/characters/worker_placeholder.png";

    // Field initializer so Configure() can set Text before _Ready().
    private readonly Label _nameLabel = new()
    {
        Position = new Vector2(0, -20),
        Size = new Vector2(PresentationConstants.DetailedCitizenWidth, 18),
        HorizontalAlignment = HorizontalAlignment.Center,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    private TextureRect _sprite = null!;
    private Button _hitArea = null!;
    private AnimationPlayer _animationPlayer = null!;
    private AnimationLibrary _library = null!;
    private bool _exiting;

    public CitizenId CitizenId { get; private set; }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(
            PresentationConstants.DetailedCitizenWidth,
            PresentationConstants.DetailedCitizenHeight);

        _sprite = new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(WorkerSpritePath),
            StretchMode = TextureRect.StretchModeEnum.Keep,
            Size = new Vector2(
                PresentationConstants.DetailedCitizenWidth,
                PresentationConstants.DetailedCitizenHeight),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(_sprite);

        AddChild(_nameLabel);

        _hitArea = new Button
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
        _library.AddAnimation(AnimWork, BuildWorkAnimation());
        _library.AddAnimation(AnimExit, BuildExitAnimation());
        _animationPlayer.AddAnimationLibrary("", _library);

        _animationPlayer.AnimationFinished += OnAnimationFinished;
        _animationPlayer.Play(AnimEntry);
    }

    public void Configure(CitizenId citizenId, string displayName)
    {
        CitizenId = citizenId;
        _nameLabel.Text = displayName;
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

        if (name == AnimEntry)
        {
            _animationPlayer.Play(AnimWork);
        }
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

        int scaleTrack = entry.AddTrack(Animation.TrackType.Value);
        entry.TrackSetPath(scaleTrack, ".:scale");
        entry.TrackInsertKey(scaleTrack, 0.0, new Vector2(0.7f, 0.7f), 0);
        entry.TrackInsertKey(scaleTrack, 0.4, new Vector2(1f, 1f), 0);
        entry.TrackSetInterpolationType(scaleTrack, Animation.InterpolationType.Cubic);

        return entry;
    }

    private static Animation BuildWorkAnimation()
    {
        var work = new Animation
        {
            Length = 0.8f,
            Step = 0.05f,
            LoopMode = Animation.LoopModeEnum.Linear,
        };

        int workTrack = work.AddTrack(Animation.TrackType.Value);
        work.TrackSetPath(workTrack, ".:position");
        work.TrackInsertKey(workTrack, 0.0, new Vector2(0, 0), 0);
        work.TrackInsertKey(workTrack, 0.4, new Vector2(0, -3), 0);
        work.TrackInsertKey(workTrack, 0.8, new Vector2(0, 0), 0);
        work.TrackSetInterpolationType(workTrack, Animation.InterpolationType.Cubic);

        return work;
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

        int scaleTrack = exit.AddTrack(Animation.TrackType.Value);
        exit.TrackSetPath(scaleTrack, ".:scale");
        exit.TrackInsertKey(scaleTrack, 0.0, new Vector2(1f, 1f), 0);
        exit.TrackInsertKey(scaleTrack, 0.35, new Vector2(0.6f, 0.6f), 0);
        exit.TrackSetInterpolationType(scaleTrack, Animation.InterpolationType.Cubic);

        return exit;
    }
}
