#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Four visual vanguard positions. It deliberately does not read or change
/// <c>ExpeditionRequest.MaxTeamSize</c>.
/// </summary>
[GlobalClass]
public partial class ExpeditionSquadStrip : PanelContainer
{
    private ExpeditionSquadSlot[] _slots = Array.Empty<ExpeditionSquadSlot>();

    public IReadOnlyList<ExpeditionSquadSlot> Slots => _slots;

    public override void _Ready()
    {
        _slots = new[]
        {
            GetNode<ExpeditionSquadSlot>("Content/Slots/Slot1"),
            GetNode<ExpeditionSquadSlot>("Content/Slots/Slot2"),
            GetNode<ExpeditionSquadSlot>("Content/Slots/Slot3"),
            GetNode<ExpeditionSquadSlot>("Content/Slots/Slot4"),
        };

        _slots[0].Configure(1, ExpeditionSquadSlot.SlotState.Empty);
        for (int i = 1; i < _slots.Length; i++)
        {
            _slots[i].Configure(i + 1, ExpeditionSquadSlot.SlotState.Locked);
        }
        WireFocus();
    }

    public void ConfigureFounderFixture(
        Texture2D? portrait,
        string founderName,
        double? hpRatio,
        string? secondaryName = null,
        double secondaryRatio = 0,
        string? criticalState = null)
    {
        _slots[0].Configure(
            1,
            ExpeditionSquadSlot.SlotState.Active,
            portrait,
            founderName,
            hpRatio,
            secondaryName,
            secondaryRatio,
            criticalState);
        for (int i = 1; i < _slots.Length; i++)
        {
            _slots[i].Configure(i + 1, ExpeditionSquadSlot.SlotState.Locked);
        }
    }

    public void ConfigureSlot(
        int index,
        ExpeditionSquadSlot.SlotState state,
        Texture2D? portrait = null,
        string? shortName = null,
        double? hpRatio = null,
        string? secondaryName = null,
        double secondaryRatio = 0,
        string? criticalState = null)
    {
        if (index is < 0 or >= 4) throw new ArgumentOutOfRangeException(nameof(index));
        _slots[index].Configure(
            index + 1,
            state,
            portrait,
            shortName,
            hpRatio,
            secondaryName,
            secondaryRatio,
            criticalState);
    }

    public void GrabDefaultFocus() => _slots[0].GrabFocus();

    private void WireFocus()
    {
        // Squad strip exposes only horizontal neighbors for a
        // D-pad left/right cycle. Rerouted through the shared helper
        // for symmetry with the rest of the surfaces; Close #52.
        FocusRing.WireHorizontal(_slots);
    }
}
