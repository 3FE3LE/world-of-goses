#nullable enable
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses.Ui;

/// <summary>
/// Project-owned procedural pixel icon for one resource kind. The geometry is
/// drawn on integer coordinates with antialiasing disabled, so the same visual
/// can be reused by storage rows and transient world feedback without adding a
/// raster asset outside the Pixelorama pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The icon-only HUD ticker means every resource that can appear in the
/// same row must have a distinct silhouette. The 16 px canvas therefore
/// hosts nine purposefully different shapes — a boulder, a cluster of
/// pebbles, a log end, a Y-forked twig, a tied sheaf, an apple, a berry
/// cluster, an ingot and a flask — rather than nine colour-only variants
/// of the same box. Each silhouette stays readable after the lineage-accent
/// <c>Modulate</c> pass that the rest of the HUD applies uniformly.
/// </para>
/// <para>
/// All geometry uses integer pixel rectangles; no antialiasing is requested.
/// Curves (the log end, the apple, the flask body) are approximated by
/// stacking short rects of varying width — a deliberate "pixel art" idiom
/// that keeps the silhouette readable at the 24 px cell the HUD reserves.
/// </para>
/// </remarks>
public partial class ResourceIcon : Control
{
    private const float CanvasSize = 16f;
    private ResourceType _resourceType;

    public ResourceType ResourceType
    {
        get => _resourceType;
        set
        {
            _resourceType = value;
            QueueRedraw();
        }
    }

