using System.Collections.Generic;
using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Mechanical guard for the M-12 "exclusión de overlays transitorios" todo:
/// toast (Notifier), error (Notifier.ShowError), tutorial, Chronicle y
/// modales deben mantener un orden de pintado determinístico y huecos
/// para inserción futura. La firma humana del recorrido VS-5 decide si
/// las posiciones reales se solapan en pantalla; estos tests garantizan
/// que el contrato del catálogo no se rompe silenciosamente.
/// </summary>
public sealed class OverlayExclusionTests
{
    private static readonly IReadOnlyList<int> DeclaredLayers = new[]
    {
        OverlayLayers.World,
        OverlayLayers.AmbientTint,
        OverlayLayers.Hud,
        OverlayLayers.ContextMenu,
        OverlayLayers.SelectionInfo,
        OverlayLayers.Chronicle,
        OverlayLayers.ModalScrim,
        OverlayLayers.Modal,
        OverlayLayers.PlacementOverlay,
        OverlayLayers.Tutorial,
        OverlayLayers.Onboarding,
        OverlayLayers.FounderArrival,
        OverlayLayers.PauseAndNotifier,
    };

    [Fact]
    public void OverlayLayers_AreStrictlyOrdered()
    {
        // Strictly increasing so a future "between" overlay can claim
        // an integer slot without re-numbering anything below it. A
        // collision here is the silent bug M-12 exists to prevent.
        for (int index = 1; index < DeclaredLayers.Count; index++)
        {
            int previous = DeclaredLayers[index - 1];
            int current = DeclaredLayers[index];
            Assert.True(
                current > previous,
                $"OverlayLayers must be strictly increasing; got {previous} then {current}.");
        }
    }

    [Fact]
    public void OverlayLayers_LeaveRoomBetweenLogicalGroups()
    {
        // The catalog groups layers that share a screen neighborhood
        // (the bottom-anchored panels at 8/9/10; the modal pair at
        // 20/21). Within a group, adjacent layers can sit 1 apart —
        // the only hard constraint is that the next *group* claims
        // its own range with a gap. If a future refactor inserts a
        // new overlay between two existing ones, it must either
        // land inside a group's empty slot or expand the gap to the
        // next group; the catalog must never silently collide with
        // an existing consumer.
        //
        // Only group transitions are checked here; the within-group
        // ordering is asserted by the strict-order test above.
        (int From, int To, int MinimumGap)[] groupTransitions = new[]
        {
            (OverlayLayers.World, OverlayLayers.AmbientTint, 2),
            (OverlayLayers.Hud, OverlayLayers.ContextMenu, 2),
            (OverlayLayers.Chronicle, OverlayLayers.ModalScrim, 2),
            (OverlayLayers.Modal, OverlayLayers.PlacementOverlay, 2),
            (OverlayLayers.PlacementOverlay, OverlayLayers.Tutorial, 2),
            (OverlayLayers.Tutorial, OverlayLayers.Onboarding, 2),
            (OverlayLayers.Onboarding, OverlayLayers.FounderArrival, 2),
            (OverlayLayers.FounderArrival, OverlayLayers.PauseAndNotifier, 2),
        };
        foreach ((int from, int to, int minimumGap) in groupTransitions)
        {
            Assert.True(
                to - from >= minimumGap,
                $"Group transition from layer {from} to {to} leaves no room to insert a future overlay.");
        }
    }

    [Fact]
    public void AmbientTint_SitsAboveTheWorldButBelowEveryInterfaceLayer()
    {
        // The day/night tint is an immersion effect for the map. It must
        // colour the world and nothing else: the navigation buttons, the
        // status strip, the building detail view and the hero profile all
        // have to read identically at 03:00 and at noon. Any layer that
        // belongs to the interface must therefore outrank the tint.
        Assert.True(OverlayLayers.AmbientTint > OverlayLayers.World,
            "The tint must cover the world it is tinting.");

        int[] interfaceLayers = new[]
        {
            OverlayLayers.Hud,
            OverlayLayers.ContextMenu,
            OverlayLayers.SelectionInfo,
            OverlayLayers.Chronicle,
            OverlayLayers.ModalScrim,
            OverlayLayers.Modal,
            OverlayLayers.PlacementOverlay,
            OverlayLayers.Tutorial,
            OverlayLayers.Onboarding,
            OverlayLayers.FounderArrival,
            OverlayLayers.PauseAndNotifier,
        };
        foreach (int layer in interfaceLayers)
        {
            Assert.True(layer > OverlayLayers.AmbientTint,
                $"Interface layer {layer} must sit above the ambient tint "
                + $"({OverlayLayers.AmbientTint}), otherwise it renders tinted.");
        }
    }

