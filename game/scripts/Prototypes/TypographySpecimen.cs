using Godot;

namespace WorldofGoses.Prototypes;

/// <summary>Read-only visual specimen for pixel-perfect font regression.</summary>
public sealed partial class TypographySpecimen : Control
{
    private const string OutputArgumentPrefix = "--wog-typography-output=";
    private int _renderedFrames;

    public override void _Process(double delta)
    {
        if (++_renderedFrames < 4) return;

        string outputPath = FindOutputPath();
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            SetProcess(false);
            return;
        }

        Image image = GetViewport().GetTexture().GetImage();
        Error result = image.SavePng(outputPath);
        GD.Print($"[WOG-TYPOGRAPHY-CAPTURE] {image.GetWidth()}x{image.GetHeight()} -> {outputPath}");
        if (result != Error.Ok)
        {
            GD.PushError($"Typography capture failed with {result}: {outputPath}");
        }
        SetProcess(false);
    }

    private static string FindOutputPath()
    {
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (argument.StartsWith(OutputArgumentPrefix, System.StringComparison.Ordinal))
            {
                return argument[OutputArgumentPrefix.Length..];
            }
        }
        return string.Empty;
    }
}
