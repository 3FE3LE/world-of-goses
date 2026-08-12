#nullable enable

using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// The heading that names a section inside a list column — "Assigned",
/// "Available", "Unavailable".
/// </summary>
/// <remarks>
/// Lighter than <see cref="HudSectionHeader"/> on purpose: that one is a
/// recessed strip with its own surface and a trailing count, which is right
/// for a panel's top-level sections and too heavy for a column that already
/// sits inside one.
/// </remarks>
[GlobalClass]
public partial class HudListHeading : Label
{
    public HudListHeading(string text)
    {
        Text = text;
        ThemeTypeVariation = "HudLabel";
    }
}

/// <summary>
/// A wrapped caption line inside a list column — the "nobody is available"
/// kind of sentence a section shows in place of rows.
/// </summary>
/// <remarks>
/// Wraps rather than clips, for the reason <see cref="HudEmptyState"/> gives:
/// a sentence cut mid-word reads as a rendering bug rather than as an empty
/// list. It takes resolved text instead of a key because its callers compose
/// the sentence from snapshot values; where the whole line is a catalogue
/// entry, <see cref="HudEmptyState"/> is the one to reach for, since taking
/// the key is what stops a locale switch leaving it stale.
/// </remarks>
[GlobalClass]
public partial class HudListCaption : Label
{
    public HudListCaption(string text)
    {
        Text = text;
        ThemeTypeVariation = "HudCaption";
        AutowrapMode = TextServer.AutowrapMode.WordSmart;
    }
}
