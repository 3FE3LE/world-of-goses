#nullable enable
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Sets and updates the project cursor at runtime. The cursor is
/// sourced from <c>res://assets/ui/icons/24/cursor.svg</c> (a copy of
/// Pixelarticons' <c>cursor-minimal</c>, MIT) with a white fill. Each
/// time the active linaje changes, the cursor image is re-baked with
/// the linaje's accent colour so the cursor reads as part of the same
/// family as the in-game icons.
///
/// Registered as an autoload under the name <c>CursorController</c>.
/// </summary>
public partial class CursorController : Node
{
    // Pixel-art cursors from Kenney's CC0 Cursor Pixel Pack, promoted one file
    // at a time. The arrow used to be a 24 px UI icon SVG, which read soft
    // against the game's nearest-neighbour pixel art, and the "interactive"
    // cursor was that same arrow re-coloured — so hovering a button produced a
    // paler arrow rather than a hand, and the affordance was lost.
    private const string CursorPath = "res://assets/ui/cursors/kenney-pixel/pointer.png";
    private const string InteractiveCursorPath =
        "res://assets/ui/cursors/kenney-pixel/hand_point.png";
    private const string GatherCursorPath =
        "res://assets/ui/cursors/kenney-pixel/axe.png";
    private const string PickaxeCursorPath =
        "res://assets/ui/cursors/kenney-pixel/pickaxe.png";
    private const string GrabCursorPath =
        "res://assets/ui/cursors/kenney-pixel/hand_grab.png";
    private static readonly Vector2 CursorHotspot = new(1, 1);
    private static readonly Vector2 InteractiveHotspot = new(5, 1);
    private static readonly Vector2 GatherHotspot = new(3, 13);
    private static readonly Vector2 PickaxeHotspot = new(3, 13);
    private static readonly Vector2 GrabHotspot = new(7, 4);

    private Texture2D? _arrowCursor;
    private Texture2D? _interactiveCursor;

    public override void _Ready()
    {
        LineageThemeRegistry.ActiveLineageChanged += OnLineageChanged;
        GetTree().NodeAdded += OnNodeAdded;
        ApplyCursor();
    }

    public override void _ExitTree()
    {
        LineageThemeRegistry.ActiveLineageChanged -= OnLineageChanged;
        GetTree().NodeAdded -= OnNodeAdded;
    }

    private void OnLineageChanged(string lineage) => ApplyCursor();

    private void ApplyCursor()
    {
        Color accent = LineageThemeRegistry.IconAccent;
        _arrowCursor = LoadTinted(CursorPath, accent);
        _interactiveCursor = LoadTinted(InteractiveCursorPath, accent.Lightened(0.38f));
        RestoreSurfaceCursor();
    }

    /// <summary>
    /// Loads a cursor and multiplies every opaque pixel by the lineage accent.
    /// The pack glyphs ship white with a dark outline, so multiplying recolours
    /// the fill and leaves the outline readable — which is exactly the
    /// "recolour by tokens, never deform geometry" rule the design bible sets
    /// for provisional icon art. The source PNG on disk is never modified.
    /// </summary>
    private static Texture2D? LoadTinted(string path, Color tint)
    {
        var source = ResourceLoader.Load<Texture2D>(path);
        if (source is null)
        {
            GD.PushWarning($"CursorController: cursor image missing at '{path}'.");
            return null;
        }

        Image? image = source.GetImage();
        if (image is null)
        {
            GD.PushWarning($"CursorController: could not read image data from '{path}'.");
            return null;
        }

        image.Convert(Image.Format.Rgba8);
        for (int y = 0; y < image.GetHeight(); y++)
        {
            for (int x = 0; x < image.GetWidth(); x++)
            {
                Color px = image.GetPixel(x, y);
                if (px.A <= 0f) continue;
                image.SetPixel(x, y, new Color(px.R * tint.R, px.G * tint.G, px.B * tint.B, px.A));
            }
        }
        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Shows the tool the founder would actually reach for. Every gatherable
    /// used to raise the axe, including stones and plant fibre, which told the
    /// player the wrong thing about what they were about to do.
    /// </summary>
    public void UseGatherCursor(ResourceType resource)
    {
        (string path, Vector2 hotspot) = resource switch
        {
            // Felling.
            ResourceType.Wood => (GatherCursorPath, GatherHotspot),
            // Breaking stone.
            ResourceType.SmallStone => (PickaxeCursorPath, PickaxeHotspot),
            // Everything else on the ground is picked up by hand: branches,
            // plant fibre, wild food.
            _ => (GrabCursorPath, GrabHotspot),
        };

        Texture2D? gather = ResourceLoader.Load<Texture2D>(path);
        if (gather is null) return;
        Input.SetCustomMouseCursor(gather, Input.CursorShape.Arrow, hotspot);
        Input.SetCustomMouseCursor(gather, Input.CursorShape.PointingHand, hotspot);
    }

    public void RestoreSurfaceCursor()
    {
        if (_arrowCursor is not null)
        {
            Input.SetCustomMouseCursor(
                _arrowCursor,
                Input.CursorShape.Arrow,
                CursorHotspot);
        }
        if (_interactiveCursor is not null)
        {
            Input.SetCustomMouseCursor(
                _interactiveCursor,
                Input.CursorShape.PointingHand,
                InteractiveHotspot);
        }
    }

    private static void OnNodeAdded(Node node)
    {
        if (node is BaseButton button)
        {
            button.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
            return;
        }
        if (node is LineEdit or TextEdit)
        {
            ((Control)node).MouseDefaultCursorShape = Control.CursorShape.Ibeam;
        }
    }
}
