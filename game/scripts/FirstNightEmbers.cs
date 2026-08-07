#nullable enable
using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Presentation placeholder for the embers the campfire leaves behind
/// once the fire spirit has departed at dawn
/// (<c>docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md</c> §11). The embers
/// replace the spirit's inhabited ring on the same screen position,
/// so the campfire visually transitions from "spirit-present" to
/// "spirit-departed" without any geometry shift.
///
/// <para>
/// This was a wireframe quadrilateral, which read as an abstract marker
/// stamped on a terrain tile rather than as a spent fire — a playtester
/// asked whether it was a lineage sigil. It is now the atlas's campfire
/// sprite, darkened and desaturated so it reads as a fire that has gone
/// out on the spot where the spirit lived.
/// </para>
/// </summary>
public partial class FirstNightEmbers : Node2D
{
    /// <summary>Campfire tile in the shared roguelike atlas.</summary>
    private const int CampfireColumn = 14;
    private const int CampfireRow = 8;

    /// <summary>Integer magnification, matching the world's other 16 px sprites.</summary>
    private const int Scale2X = 2;

    /// <summary>Cooled down: the fire's own colours, pushed toward ash.</summary>
    private static readonly Color SpentTint = new(0.62f, 0.44f, 0.34f, 0.92f);

    private Sprite2D _sprite = null!;

    public override void _Ready()
    {
        var atlas = ResourceLoader.Load<Texture2D>(TerrainAtlas.AtlasPath);
        _sprite = new Sprite2D
        {
            Texture = atlas is null
                ? null
                : new AtlasTexture
                {
                    Atlas = atlas,
                    Region = TerrainAtlas.Region(CampfireColumn, CampfireRow),
                },
            Scale = new Vector2(Scale2X, Scale2X),
            Modulate = SpentTint,
            TextureFilter = TextureFilterEnum.Nearest,
        };
        AddChild(_sprite);

        Visible = false;
    }

    /// <summary>Positions the embers at the campfire's screen location.</summary>
    public void PlaceAt(Vector2 campfirePosition)
    {
        Position = campfirePosition + new Vector2(0f, -4f);
        Visible = true;
    }

    /// <summary>Hides the embers; the controller may re-show them later.</summary>
    public void Vanish()
    {
        Visible = false;
    }
}
