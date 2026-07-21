#nullable enable
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Renders the world's citizen population on the macro city view.
///
/// <para>
/// Two modes:
/// </para>
/// <list type="bullet">
///   <item><description><b>Population dots</b> when the city has at
///   least one building or more than one citizen. One small marker per
///   citizen, in a gentle arc around the city centre.</description></item>
///   <item><description><b>Hero on the field</b> when there is exactly
///   one citizen (the hero) and no buildings. A full LPC sprite walks
///   side-to-side in the centre of the field so the empty world still
///   reads as alive.</description></item>
/// </list>
/// </summary>
public partial class MacroCitizenActivity : Node2D
{
    private const float WalkAmplitudePx = 220f;
    private const float WalkPeriodSeconds = 3.6f;

    private LineageSpritePlayer? _heroSprite;
    private Vector2 _heroBasePosition;
    private float _walkClock;
    private bool _walkingRight = true;

    /// <summary>
    /// (Re)builds the macro population dots or places the walking
    /// hero sprite when the city is empty. Positions are deterministic
    /// so the visual is identical across reloads.
    /// </summary>
    /// <param name="citizenCount">Total citizens in the world.</param>
    /// <param name="buildingCount">Total buildings (zero while the
    /// world is empty).</param>
    /// <param name="projectCount">Total in-flight construction projects
    /// (zero while the world is empty).</param>
    public void Populate(int citizenCount, int buildingCount = 0, int projectCount = 0)
    {
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }
        _heroSprite = null;

        var parentSize = ((Control)GetParent()).Size;
        if (parentSize == Vector2.Zero)
        {
            parentSize = new Vector2(
                PresentationConstants.CanvasWidth,
                PresentationConstants.CanvasHeight);
        }

        // Lone hero in a freshly founded world: render the hero sprite
        // walking in the centre of the field. Once the player authorises
        // any construction, the work-site (or later plot) takes over
        // the centre and the dot pattern returns.
        if (citizenCount == 1 && buildingCount == 0 && projectCount == 0 && _hero is not null)
        {
            PlaceWalkingHero(_hero, parentSize);
            return;
        }

        for (int i = 0; i < citizenCount; i++)
        {
            int denominator = Mathf.Max(citizenCount, 1);
            float angle = Mathf.Pi * (0.15f + 0.7f * (i / (float)denominator));
            float radius = 220f;
            float cx = parentSize.X * 0.5f;
            float cy = parentSize.Y * 0.85f;
            float x = cx + Mathf.Cos(angle) * radius;
            float y = cy - Mathf.Sin(angle) * radius;

            var dot = new ColorRect
            {
                Color = new Color("c8b88a"),
                Size = new Vector2(
                    PresentationConstants.MacroCitizenSize,
                    PresentationConstants.MacroCitizenSize),
                Position = new Vector2(x, y),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            dot.AddToGroup(PresentationConstants.GroupMacroCitizenDot);
            AddChild(dot);
        }
    }

    /// <summary>
    /// Hero reference, set by <see cref="CityMacroView"/> alongside
    /// the population count so this node can resolve the lineage +
    /// gender scene for the empty-field sprite. Null when no hero
    /// exists yet (population mode applies).
    /// </summary>
    public Citizen? Hero
    {
        get => _hero;
        set
        {
            _hero = value;
        }
    }
    private Citizen? _hero;

    private void PlaceWalkingHero(Citizen hero, Vector2 parentSize)
    {
        var bodyVariant = CharacterVisualRegistry.ResolveBodyVariant(hero.Profile.Gender);
        var scene = CharacterVisualRegistry.LoadScene(hero.Profile.Lineage, bodyVariant);
        _heroSprite = scene.Instantiate<LineageSpritePlayer>();
        _heroBasePosition = new Vector2(parentSize.X * 0.5f, parentSize.Y * 0.6f);
        _heroSprite.Position = _heroBasePosition;
        AddChild(_heroSprite);
        _walkClock = 0f;
        _walkingRight = true;
        _heroSprite.PlayWalk(Vector2.Right);
    }

    public override void _Process(double delta)
    {
        if (_heroSprite is null) return;
        _walkClock += (float)delta;
        float phase = (_walkClock / WalkPeriodSeconds) * Mathf.Tau;
        float offsetX = Mathf.Sin(phase) * WalkAmplitudePx;
        _heroSprite.Position = new Vector2(
            _heroBasePosition.X + offsetX,
            _heroBasePosition.Y);

        bool nowRight = offsetX >= 0f;
        if (nowRight != _walkingRight)
        {
            _walkingRight = nowRight;
            _heroSprite.PlayWalk(_walkingRight ? Vector2.Right : Vector2.Left);
        }
    }
}