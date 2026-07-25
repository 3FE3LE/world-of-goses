#nullable enable
using Godot;
using WorldofGoses.Ui;

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
    private CursorController _cursorController = null!;

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
        _cursorController = GetNode<CursorController>("/root/CursorController");
        IgnoreTextureSize = true;
        StretchMode = StretchModeEnum.Scale;
        FocusMode = FocusModeEnum.All;
        Pressed += OnPressed;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        FocusEntered += OnFocusEntered;
    }

    public override void _ExitTree()
    {
        Pressed -= OnPressed;
        MouseEntered -= OnMouseEntered;
        MouseExited -= OnMouseExited;
        FocusEntered -= OnFocusEntered;
        if (IsInstanceValid(_cursorController))
        {
            _cursorController.RestoreSurfaceCursor();
        }
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

    private void OnMouseEntered()
    {
        _cursorController.UseGatherCursor();
        UiMotion.Pulse(this, LineageThemeRegistry.IconAccent);
    }

    private void OnMouseExited() => _cursorController.RestoreSurfaceCursor();

    private void OnFocusEntered() =>
        UiMotion.Pulse(this, LineageThemeRegistry.IconAccent);

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
