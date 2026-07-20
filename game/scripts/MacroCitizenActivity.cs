using Godot;

namespace WorldofGoses;

/// <summary>
/// Small markers representing the world's actual citizens in the macro view.
/// They are not yet individually interactive; their count is derived from the
/// domain rather than from a decorative fixed fixture.
/// </summary>
public partial class MacroCitizenActivity : Node2D
{
    /// <summary>
    /// (Re)builds the macro activity dots. Deterministic: the positions
    /// are derived from the index, not from random numbers.
    /// </summary>
    public void Populate(int citizenCount)
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

        // Lay the real population in a gentle arc around the city centre.
        for (int i = 0; i < citizenCount; i++)
        {
            int denominator = Mathf.Max(citizenCount, 1);
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
        }
    }
}