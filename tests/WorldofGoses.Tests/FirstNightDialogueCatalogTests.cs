using System;
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// The first-night catalog (<c>Domain/FireSpiritDialogueCatalog.cs</c>) is
/// the only source of ids and translation keys the night's
/// non-modal dialogue strip resolves against. It must therefore:
/// <list type="bullet">
///   <item>Expose one stable id per stage that opens a main-dialogue node.</item>
///   <item>Carry a body variant for every <see cref="LineageId"/> the
///         project supports, with no missing-key fallbacks at runtime.</item>
///   <item>Keep the id constant across lineages — variation lives in the
///         body, not in the route (doc 19 §13–14).</item>
///   <item>Recognise the stages that wait on a module (no dialogue yet)
///         and the absorbing <c>Concluded</c> stage as <c>null</c>.</item>
/// </list>
/// </summary>
public sealed class FirstNightDialogueCatalogTests
{
    [Fact]
    public void CatalogExposesExactlySixStableNodeIds()
    {
        // Manifested, SpiritArrived, CampfireBuilt, ShelterBuilt, OtherLightTold, Sleeping.
        // ColdExplained and ShelterExplained wait on a module; Concluded is absorbing.
        Assert.Equal(6, FireSpiritDialogueCatalog.NodeIds.Count);
        Assert.Contains(FireSpiritDialogueCatalog.ManifestedGreetingId, FireSpiritDialogueCatalog.NodeIds);
        Assert.Contains(FireSpiritDialogueCatalog.SpiritArrivedId, FireSpiritDialogueCatalog.NodeIds);
        Assert.Contains(FireSpiritDialogueCatalog.CampfireBuiltId, FireSpiritDialogueCatalog.NodeIds);
        Assert.Contains(FireSpiritDialogueCatalog.ShelterBuiltId, FireSpiritDialogueCatalog.NodeIds);
        Assert.Contains(FireSpiritDialogueCatalog.OtherLightToldId, FireSpiritDialogueCatalog.NodeIds);
        Assert.Contains(FireSpiritDialogueCatalog.SleepingId, FireSpiritDialogueCatalog.NodeIds);
    }

    [Fact]
    public void CatalogExposesAllEightLineages()
    {
        Assert.Equal(8, FireSpiritDialogueCatalog.Lineages.Count);
        Assert.Contains(LineageId.Ardhen, FireSpiritDialogueCatalog.Lineages);
        Assert.Contains(LineageId.Eirune, FireSpiritDialogueCatalog.Lineages);
        Assert.Contains(LineageId.Kovari, FireSpiritDialogueCatalog.Lineages);
        Assert.Contains(LineageId.Myrven, FireSpiritDialogueCatalog.Lineages);
        Assert.Contains(LineageId.Vaelun, FireSpiritDialogueCatalog.Lineages);
        Assert.Contains(LineageId.Orveth, FireSpiritDialogueCatalog.Lineages);
        Assert.Contains(LineageId.Caelith, FireSpiritDialogueCatalog.Lineages);
        Assert.Contains(LineageId.Theryn, FireSpiritDialogueCatalog.Lineages);
    }

    [Theory]
    [MemberData(nameof(Lineages))]
    public void EveryNodeHasABodyKeyForEveryLineage(LineageId lineage)
    {
        foreach (string nodeId in FireSpiritDialogueCatalog.NodeIds)
        {
            string bodyKey = FireSpiritDialogueCatalog.BodyKeyFor(nodeId, lineage);
            Assert.False(
                string.IsNullOrWhiteSpace(bodyKey),
                $"Body key for ({nodeId}, {lineage.Value}) is empty.");
            Assert.StartsWith("firstnight.", bodyKey);
        }
    }

    [Fact]
    public void NodeForDialogueStagesReturnsANode()
    {
        foreach (FirstNightStage stage in DialogueStages())
        {
            foreach (LineageId lineage in FireSpiritDialogueCatalog.Lineages)
            {
                IDialogueNode? node = FireSpiritDialogueCatalog.NodeFor(stage, lineage);
                Assert.NotNull(node);
                Assert.Equal(FireSpiritDialogueCatalog.FireSpiritSpeakerId, node!.SpeakerId);
                Assert.False(string.IsNullOrWhiteSpace(node.BodyKey));
            }
        }
    }