    public ResourceIcon()
    {
        CustomMinimumSize = new Vector2(20, 20);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public ResourceIcon(ResourceType resourceType) : this()
    {
        _resourceType = resourceType;
    }

    public override void _Draw()
    {
        Vector2 origin = new(
            Mathf.Round((Size.X - CanvasSize) * 0.5f),
            Mathf.Round((Size.Y - CanvasSize) * 0.5f));
        switch (_resourceType)
        {
            case ResourceType.Stone:      DrawBoulder(origin);  break;
            case ResourceType.SmallStone: DrawPebbles(origin);  break;
            case ResourceType.Wood:       DrawLogEnd(origin);   break;
            case ResourceType.Branches:   DrawTwig(origin);     break;
            case ResourceType.PlantFiber: DrawSheaf(origin);    break;
            case ResourceType.Food:       DrawApple(origin);    break;
            case ResourceType.WildFood:   DrawBerries(origin);  break;
            case ResourceType.Iron:       DrawIngot(origin);    break;
            case ResourceType.Potions:    DrawFlask(origin);    break;
            default:                      DrawFallback(origin); break;
        }
    }

    // ── Silhouettes ───────────────────────────────────────────────────────
    //
    // Each helper is laid out on a 16×16 grid. Cols are 0..15, rows are 0..15.
    // Coordinates are integers, rects share edges, and no rect is partially
    // covered by another of the same color. The order in which rects are
    // drawn matches the natural top-to-bottom rendering of the shape.

    private static readonly Color StoneMid       = new("#777f84");
    private static readonly Color StoneLight     = new("#aeb4b6");
    private static readonly Color StoneHighlight = new("#d0d4d3");

    private static readonly Color IronMid        = new("#59626d");
    private static readonly Color IronLight      = new("#919ba5");
    private static readonly Color IronHighlight  = new("#c1c7ca");

    private static readonly Color WoodMid        = new("#8f5b32");
    private static readonly Color WoodLight      = new("#bd7d45");
    private static readonly Color WoodRing       = new("#5a3a1f");

    private static readonly Color TwigDark       = new("#7a4a26");
    private static readonly Color TwigLight      = new("#b87942");

    private static readonly Color FiberStalk     = new("#4f7f35");
    private static readonly Color FiberLeaf      = new("#73a942");
    private static readonly Color FiberTie       = new("#c8a26a");

    private static readonly Color AppleSkin      = new("#c4514f");
    private static readonly Color AppleShade     = new("#8c2f2d");
    private static readonly Color AppleLeaf      = new("#568b3d");
    private static readonly Color AppleStem      = new("#5a3a1f");

    private static readonly Color BerryA         = new("#9c2b5c");
    private static readonly Color BerryB         = new("#d9a441");
    private static readonly Color BerryLeaf      = new("#568b3d");

    private static readonly Color PotionBody     = new("#5b3da8");
    private static readonly Color PotionShine    = new("#a98fe0");
    private static readonly Color PotionNeck     = new("#3a2670");
    private static readonly Color PotionCork     = new("#8f5b32");

    private void DrawBoulder(Vector2 origin)
    {
        // Irregular peak on the top-left, broad base — a single heavy mass.
        // Outline reads "boulder", not "log" (Stone is full canvas width)
        // and not "pebble" (SmallStone is a cluster of small lumps).
        PixelRect(origin, 6, 2, 2, 1, StoneMid);   // peak
        PixelRect(origin, 4, 3, 6, 1, StoneMid);   // upper mass
        PixelRect(origin, 3, 4, 8, 1, StoneMid);
        PixelRect(origin, 2, 5, 10, 1, StoneMid);
        PixelRect(origin, 1, 6, 12, 1, StoneMid);
        PixelRect(origin, 0, 7, 16, 1, StoneMid);
        PixelRect(origin, 0, 8, 16, 1, StoneMid);
        PixelRect(origin, 0, 9, 16, 1, StoneMid);
        PixelRect(origin, 0, 10, 16, 1, StoneMid);
        PixelRect(origin, 0, 11, 16, 1, StoneMid);
        PixelRect(origin, 0, 12, 16, 1, StoneMid);
        PixelRect(origin, 0, 13, 16, 1, StoneMid);
        PixelRect(origin, 0, 14, 16, 1, StoneMid);
        // Highlight ridge on the top-left slope
        PixelRect(origin, 5, 3, 1, 1, StoneLight);
        PixelRect(origin, 4, 4, 1, 1, StoneLight);
        PixelRect(origin, 3, 5, 1, 1, StoneLight);
        PixelRect(origin, 2, 6, 1, 1, StoneLight);
        PixelRect(origin, 1, 7, 1, 1, StoneLight);
        PixelRect(origin, 1, 8, 1, 1, StoneLight);
        // Hot edge
        PixelRect(origin, 5, 4, 1, 1, StoneHighlight);
    }

    private void DrawPebbles(Vector2 origin)
    {
        // Three independent small lumps, each visibly smaller than Stone.
        // Pebble 1 (left): tallest
        PixelRect(origin, 1, 9, 4, 2, StoneLight);
        PixelRect(origin, 2, 8, 2, 1, StoneLight);
        PixelRect(origin, 2, 9, 1, 1, StoneHighlight);
        // Pebble 2 (centre): lowest
        PixelRect(origin, 6, 11, 4, 2, StoneLight);
        PixelRect(origin, 7, 10, 2, 1, StoneLight);
        PixelRect(origin, 7, 11, 1, 1, StoneHighlight);
        // Pebble 3 (right): medium
        PixelRect(origin, 11, 10, 4, 2, StoneLight);
        PixelRect(origin, 12, 9, 2, 1, StoneLight);
        PixelRect(origin, 12, 10, 1, 1, StoneHighlight);
        // Subtle shadow line under each cluster to anchor them
        PixelRect(origin, 0, 13, 16, 1, StoneMid);
    }

    private void DrawLogEnd(Vector2 origin)
    {
        // Round log cross-section. A circle of stacked rects with a darker
        // outer ring, a lighter heartwood band and a small dark knot at
        // the centre — clearly different silhouette from the angular Stone.
        // Outer ring (mid)
        PixelRect(origin, 6, 1, 4, 1, WoodMid);
        PixelRect(origin, 4, 2, 8, 1, WoodMid);
        PixelRect(origin, 3, 3, 10, 1, WoodMid);
        PixelRect(origin, 2, 4, 12, 1, WoodMid);
        PixelRect(origin, 1, 5, 14, 1, WoodMid);
        PixelRect(origin, 1, 6, 14, 1, WoodMid);
        PixelRect(origin, 1, 7, 14, 1, WoodMid);
        PixelRect(origin, 1, 8, 14, 1, WoodMid);
        PixelRect(origin, 1, 9, 14, 1, WoodMid);
        PixelRect(origin, 1, 10, 14, 1, WoodMid);
        PixelRect(origin, 2, 11, 12, 1, WoodMid);
        PixelRect(origin, 3, 12, 10, 1, WoodMid);
        PixelRect(origin, 4, 13, 8, 1, WoodMid);
        PixelRect(origin, 6, 14, 4, 1, WoodMid);
        // Heartwood band (lighter)
        PixelRect(origin, 7, 2, 2, 1, WoodLight);
        PixelRect(origin, 5, 3, 6, 1, WoodLight);
        PixelRect(origin, 4, 4, 8, 1, WoodLight);
        PixelRect(origin, 3, 5, 10, 1, WoodLight);
        PixelRect(origin, 3, 6, 10, 1, WoodLight);
        PixelRect(origin, 3, 7, 10, 1, WoodLight);
        PixelRect(origin, 3, 8, 10, 1, WoodLight);
        PixelRect(origin, 3, 9, 10, 1, WoodLight);
        PixelRect(origin, 3, 10, 10, 1, WoodLight);
        PixelRect(origin, 4, 11, 8, 1, WoodLight);
        PixelRect(origin, 5, 12, 6, 1, WoodLight);
        PixelRect(origin, 7, 13, 2, 1, WoodLight);
        // Center knot (darkest)
        PixelRect(origin, 7, 6, 2, 1, WoodRing);
        PixelRect(origin, 7, 7, 2, 1, WoodRing);
        PixelRect(origin, 6, 7, 4, 1, WoodRing);
        PixelRect(origin, 6, 8, 4, 1, WoodRing);
        PixelRect(origin, 7, 9, 2, 1, WoodRing);
    }

    private void DrawTwig(Vector2 origin)
    {
        // Y-forked twig: a single thicker trunk with two diverging smaller
        // branches at the top, all knotted at one vertex. Distinct from the
        // log (no ring) and from the crossed diagonal pair that this icon
        // used to render (which read as a "+" against a log).
        // Trunk
        PixelRect(origin, 7, 8, 2, 7, TwigDark);
        // Right fork
        PixelRect(origin, 8, 7, 2, 1, TwigDark);
        PixelRect(origin, 9, 6, 2, 1, TwigDark);
        PixelRect(origin, 10, 5, 2, 1, TwigDark);
        PixelRect(origin, 11, 4, 2, 1, TwigDark);
        PixelRect(origin, 12, 3, 2, 1, TwigDark);
        // Left fork
        PixelRect(origin, 5, 7, 2, 1, TwigDark);
        PixelRect(origin, 4, 6, 2, 1, TwigDark);
        PixelRect(origin, 3, 5, 2, 1, TwigDark);
        PixelRect(origin, 2, 4, 2, 1, TwigDark);
        PixelRect(origin, 1, 3, 2, 1, TwigDark);
        // Highlight strip on the trunk's left side
        PixelRect(origin, 7, 8, 1, 6, TwigLight);
        // Knot at the fork vertex
        PixelRect(origin, 6, 8, 1, 1, TwigLight);
    }

    private void DrawSheaf(Vector2 origin)
    {
        // Five vertical stalks bound by a horizontal tie band: "tied
        // bundle of fibres" — silhouette is straight and bundled, not
        // forked, not round. Clearly different from Food and WildFood.
        // Stalks
        PixelRect(origin, 2, 4, 1, 10, FiberStalk);
        PixelRect(origin, 5, 3, 1, 11, FiberStalk);
        PixelRect(origin, 8, 2, 1, 12, FiberStalk);
        PixelRect(origin, 11, 3, 1, 11, FiberStalk);
        PixelRect(origin, 13, 4, 1, 10, FiberStalk);
        // Leaf caps on the tallest stalk
        PixelRect(origin, 7, 1, 3, 1, FiberLeaf);
        PixelRect(origin, 8, 0, 1, 1, FiberLeaf);
        // Tie band (lighter tan strip across the middle)
        PixelRect(origin, 1, 9, 14, 2, FiberTie);
        PixelRect(origin, 1, 9, 14, 1, FiberTie);
        // Subtle stalk highlight on the left of each
        PixelRect(origin, 2, 4, 1, 5, FiberLeaf);
        PixelRect(origin, 5, 3, 1, 6, FiberLeaf);
        PixelRect(origin, 8, 2, 1, 7, FiberLeaf);
        PixelRect(origin, 11, 3, 1, 6, FiberLeaf);
        PixelRect(origin, 13, 4, 1, 5, FiberLeaf);
    }

    private void DrawApple(Vector2 origin)
    {
        // Round apple body with a short stem and a single leaf — distinct
        // from the WildFood cluster (which is three separate berries) and
        // from the Potion flask (taller, narrower neck).
        // Body (round-ish: stacked rects)
        PixelRect(origin, 6, 4, 4, 1, AppleSkin);
        PixelRect(origin, 4, 5, 8, 1, AppleSkin);
        PixelRect(origin, 3, 6, 10, 1, AppleSkin);
        PixelRect(origin, 3, 7, 10, 1, AppleSkin);
        PixelRect(origin, 2, 8, 12, 1, AppleSkin);
        PixelRect(origin, 2, 9, 12, 1, AppleSkin);
        PixelRect(origin, 2, 10, 12, 1, AppleSkin);
        PixelRect(origin, 3, 11, 10, 1, AppleSkin);
        PixelRect(origin, 3, 12, 10, 1, AppleSkin);
        PixelRect(origin, 4, 13, 8, 1, AppleSkin);
        PixelRect(origin, 5, 14, 6, 1, AppleSkin);
        // Shadow on the right side
        PixelRect(origin, 11, 6, 2, 1, AppleShade);
        PixelRect(origin, 12, 7, 1, 3, AppleShade);
        PixelRect(origin, 11, 10, 2, 1, AppleShade);
        PixelRect(origin, 10, 11, 1, 2, AppleShade);
        // Highlight on the top-left
        PixelRect(origin, 4, 6, 1, 1, new Color("#e88a85"));
        PixelRect(origin, 3, 7, 1, 2, new Color("#e88a85"));
        // Stem
        PixelRect(origin, 7, 2, 1, 2, AppleStem);
        // Leaf
        PixelRect(origin, 8, 1, 4, 1, AppleLeaf);
        PixelRect(origin, 9, 2, 3, 1, AppleLeaf);
    }

    private void DrawBerries(Vector2 origin)
    {
        // Three small round berries in a triangle formation, each with a
        // different hue — clearly "a handful of foraged berries" rather
        // than the single red apple silhouette of Food.
        // Top berry (mixed warm)
        PixelRect(origin, 6, 2, 4, 1, BerryA);
        PixelRect(origin, 5, 3, 6, 1, BerryA);
        PixelRect(origin, 5, 4, 6, 1, BerryA);
        PixelRect(origin, 6, 5, 4, 1, BerryA);
        PixelRect(origin, 6, 3, 1, 1, new Color("#c95887"));
        // Bottom-left berry (warm gold)
        PixelRect(origin, 2, 8, 4, 1, BerryB);
        PixelRect(origin, 1, 9, 6, 1, BerryB);
        PixelRect(origin, 1, 10, 6, 1, BerryB);
        PixelRect(origin, 2, 11, 4, 1, BerryB);
        PixelRect(origin, 2, 9, 1, 1, new Color("#f0c260"));
        // Bottom-right berry (deep magenta)
        PixelRect(origin, 10, 8, 4, 1, BerryA);
        PixelRect(origin, 9, 9, 6, 1, BerryA);
        PixelRect(origin, 9, 10, 6, 1, BerryA);
        PixelRect(origin, 10, 11, 4, 1, BerryA);
        PixelRect(origin, 10, 9, 1, 1, new Color("#c95887"));
        // Tiny stem on the top berry
        PixelRect(origin, 7, 1, 2, 1, BerryLeaf);
        PixelRect(origin, 6, 0, 1, 1, BerryLeaf);
    }

    private void DrawIngot(Vector2 origin)
    {
        // Trapezoidal metal bar: wider at the top, narrower at the bottom,
        // with a horizontal bevel near the top. Reads as "forged metal",
        // not "stone", and not "log" (no rings).
        // Top face
        PixelRect(origin, 1, 2, 14, 1, IronLight);
        PixelRect(origin, 1, 3, 14, 1, IronLight);
        // Upper body
        PixelRect(origin, 1, 4, 14, 1, IronMid);
        PixelRect(origin, 1, 5, 14, 1, IronMid);
        PixelRect(origin, 1, 6, 14, 1, IronMid);
        // Bevel
        PixelRect(origin, 2, 7, 12, 1, IronHighlight);
        // Taper
        PixelRect(origin, 1, 8, 14, 1, IronMid);
        PixelRect(origin, 1, 9, 14, 1, IronMid);
        PixelRect(origin, 2, 10, 12, 1, IronMid);
        PixelRect(origin, 2, 11, 12, 1, IronMid);
        PixelRect(origin, 3, 12, 10, 1, IronMid);
        PixelRect(origin, 3, 13, 10, 1, IronMid);
        PixelRect(origin, 4, 14, 8, 1, IronLight);
        // Hot highlight on the top-left
        PixelRect(origin, 1, 2, 1, 2, IronHighlight);
        PixelRect(origin, 2, 3, 1, 1, IronHighlight);
    }

    private void DrawFlask(Vector2 origin)
    {
        // Apothecary flask: small stopper, narrow neck, broad round body.
        // Distinct from Food (no leaf) and from Iron (round not trapezoidal).
        // Stopper
        PixelRect(origin, 7, 1, 2, 1, PotionCork);
        PixelRect(origin, 6, 2, 4, 1, PotionCork);
        // Neck (thin, dark)
        PixelRect(origin, 7, 3, 2, 1, PotionNeck);
        PixelRect(origin, 7, 4, 2, 1, PotionNeck);
        // Shoulder transition
        PixelRect(origin, 6, 5, 4, 1, PotionNeck);
        PixelRect(origin, 5, 6, 6, 1, PotionNeck);
        // Round body
        PixelRect(origin, 3, 7, 10, 1, PotionBody);
        PixelRect(origin, 2, 8, 12, 1, PotionBody);
        PixelRect(origin, 2, 9, 12, 1, PotionBody);
        PixelRect(origin, 1, 10, 14, 1, PotionBody);
        PixelRect(origin, 1, 11, 14, 1, PotionBody);
        PixelRect(origin, 1, 12, 14, 1, PotionBody);
        PixelRect(origin, 2, 13, 12, 1, PotionBody);
        PixelRect(origin, 3, 14, 10, 1, PotionBody);
        // Shadow on the right
        PixelRect(origin, 11, 8, 1, 1, PotionNeck);
        PixelRect(origin, 12, 9, 1, 3, PotionNeck);
        PixelRect(origin, 11, 12, 1, 1, PotionNeck);
        // Liquid highlight
        PixelRect(origin, 3, 7, 1, 1, PotionShine);
        PixelRect(origin, 2, 8, 1, 3, PotionShine);
        PixelRect(origin, 3, 11, 1, 1, PotionShine);
        PixelRect(origin, 3, 12, 1, 1, PotionShine);
    }

    private void DrawFallback(Vector2 origin)
    {
        // Last-resort neutral marker for any future ResourceType the icon
        // does not yet know about. The lineage accent frames a cream
        // question mark so an unstyled resource is never invisible.
        PixelRect(origin, 4, 4, 8, 8, LineageThemeRegistry.IconAccent);
        PixelRect(origin, 6, 6, 4, 4, new Color("#f0e5c8"));
    }

    // ── Drawing primitives ────────────────────────────────────────────────
    //
    // Integer-aligned rectangles only. Antialiasing is explicitly disabled
    // on lines to keep strokes pixel-exact at the 24 px cell the HUD
    // reserves for an inline icon. The HUD ticker modulates the whole
    // Control with the active lineage accent, so colours here stay in the
    // neutral material range (grey for stone, brown for wood, etc.).

    private void PixelRect(Vector2 origin, int x, int y, int width, int height, Color color) =>
        DrawRect(new Rect2(origin + new Vector2(x, y), new Vector2(width, height)), color);

    private void PixelLine(
        Vector2 origin,
        Vector2 from,
        Vector2 to,
        Color color,
        float width) =>
        DrawLine(origin + from, origin + to, color, width, antialiased: false);
}
