using Godot;

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
    private const string CursorPath = "res://assets/ui/icons/24/cursor.svg";
    private static readonly Vector2 CursorHotspot = new(2, 2);

    public override void _Ready()
    {
        LineageThemeRegistry.ActiveLineageChanged += OnLineageChanged;
        ApplyCursor();
    }

    public override void _ExitTree()
    {
        LineageThemeRegistry.ActiveLineageChanged -= OnLineageChanged;
    }

    private void OnLineageChanged(string lineage) => ApplyCursor();

    private void ApplyCursor()
    {
        var source = ResourceLoader.Load<Texture2D>(CursorPath);
        if (source is null)
        {
            GD.PushWarning($"CursorController: cursor image missing at '{CursorPath}'.");
            return;
        }

        var tintedImage = source.GetImage();
        if (tintedImage is null)
        {
            GD.PushWarning($"CursorController: could not read image data from '{CursorPath}'.");
            return;
        }

        var accent = LineageThemeRegistry.IconAccent;
        // The SVG ships with a white fill; multiplying each pixel by
        // the linaje accent bakes the linaje colour into the cursor
        // without modifying the source asset on disk.
        tintedImage.Convert(Image.Format.Rgba8);
        for (int y = 0; y < tintedImage.GetHeight(); y++)
        {
            for (int x = 0; x < tintedImage.GetWidth(); x++)
            {
                Color px = tintedImage.GetPixel(x, y);
                if (px.A <= 0f) continue;
                tintedImage.SetPixel(x, y, new Color(
                    px.R * accent.R,
                    px.G * accent.G,
                    px.B * accent.B,
                    px.A));
            }
        }

        var tintedTexture = ImageTexture.CreateFromImage(tintedImage);
        Input.SetCustomMouseCursor(tintedTexture, Input.CursorShape.Arrow, CursorHotspot);
    }
}