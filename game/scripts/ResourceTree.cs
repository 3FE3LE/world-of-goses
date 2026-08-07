#nullable enable
using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>Interactive visual unit for a Forest reserve.</summary>
public partial class ResourceTree : TextureButton
{
    internal const string TerrainAtlasPath = TerrainAtlas.AtlasPath;
    public const string AxeCursorPath =
        "res://assets/ui/cursors/kenney-pixel/axe.png";

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
        TextureNormal = CreateRegion(atlas, TerrainAtlas.TreeRegion(visualVariant, 0));
        TextureHover = TextureNormal;
        TooltipText = UiText.Get("Wood resource — click for actions");
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
        // ResourceTree is the tree widget specifically, so the tool is the axe.
        _cursorController.UseGatherCursor(WorldofGoses.Domain.ResourceType.Wood);
        UiMotion.Pulse(this, LineageThemeRegistry.IconAccent);
    }

    private void OnMouseExited() => _cursorController.RestoreSurfaceCursor();

    private void OnFocusEntered() =>
        UiMotion.Pulse(this, LineageThemeRegistry.IconAccent);

    internal static AtlasTexture CreateRegion(Texture2D atlas, int column, int row) =>
        CreateRegion(atlas, TerrainAtlas.Region(column, row));

    /// <summary>Wraps an already-resolved atlas region as a texture.</summary>
    internal static AtlasTexture CreateRegion(Texture2D atlas, Rect2 region) =>
        new()
        {
            Atlas = atlas,
            Region = region,
        };
}