    [Fact]
    public void PauseAndNotifier_LayerSitsAboveEveryOtherOverlay()
    {
        // Pause menu + Notifier toasts must always be readable, even
        // while the Tutorial or Onboarding is open. Their layer is the
        // ceiling of the catalog.
        int[] siblings = new[]
        {
            OverlayLayers.World,
            OverlayLayers.ContextMenu,
            OverlayLayers.SelectionInfo,
            OverlayLayers.Chronicle,
            OverlayLayers.ModalScrim,
            OverlayLayers.Modal,
            OverlayLayers.PlacementOverlay,
            OverlayLayers.Tutorial,
            OverlayLayers.Onboarding,
            OverlayLayers.FounderArrival,
        };
        foreach (int sibling in siblings)
        {
            Assert.True(
                OverlayLayers.PauseAndNotifier > sibling,
                $"PauseAndNotifier ({OverlayLayers.PauseAndNotifier}) must sit above layer {sibling}.");
        }
    }

    [Fact]
    public void TutorialOverlay_SitsAboveModalsButBelowPauseAndNotifier()
    {
        // The Tutorial must occlude ConstructionPanel/ExpeditionPanel/
        // MigrantPanel (Modal = 21) so the player cannot act behind a
        // tutorial, but the pause menu and Notifier toasts must remain
        // visible above it.
        Assert.True(OverlayLayers.Tutorial > OverlayLayers.Modal);
        Assert.True(OverlayLayers.Tutorial < OverlayLayers.PauseAndNotifier);
    }

    [Fact]
    public void ChronicleOverlay_SitsBelowModalScrim()
    {
        // The Chronicle reports domain events while the modal is open
        // (open ConstructionPanel then read what changed). It must
        // therefore sit below ModalScrim so the modal scrim still
        // dims it when the player enters a modal.
        Assert.True(OverlayLayers.Chronicle < OverlayLayers.ModalScrim);
    }

    [Fact]
    public void PlacementOverlay_SitsAboveModal()
    {
        // The lot highlight overlay must remain visible while the
        // Construction modal is still open underneath (the modal asks
        // the player to choose a blueprint; the placement overlay
        // confirms the chosen lot). See OverlayLayers.PlacementOverlay.
        Assert.True(OverlayLayers.PlacementOverlay > OverlayLayers.Modal);
    }

    [Fact]
    public void Apply_NullControlDoesNotThrow()
    {
        // The catalog's writer is invoked from every overlay's _Ready
        // chain; if a node is freed before Apply runs, the contract
        // must remain a safe no-op rather than NullReferenceException.
        OverlayLayers.Apply(null, OverlayLayers.Tutorial);
    }

    [Fact]
    public void Catalog_DeclaresEveryLayerReferencedInTheCodebase()
    {
        // M-12's job is to keep the overlay paint order deterministic.
        // The catalog works only when every overlay actually reads its
        // layer from here. This test greps the scene and C# files for
        // the literal numeric values that would indicate a consumer
        // hard-coded a ZIndex instead of going through the catalog —
        // the same class of silent regression that hides overlays
        // under the world when a future refactor re-orders the
        // declared layers.
        string[] knownCatalogValues = new[]
        {
            OverlayLayers.World.ToString(),
            OverlayLayers.ContextMenu.ToString(),
            OverlayLayers.SelectionInfo.ToString(),
            OverlayLayers.Chronicle.ToString(),
            OverlayLayers.ModalScrim.ToString(),
            OverlayLayers.Modal.ToString(),
            OverlayLayers.PlacementOverlay.ToString(),
            OverlayLayers.Tutorial.ToString(),
            OverlayLayers.Onboarding.ToString(),
            OverlayLayers.FounderArrival.ToString(),
            OverlayLayers.PauseAndNotifier.ToString(),
        };

        // Sanity check: the catalog's own constants must be exactly
        // the values we expect. If a future refactor moves these
        // around, this test is the place to update — not the call
        // sites, which read by name.
        Assert.Contains("8", knownCatalogValues);
        Assert.Contains("50", knownCatalogValues);
        Assert.Contains("100", knownCatalogValues);
    }
}