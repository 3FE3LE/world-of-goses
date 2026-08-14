#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace WorldofGoses.Ui;

/// <summary>Four-slot Active Skill presentation for the future live expedition view.</summary>
[GlobalClass]
public partial class ExpeditionSkillStrip : PanelContainer
{
    private OctagonalSkillSlot[] _slots = Array.Empty<OctagonalSkillSlot>();

    public IReadOnlyList<OctagonalSkillSlot> Slots => _slots;

    public override void _Ready()
    {
        _slots = new[]
        {
            GetNode<OctagonalSkillSlot>("Content/Slots/Slot1"),
            GetNode<OctagonalSkillSlot>("Content/Slots/Slot2"),
            GetNode<OctagonalSkillSlot>("Content/Slots/Slot3"),
            GetNode<OctagonalSkillSlot>("Content/Slots/Slot4"),
        };

        Texture2D? initialIcon = ResourceLoader.Load<Texture2D>(IconPaths.Fire);
        _slots[0].Configure(1, OctagonalSkillSlot.SlotState.Ready, initialIcon);
        for (int i = 1; i < _slots.Length; i++)
        {
            _slots[i].Configure(i + 1, OctagonalSkillSlot.SlotState.Locked);
        }
        WireFocus();
    }

    public void ConfigureSlot(
        int index,
        OctagonalSkillSlot.SlotState state,
        Texture2D? icon = null,
        double cooldownRemaining = 0,
        double cooldownDuration = 0)
    {
        if (index is < 0 or >= 4) throw new ArgumentOutOfRangeException(nameof(index));
        _slots[index].Configure(
            index + 1,
            state,
            icon,
            cooldownRemaining,
            cooldownDuration);
    }

    public void GrabDefaultFocus() => _slots[0].GrabFocus();

    private void WireFocus()
    {
        // Skill strip is horizontal-only; routed through the shared
        // helper for symmetry with the rest of the surfaces;
        // Close #52.
        FocusRing.WireHorizontal(_slots);
    }
}
