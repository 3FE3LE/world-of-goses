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
            case ResourceType.Branches:
                PixelLine(origin, new Vector2(2, 12), new Vector2(13, 4), new Color("#8f5b32"), 2);
                PixelLine(origin, new Vector2(4, 5), new Vector2(12, 12), new Color("#b87942"), 2);
                break;
            case ResourceType.PlantFiber:
                PixelRect(origin, 7, 3, 2, 11, new Color("#4f7f35"));
                PixelRect(origin, 3, 5, 5, 3, new Color("#73a942"));
                PixelRect(origin, 8, 8, 5, 3, new Color("#8fc35b"));
                break;
            case ResourceType.SmallStone:
            case ResourceType.Stone:
                PixelRect(origin, 3, 8, 10, 5, new Color("#777f84"));
                PixelRect(origin, 5, 5, 6, 3, new Color("#aeb4b6"));
                PixelRect(origin, 5, 8, 3, 2, new Color("#d0d4d3"));
                break;
            case ResourceType.WildFood:
            case ResourceType.Food:
                PixelRect(origin, 4, 7, 4, 4, new Color("#c4514f"));
                PixelRect(origin, 9, 8, 4, 4, new Color("#d9a441"));
                PixelRect(origin, 7, 3, 2, 5, new Color("#568b3d"));
                break;
            case ResourceType.Wood:
                PixelRect(origin, 2, 6, 12, 6, new Color("#8f5b32"));
                PixelRect(origin, 3, 7, 8, 2, new Color("#bd7d45"));
                PixelRect(origin, 11, 7, 2, 4, new Color("#d0a36a"));
                break;
            case ResourceType.Iron:
                PixelRect(origin, 3, 6, 10, 7, new Color("#59626d"));
                PixelRect(origin, 5, 4, 6, 3, new Color("#919ba5"));
                PixelRect(origin, 5, 7, 5, 2, new Color("#c1c7ca"));
                break;
            default:
                PixelRect(origin, 4, 4, 8, 8, LineageThemeRegistry.IconAccent);
                PixelRect(origin, 6, 6, 4, 4, new Color("#f0e5c8"));
                break;
        }
    }

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
