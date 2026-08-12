#nullable enable

using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// The line a HUD section shows when it has nothing to list — "no resources
/// stored", "nothing under construction".
/// </summary>
/// <remarks>
/// <para>
/// It wraps rather than clips. Every other line in these panels is a metric
/// with a known short value and truncates on overflow; this one is a sentence,
/// and a sentence cut to "no hay nada en constr…" reads as a rendering bug
/// rather than as an empty list. <see cref="TextServer.AutowrapMode.WordSmart"/>
/// is what makes the Spanish strings, which run longer than the English ones,
/// stay legible in a 240 px column.
/// </para>
/// <para>
/// It takes a localisation key rather than a string so the call site cannot
/// pass an already-resolved literal that a locale switch would then leave
/// stale — the panel rebuilds its body on <c>LocaleChanged</c>, and reading the
/// catalogue at construction is what makes that rebuild sufficient.
/// </para>
/// </remarks>
[GlobalClass]
public partial class HudEmptyState : Label
{
    public HudEmptyState(string textKey)
    {
        Text = UiText.Get(textKey);
        ThemeTypeVariation = "HudCaption";
        AutowrapMode = TextServer.AutowrapMode.WordSmart;
        MouseFilter = MouseFilterEnum.Ignore;
    }
}
