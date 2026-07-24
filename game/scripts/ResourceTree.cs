#nullable enable
using Godot;

namespace WorldofGoses;

/// <summary>Interactive visual unit for a Forest reserve.</summary>
public partial class ResourceTree : TextureButton
{
    private const string TerrainAtlasPath =
        "res://assets/terrain/kenney/roguelike-rpg/roguelike_sheet_transparent.png";
    public const string AxeCursorPath =
        "res://assets/ui/cursors/kenney-pixel/axe.png";
    private const int SourceTileSize = 16;
    private const int SourceStride = 17;

    [Signal]
    public delegate void ResourcePressedEventHandler(
        int forestId,
        int unitId,
        Vector2 globalPosition);

    public int ForestId { get; private set; }
    public int UnitId { get; private set; }

    public void Configure(int forestId, int unitId, int visualVariant)
    {
        ForestId = forestId;
        UnitId = unitId;
        Texture2D atlas = GD.Load<Texture2D>(TerrainAtlasPath);
        TextureNormal = CreateRegion(atlas, visualVariant % 2 == 0 ? 13 : 14, 9);
        TextureHover = TextureNormal;
        TooltipText = "Wood resource — click for actions";
    }

    public override void _Ready()
    {
        IgnoreTextureSize = true;
        StretchMode = StretchModeEnum.Scale;
        FocusMode = FocusModeEnum.All;
        Pressed += OnPressed;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    public override void _ExitTree()
    {
        Pressed -= OnPressed;
        MouseEntered -= OnMouseEntered;
        MouseExited -= OnMouseExited;
        Input.SetCustomMouseCursor(null, Input.CursorShape.Arrow);
    }

    private void OnPressed() =>
        EmitSignal(
            SignalName.ResourcePressed,
            ForestId,
            UnitId,
            GlobalPosition + Size * 0.5f);

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouse
            || mouse.ButtonIndex != MouseButton.Right
            || !mouse.Pressed)
        {
            return;
        }
        OnPressed();
        AcceptEvent();
    }

    private static void OnMouseEntered()
    {
        Texture2D cursor = GD.Load<Texture2D>(AxeCursorPath);
        Input.SetCustomMouseCursor(cursor, Input.CursorShape.Arrow, new Vector2(3, 13));
    }

    private static void OnMouseExited() =>
        Input.SetCustomMouseCursor(null, Input.CursorShape.Arrow);

    internal static AtlasTexture CreateRegion(Texture2D atlas, int column, int row) =>
        new()
        {
            Atlas = atlas,
            Region = new Rect2(
                column * SourceStride,
                row * SourceStride,
                SourceTileSize,
                SourceTileSize),
        };
}