    [Theory]
    [InlineData(FirstNightStage.ColdExplained)]
    [InlineData(FirstNightStage.ShelterExplained)]
    [InlineData(FirstNightStage.Concluded)]
    public void NodeForModuleOrConcludedStagesReturnsNull(FirstNightStage stage)
    {
        // ColdExplained and ShelterExplained wait on a module — no dialogue to render.
        // Concluded is absorbing — the strip is gone.
        IDialogueNode? node = FireSpiritDialogueCatalog.NodeFor(stage, LineageId.Ardhen);
        Assert.Null(node);
    }

    [Fact]
    public void NodeIdDoesNotChangeAcrossLineages()
    {
        // The route is strictly linear; the spirit reacts to the lineage via
        // body text only, never via different node ids (doc 19 §13–14).
        foreach (FirstNightStage stage in DialogueStages())
        {
            string? referenceId = null;
            foreach (LineageId lineage in FireSpiritDialogueCatalog.Lineages)
            {
                IDialogueNode? node = FireSpiritDialogueCatalog.NodeFor(stage, lineage);
                Assert.NotNull(node);
                if (referenceId is null)
                {
                    referenceId = node!.Id;
                }
                else
                {
                    Assert.Equal(referenceId, node!.Id);
                }
            }
        }
    }

    [Fact]
    public void UnknownLineageFallsBackToTheArdhenVariant()
    {
        // A save whose lineage is corrupt or from a future build must not
        // crash the UI: the catalog resolves to the Ardhen body instead of
        // throwing on a missing lineage entry.
        var unknown = new LineageId("atlantis");
        foreach (string nodeId in FireSpiritDialogueCatalog.NodeIds)
        {
            string fallback = FireSpiritDialogueCatalog.BodyKeyFor(nodeId, LineageId.Ardhen);
            string unknownResult = FireSpiritDialogueCatalog.BodyKeyFor(nodeId, unknown);
            Assert.Equal(fallback, unknownResult);
        }
    }

    [Fact]
    public void UnknownNodeIdThrowsSoACorruptSaveCannotSilentlyRender()
    {
        // The opposite failure mode: a hand-edited save with a bogus node id
        // must not render as if everything were fine. The catalog throws so
        // the controller can fall back to closing the open dialogue and
        // leaving the player on the world, not on a frozen line.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FireSpiritDialogueCatalog.BodyKeyFor("firstnight.nonexistent", LineageId.Ardhen));
    }

    [Fact]
    public void IsKnownIdAcceptsCatalogIdsAndRejectsOthers()
    {
        foreach (string nodeId in FireSpiritDialogueCatalog.NodeIds)
        {
            Assert.True(FireSpiritDialogueCatalog.IsKnownId(nodeId));
        }
        Assert.False(FireSpiritDialogueCatalog.IsKnownId(null));
        Assert.False(FireSpiritDialogueCatalog.IsKnownId(""));
        Assert.False(FireSpiritDialogueCatalog.IsKnownId("firstnight.unknown"));
    }

    [Fact]
    public void ChoicesAndNextAreAlwaysEmptyOrNull()
    {
        // The route is linear. Persisting a choice target or a follow-up node
        // would break the stage-driven advance from CityWorld.TryAdvance and
        // contradict doc 19 invariant 11 (no permanent mission list).
        foreach (FirstNightStage stage in DialogueStages())
        {
            IDialogueNode? node = FireSpiritDialogueCatalog.NodeFor(stage, LineageId.Ardhen);
            Assert.NotNull(node);
            Assert.Empty(node!.Choices);
            Assert.Null(node.Next);
        }
    }

    public static IEnumerable<object[]> Lineages =>
        FireSpiritDialogueCatalog.Lineages.Select(lineage => new object[] { lineage });

    private static IEnumerable<FirstNightStage> DialogueStages() => new[]
    {
        FirstNightStage.Manifested,
        FirstNightStage.SpiritArrived,
        FirstNightStage.CampfireBuilt,
        FirstNightStage.ShelterBuilt,
        FirstNightStage.OtherLightTold,
        FirstNightStage.Sleeping,
    };
}
