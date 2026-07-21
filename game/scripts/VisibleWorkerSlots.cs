#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

public partial class VisibleWorkerSlots : Control
{
    [Signal] public delegate void CitizenClickedEventHandler(int citizenId);

    private const int SlotPadding = 8;
    private readonly List<VisibleWorkerSlot> _slots = new();

    public override void _Ready()
    {
        Size = new Vector2(
            PresentationConstants.DetailedCitizenWidth * 3 + SlotPadding * 4,
            PresentationConstants.DetailedCitizenHeight + SlotPadding * 2);
    }

    public void Render(IReadOnlyList<BuildingDetailSnapshot.CitizenItem> visibleCitizens)
    {
        var wanted = new HashSet<int>(visibleCitizens.Select(citizen => citizen.Id.Value));
        for (int i = _slots.Count - 1; i >= 0; i--)
        {
            var slot = _slots[i];
            if (!wanted.Contains(slot.CitizenId.Value))
            {
                slot.PlayExitAndFree();
                _slots.RemoveAt(i);
            }
        }

        var existing = new HashSet<int>(_slots.Select(s => s.CitizenId.Value));
        foreach (var citizen in visibleCitizens)
        {
            if (existing.Contains(citizen.Id.Value)) continue;

            var slot = new VisibleWorkerSlot();
            slot.Name = $"Slot_{citizen.Id.Value}";
            slot.Position = ComputeSlotPosition(_slots.Count);
            slot.Size = new Vector2(
                PresentationConstants.DetailedCitizenWidth,
                PresentationConstants.DetailedCitizenHeight);
            slot.Configure(citizen);
            slot.CitizenActivated += id => EmitSignal(SignalName.CitizenClicked, id);
            slot.AddToGroup(PresentationConstants.GroupVisibleWorkerSlot);
            AddChild(slot);
            _slots.Add(slot);
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].Position = ComputeSlotPosition(i);
        }
    }

    private Vector2 ComputeSlotPosition(int index)
    {
        return new Vector2(
            SlotPadding + index * (PresentationConstants.DetailedCitizenWidth + SlotPadding),
            SlotPadding);
    }
}
