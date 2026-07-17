using Godot;

namespace WorldofGoses;

/// <summary>
/// Very small placeholder dots that represent city activity in the macro
/// view. The dots are intentionally tiny (see
/// <see cref="PresentationConstants.MacroCitizenSize"/>) and exist only
/// to communicate that the city has movement; they are not individually
/// interactive and do not correspond to specific citizens.
/// </summary>
public partial class MacroCitizenActivity : Node2D
{
    public override void _Ready()
    {
        Populate();
    }

    /// <summary>
    /// (Re)builds the macro activity dots. Deterministic: the positions
    /// are derived from the index, not from random numbers.
    /// </summary>
    public void Populate()
    {
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }

        var parentSize = ((Control)GetParent()).Size;
        if (parentSize == Vector2.Zero)
        {
            parentSize = new Vector2(
                PresentationConstants.CanvasWidth,
                PresentationConstants.CanvasHeight);
        }

        // Lay the dots in a gentle arc above and around the mine plot.
        // Positions are computed from a stable formula so the macro
        // view is identical across runs.
        for (int i = 0; i < PresentationConstants.MacroActivityDotCount; i++)
        {
            float angle = Mathf.Pi * (0.15f + 0.7f * (i / (float)PresentationConstants.MacroActivityDotCount));
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
}