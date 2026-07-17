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
    private Building? _building;
    private IReadOnlyDictionary<CitizenId, Citizen>? _citizens;

    public override void _Ready()
    {
        Size = new Vector2(
            PresentationConstants.DetailedCitizenWidth * 3 + SlotPadding * 4,
            PresentationConstants.DetailedCitizenHeight + SlotPadding * 2);
    }

    public void Render(
        IReadOnlyList<CitizenId> visibleIds,
        Building building,
        IReadOnlyDictionary<CitizenId, Citizen> citizens)
    {
        _building = building;
        _citizens = citizens;

        var wanted = new HashSet<int>(visibleIds.Select(id => id.Value));
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
        foreach (var citizenId in visibleIds)
        {
            if (existing.Contains(citizenId.Value)) continue;
            if (!citizens.TryGetValue(citizenId, out var citizen)) continue;

            var slot = new VisibleWorkerSlot();
            slot.Name = $"Slot_{citizenId.Value}";
            slot.Position = ComputeSlotPosition(_slots.Count);
            slot.Size = new Vector2(
                PresentationConstants.DetailedCitizenWidth,
                PresentationConstants.DetailedCitizenHeight);
            slot.Configure(citizenId, citizen.Name);
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
