#nullable enable
using System.Collections.Generic;
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
    private const float HeroHitboxPx = 128f;

    /// <summary>
    /// Emitted when the player clicks the hero sprite on the macro
    /// view. The host routes the click to the hero profile detail.
    /// </summary>
    [Signal] public delegate void HeroClickedEventHandler();

    private CitizenSpriteCarrier? _heroCarrier;
    private Vector2 _heroBasePosition;
    private float _walkClock;
    private bool _walkingRight = true;
    private bool _heroHovered;

    /// <summary>
    /// (Re)builds the macro population dots or places the walking
    /// hero sprite when the city is empty. Positions are deterministic
    /// so the visual is identical across reloads.
    /// </summary>
    /// <param name="citizens">Citizens projected for the macro view.</param>
    /// <param name="buildingCount">Total buildings (zero while the
    /// world is empty).</param>
    /// <param name="projectCount">Total in-flight construction projects
    /// (zero while the world is empty).</param>
    public void Populate(
        IReadOnlyList<CityMacroSnapshot.CitizenItem> citizens,
        int buildingCount = 0,
        int projectCount = 0)
    {
        if (_heroHovered)
        {
            _heroHovered = false;
            Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
        }
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }
        if (_heroCarrier?.State == CitizenSpriteCarrier.VisualState.Macro)
        {
            _heroCarrier.SetState(CitizenSpriteCarrier.VisualState.Hidden);
        }
        _heroCarrier = null;

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
        if (citizens.Count == 1 && buildingCount == 0 && projectCount == 0 && _hero is not null)
        {
            PlaceWalkingHero(_hero, parentSize);
            return;
        }

        for (int i = 0; i < citizens.Count; i++)
        {
            int denominator = Mathf.Max(citizens.Count, 1);
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

            if (!citizens[i].IsAvailable) continue;

            // An unassigned citizen is physically AtHome in the domain. On
            // the macro view, keep that state visible by naming its existing
            // population marker instead of letting it become an anonymous dot.
            string statusIcon = CitizenStatusIcon(citizens[i]);
            var row = new HBoxContainer
            {
                Position = new Vector2(x - 48f, y + PresentationConstants.MacroCitizenSize + 3f),
                Size = new Vector2(96f, 22f),
                Alignment = BoxContainer.AlignmentMode.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation", 4);
            AddChild(row);

            var icon = new TextureRect
            {
                Texture = ResourceLoader.Load<Texture2D>(statusIcon),
                StretchMode = TextureRect.StretchModeEnum.Keep,
                CustomMinimumSize = new Vector2(12, 12),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Modulate = LineageThemeRegistry.IconAccent,
            };
            row.AddChild(icon);

            var nameLabel = new Label
            {
                Text = citizens[i].Name,
                ThemeTypeVariation = "BodySmall",
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            row.AddChild(nameLabel);
        }
    }

    /// <summary>
    /// Maps a citizen's state to the icon shown next to their name on
    /// the macro view. The hero walking on the empty field ignores
    /// this helper (the sprite carries the affordance).
    /// </summary>
    private static string CitizenStatusIcon(CityMacroSnapshot.CitizenItem item)
    {
        if (item.CurrentStamina <= 0)
        {
            return IconPaths.Warning;
        }
        return item.Location switch
        {
            CitizenLocation.AtHome => IconPaths.House,
            CitizenLocation.AtWork => IconPaths.Building,
            _ => IconPaths.User,
        };
    }

    /// <summary>
    /// Hero reference, set by <see cref="CityMacroView"/> alongside
    /// the population count so this node can resolve the lineage +
    /// gender scene for the empty-field sprite. Null when no hero
    /// exists yet (population mode applies).
    /// </summary>
    public CityMacroSnapshot.HeroVisual? Hero
    {
        get => _hero;
        set
        {
            _hero = value;
        }
    }
    private CityMacroSnapshot.HeroVisual? _hero;

    private void PlaceWalkingHero(CityMacroSnapshot.HeroVisual hero, Vector2 parentSize)
    {
        _heroCarrier = CitizenSpriteBank.Instance.GetOrCreate(hero.Id, hero.Lineage, hero.Gender);
        CitizenSpriteBank.Instance.Mount(_heroCarrier, this);
        _heroBasePosition = new Vector2(parentSize.X * 0.5f, parentSize.Y * 0.6f);
        _heroCarrier.SetPositionImmediate(_heroBasePosition);
        _heroCarrier.SetState(CitizenSpriteCarrier.VisualState.Macro);
        _walkClock = 0f;
        _walkingRight = true;
        _heroCarrier.Walk(Vector2.Right);
    }

    public override void _Process(double delta)
    {
        if (_heroCarrier is null || _heroCarrier.State != CitizenSpriteCarrier.VisualState.Macro) return;

        // Pause the walk cycle when the player's cursor is over the
        // hero sprite. The hero stops in place, which signals "this
        // is interactive" and gives the player a moment to click.
        UpdateHoverState();
        if (_heroHovered) return;

        _walkClock += (float)delta;
        float phase = (_walkClock / WalkPeriodSeconds) * Mathf.Tau;
        float offsetX = Mathf.Sin(phase) * WalkAmplitudePx;
        _heroCarrier.SetPositionImmediate(new Vector2(
            _heroBasePosition.X + offsetX,
            _heroBasePosition.Y));

        bool nowRight = offsetX >= 0f;
        if (nowRight != _walkingRight)
        {
            _walkingRight = nowRight;
            _heroCarrier.Walk(_walkingRight ? Vector2.Right : Vector2.Left);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_heroCarrier is null || _heroCarrier.State != CitizenSpriteCarrier.VisualState.Macro) return;
        if (@event is InputEventMouseButton mb
            && mb.ButtonIndex == MouseButton.Left
            && mb.Pressed
            && _heroHovered)
        {
            EmitSignal(SignalName.HeroClicked);
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>
    /// Tracks whether the cursor is currently over the hero sprite and
    /// toggles the global cursor shape to communicate that the sprite
    /// is interactive. The hitbox is the same 128×128 box the LPC
    /// sprite uses, so the hint matches the visible art.
    /// </summary>
    private void UpdateHoverState()
    {
        if (_heroCarrier is null) return;
        Vector2 mouse = GetViewport().GetMousePosition();
        Vector2 pos = _heroCarrier.GlobalPosition;
        float half = HeroHitboxPx * 0.5f;
        bool nowHovered = mouse.X >= pos.X - half
            && mouse.X <= pos.X + half
            && mouse.Y >= pos.Y - half
            && mouse.Y <= pos.Y + half;
        if (nowHovered == _heroHovered) return;
        _heroHovered = nowHovered;
        Input.SetDefaultCursorShape(nowHovered
            ? Input.CursorShape.PointingHand
            : Input.CursorShape.Arrow);
    }
}
