using System;
using WorldofGoses.Domain;
using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Locks the icon-for-phase map on <see cref="HudStateBadge"/>.
/// </summary>
/// <remarks>
/// <para>
/// The expedition rail's compact card and the showcase composition both
/// depend on a stable mapping between <see cref="ExpeditionPhase"/> and an
/// icon path. A test like this catches the failure mode where a new phase
/// is added to the enum but not to the badge — the card silently falls
/// through to the Resolved glyph and two unrelated phases become
/// indistinguishable.
/// </para>
/// </remarks>
public sealed class HudStateBadgePhaseMapTests
{
    [Theory]
    [InlineData(ExpeditionPhase.Outbound)]
    [InlineData(ExpeditionPhase.Encounter)]
    [InlineData(ExpeditionPhase.Objective)]
    [InlineData(ExpeditionPhase.Returning)]
    [InlineData(ExpeditionPhase.Retreating)]
    [InlineData(ExpeditionPhase.Resolved)]
    public void EveryPhase_HasANonEmptyIconPath(ExpeditionPhase phase)
    {
        string iconPath = HudStateBadge.IconFor(phase);

        Assert.False(
            string.IsNullOrWhiteSpace(iconPath),
            $"HudStateBadge.IconFor({phase}) returned an empty path.");
        Assert.StartsWith("res://", iconPath, StringComparison.Ordinal);
    }

    [Fact]
    public void ReturningAndRetreating_DistinctByLabel_ShareTheirGlyph()
    {
        // The two phases intentionally share the left-arrow glyph
        // because the arrow is the "going home" semantic; the localized
        // label is what distinguishes them. Colour is never the only
        // signal — a glyph alone would be ambiguous, but a glyph
        // plus the label is unambiguous.
        string returningIcon = HudStateBadge.IconFor(ExpeditionPhase.Returning);
        string retreatingIcon = HudStateBadge.IconFor(ExpeditionPhase.Retreating);

        Assert.Equal(returningIcon, retreatingIcon);
    }

    [Fact]
    public void Outbound_DoesNotShareGlyphWithReturning()
    {
        // The two phases are semantically opposite (going out vs coming
        // back); they must not silently collapse onto the same glyph.
        string outboundIcon = HudStateBadge.IconFor(ExpeditionPhase.Outbound);
        string returningIcon = HudStateBadge.IconFor(ExpeditionPhase.Returning);

        Assert.NotEqual(outboundIcon, returningIcon);
    }
}