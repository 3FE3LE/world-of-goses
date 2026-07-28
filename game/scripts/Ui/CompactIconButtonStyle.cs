#nullable enable
using Godot;

namespace WorldofGoses.Ui;

/// <summary>Applies compact, local padding to a button that contains no text.</summary>
public static class CompactIconButtonStyle
{
    private const float Padding = 2f;
    private static readonly StringName[] StyleNames =
    {
        "normal", "hover", "pressed", "disabled",
    };

    public static void Apply(Button button)
    {
        foreach (StringName styleName in StyleNames)
        {
            StyleBox source = button.GetThemeStylebox(styleName);
            var compact = (StyleBox)source.Duplicate();
            compact.ContentMarginLeft = Padding;
            compact.ContentMarginTop = Padding;
            compact.ContentMarginRight = Padding;
            compact.ContentMarginBottom = Padding;
            button.AddThemeStyleboxOverride(styleName, compact);
        }
    }
}
