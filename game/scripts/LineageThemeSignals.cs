#nullable enable
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Autoload that bridges the domain's <see cref="LineageId"/>
/// selection with the visual <see cref="LineageThemeRegistry"/>.
/// Exposes one Godot signal so UI nodes can refresh their StyleBox
/// overrides when the founder's lineage changes.
/// </summary>
public partial class LineageThemeSignals : Node
{
    public const string AutoloadName = "LineageThemeSignals";

    [Signal] public delegate void LineageChangedEventHandler(string lineageThemeId);

    public override void _Ready()
    {
        LineageThemeRegistry.ActiveLineageChanged += OnLineageChanged;
        LineageThemeRegistry.FallbackUsed += OnFallbackUsed;
    }

    public override void _ExitTree()
    {
        LineageThemeRegistry.ActiveLineageChanged -= OnLineageChanged;
        LineageThemeRegistry.FallbackUsed -= OnFallbackUsed;
    }

    public void ApplyLineage(LineageId lineage)
    {
        LineageThemeRegistry.ActiveLineage = LineageThemeRegistry.IdOf(lineage);
    }

    private void OnLineageChanged(string lineage)
    {
        EmitSignal(SignalName.LineageChanged, lineage);
    }

    private static void OnFallbackUsed(string message) => GD.PushWarning(message);
}
