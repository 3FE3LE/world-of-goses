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
    [Signal] public delegate void CitizenArrivedEventHandler(int buildingId, int citizenId);

    private const int SlotPadding = 8;
    private const float SpriteCenterY = 68f;
    public const int SlotHeight = 152;

    private readonly List<VisibleWorkerSlot> _slots = new();

    /// <summary>Snapshot the controller passes through.</summary>
    public sealed record CitizenSlotInfo(CitizenId Id, string Name, LineageId Lineage, GenderId Gender, AppearanceVariantId Appearance)
    {
        public static CitizenSlotInfo From(BuildingDetailSnapshot.CitizenItem item) =>
            new(item.Id, item.Name, item.Lineage, item.Gender, item.Appearance);
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(
            PresentationConstants.DetailedCitizenWidth * 3 + SlotPadding * 4,
            SlotHeight + SlotPadding * 2);
        ClipContents = true;
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
        RenderSlots(buildingId, slots, idlePresentation: false);
    }

    public void RenderIdle(
        BuildingId buildingId,
        IReadOnlyList<BuildingDetailSnapshot.CitizenItem> citizens)
    {
        var slots = new List<CitizenSlotInfo>(citizens.Count);
        foreach (BuildingDetailSnapshot.CitizenItem citizen in citizens)
        {
            slots.Add(CitizenSlotInfo.From(citizen));
        }
        RenderSlots(buildingId, slots, idlePresentation: true);
    }

    private void RenderSlots(
        BuildingId buildingId,
        IReadOnlyList<CitizenSlotInfo> visibleCitizens,
        bool idlePresentation)
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
                RemoveChild(slot);
                slot.QueueFree();
                _slots.RemoveAt(i);
                continue;
            }
            if (!wanted.Contains(slot.CitizenId.Value))
            {
                if (slot.IsExiting) continue;
                slot.HideTo(EntryBorder(slot), Vector2.Left, () =>
                {
                    if (IsInstanceValid(slot) && slot.IsExiting)
                    {
                        _slots.Remove(slot);
                        if (slot.GetParent() == this) RemoveChild(slot);
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
                existingSlot.MountCarrier(this);
                if (existingSlot.IsExiting)
                {
                    existingSlot.ResumeTo(SlotCenter(existingSlot), () => EmitArrival(existingSlot));
                }
                else if (existingSlot.CarrierIsSettledHere)
                {
                    // Arrival callbacks can be interrupted when the canonical
                    // flyweight carrier changes visual owner during the same
                    // frame. Reconcile an already-settled carrier on refresh;
                    // the domain command is intentionally idempotent.
                    EmitArrival(existingSlot);
                }
                else
                {
                    // Covers Hidden (never shown yet) AND any state that
                    // belongs to a different context entirely — most
                    // notably the macro view's own ambient Macro state,
                    // which the building-detail slot machinery never
                    // previously reconciled: the carrier would sit wherever
                    // the macro street plot left it, in macro scale, while
                    // this slot's name label rendered correctly at the
                    // detail view's own position — a citizen with a name
                    // tag but no visible sprite, forever "outside".
                    existingSlot.ShowAt(EntryBorder(existingSlot), SlotCenter(existingSlot), () => EmitArrival(existingSlot));
                }
                continue;
            }

            var slot = new VisibleWorkerSlot();
            slot.Name = $"Slot_{citizen.Id.Value}";
            slot.Position = ComputeSlotPosition(slotIndex);
            slot.Size = new Vector2(
                PresentationConstants.DetailedCitizenWidth,
                SlotHeight);
            slot.Configure(buildingId, citizen.Id, citizen.Name, idlePresentation);

            var carrier = CitizenSpriteBank.Instance.GetOrCreate(citizen.Id, citizen.Lineage, citizen.Gender, citizen.Appearance);
            slot.AttachCarrier(carrier);
            slot.CitizenActivated += id => EmitSignal(SignalName.CitizenClicked, id);
            slot.AddToGroup(PresentationConstants.GroupVisibleWorkerSlot);
            AddChild(slot);
            _slots.Add(slot);
            CitizenSpriteBank.Instance.Mount(carrier, this);

            Vector2 slotCenter = SlotCenter(slot);
            Vector2 entryBorder = EntryBorder(slot);
            slot.ShowAt(entryBorder, slotCenter, () => EmitArrival(slot));

            slotIndex++;
        }

        ReflowSlots(buildingId);
    }

    private void EmitArrival(VisibleWorkerSlot slot) =>
        EmitSignal(SignalName.CitizenArrived, slot.BuildingId.Value, slot.CitizenId.Value);

    /// <summary>
    /// Returns the slot's name label and hit area. The carrier is mounted
    /// into this clipped stage and uses stage-local coordinates.
    /// </summary>
    private Vector2 ComputeSlotPosition(int index)
    {
        return new Vector2(
            SlotPadding + index * (PresentationConstants.DetailedCitizenWidth + SlotPadding),
            SlotPadding);
    }

    private static Vector2 SlotCenter(VisibleWorkerSlot slot)
    {
        return new Vector2(
            slot.Position.X + PresentationConstants.DetailedCitizenWidth / 2f,
            slot.Position.Y + SpriteCenterY);
    }

    private static Vector2 EntryBorder(VisibleWorkerSlot slot)
    {
        return new Vector2(-PresentationConstants.DetailedCitizenWidth, slot.Position.Y + SpriteCenterY);
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
