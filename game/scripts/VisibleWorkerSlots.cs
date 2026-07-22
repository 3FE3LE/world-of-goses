#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Container for the building-detail worker slots. Each slot is a
/// position marker; the visible sprite lives in the
/// <see cref="CitizenSpriteBank"/>'s carrier so the same citizen
/// cannot appear twice.
/// </summary>
public partial class VisibleWorkerSlots : Control
{
    [Signal] public delegate void CitizenClickedEventHandler(int citizenId);

    private const int SlotPadding = 8;
    private const float SpriteYOffset = 126f;

    private readonly List<VisibleWorkerSlot> _slots = new();

    /// <summary>Snapshot the controller passes through.</summary>
    public sealed record CitizenSlotInfo(CitizenId Id, string Name, LineageId Lineage, GenderId Gender)
    {
        public static CitizenSlotInfo From(BuildingDetailSnapshot.CitizenItem item) =>
            new(item.Id, item.Name, item.Lineage, item.Gender);
    }

    public override void _Ready()
    {
        Size = new Vector2(
            PresentationConstants.DetailedCitizenWidth * 3 + SlotPadding * 4,
            PresentationConstants.DetailedCitizenHeight + SlotPadding * 2);
    }

    /// <summary>
    /// Reconciles the slot list with the current building's visible
    /// citizens. The carrier pattern means the sprite is never
    /// destroyed here; slots are hidden and re-shown as the
    /// navigation changes.
    /// </summary>
    public void Render(
        BuildingId buildingId,
        IReadOnlyList<BuildingDetailSnapshot.CitizenItem> visibleCitizens)
    {
        var slots = new List<CitizenSlotInfo>(visibleCitizens.Count);
        foreach (var c in visibleCitizens)
        {
            slots.Add(CitizenSlotInfo.From(c));
        }
        RenderSlots(buildingId, slots);
    }

    private void RenderSlots(
        BuildingId buildingId,
        IReadOnlyList<CitizenSlotInfo> visibleCitizens)
    {
        // Reap freed slots (e.g., when a backing carrier's parent
        // was freed between frames).
        _slots.RemoveAll(slot => !IsInstanceValid(slot));

        var wanted = new HashSet<int>(visibleCitizens.Select(c => c.Id.Value));
        // Slots from another building are presentation state only. Remove
        // them immediately; their carriers remain alive in the bank.
        for (int i = _slots.Count - 1; i >= 0; i--)
        {
            var slot = _slots[i];
            if (slot.BuildingId.Value != buildingId.Value)
            {
                slot.HideImmediate();
                slot.QueueFree();
                _slots.RemoveAt(i);
                continue;
            }
            if (!wanted.Contains(slot.CitizenId.Value))
            {
                slot.HideTo(EntryBorderViewport(slot), Vector2.Left, () =>
                {
                    if (IsInstanceValid(slot) && slot.IsExiting)
                    {
                        _slots.Remove(slot);
                        slot.QueueFree();
                        ReflowSlots(buildingId);
                    }
                });
            }
        }

        // Second pass: create slots for newly visible citizens.
        var existing = _slots
            .Where(s => s.BuildingId.Value == buildingId.Value)
            .Select(s => s.CitizenId.Value)
            .ToHashSet();
        int slotIndex = _slots.Count(s => s.BuildingId.Value == buildingId.Value);
        foreach (var citizen in visibleCitizens)
        {
            if (existing.Contains(citizen.Id.Value))
            {
                var existingSlot = _slots.First(s =>
                    s.BuildingId.Value == buildingId.Value && s.CitizenId.Value == citizen.Id.Value);
                if (existingSlot.IsExiting)
                {
                    existingSlot.ResumeTo(SlotCenterViewport(existingSlot));
                }
                continue;
            }

            var slot = new VisibleWorkerSlot();
            slot.Name = $"Slot_{citizen.Id.Value}";
            slot.Position = ComputeSlotPosition(slotIndex);
            slot.Size = new Vector2(
                PresentationConstants.DetailedCitizenWidth,
                PresentationConstants.DetailedCitizenHeight);
            slot.Configure(buildingId, citizen.Id, citizen.Name);

            var carrier = CitizenSpriteBank.Instance.GetOrCreate(citizen.Id, citizen.Lineage, citizen.Gender);
            slot.AttachCarrier(carrier);
            slot.CitizenActivated += id => EmitSignal(SignalName.CitizenClicked, id);
            slot.AddToGroup(PresentationConstants.GroupVisibleWorkerSlot);
            AddChild(slot);
            _slots.Add(slot);

            // Walk the carrier from the slot's left border to the
            // slot's center in viewport coordinates.
            Vector2 slotCenter = SlotCenterViewport(slot);
            Vector2 entryBorder = EntryBorderViewport(slot);
            slot.ShowAt(entryBorder, slotCenter);

            slotIndex++;
        }

        ReflowSlots(buildingId);
    }

    /// <summary>
    /// Returns the slot's name label, hit area, etc. The carrier
    /// lives in the bank's global CanvasLayer and is positioned in
    /// viewport coordinates by the slot's Show/Hide methods.
    /// </summary>
    private Vector2 ComputeSlotPosition(int index)
    {
        return new Vector2(
            SlotPadding + index * (PresentationConstants.DetailedCitizenWidth + SlotPadding),
            SlotPadding);
    }

    private Vector2 SlotCenterViewport(VisibleWorkerSlot slot)
    {
        return new Vector2(
            slot.GlobalPosition.X + PresentationConstants.DetailedCitizenWidth / 2f,
            slot.GlobalPosition.Y + SpriteYOffset);
    }

    private Vector2 EntryBorderViewport(VisibleWorkerSlot slot)
    {
        // Off-screen left, at the same vertical level as the slot.
        return new Vector2(-200f, slot.GlobalPosition.Y + SpriteYOffset);
    }

    private void ReflowSlots(BuildingId buildingId)
    {
        int index = 0;
        foreach (var slot in _slots.Where(slot => slot.BuildingId.Value == buildingId.Value))
        {
            slot.Position = ComputeSlotPosition(index++);
        }
    }
}
