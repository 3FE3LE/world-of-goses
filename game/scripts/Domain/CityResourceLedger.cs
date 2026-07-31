#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// Location-aware facade over resources physically owned by buildings. It does
/// not duplicate stock: every query and mutation reads/writes the owning entity.
/// Reservations reduce availability without moving goods until committed.
/// </summary>
public sealed class CityResourceLedger
{
    private readonly IReadOnlyDictionary<BuildingId, Building> _buildings;
    private readonly CityInventory _inventory;
    private readonly Dictionary<ResourceReservationId, ResourceReservation> _reservations = new();
    private int _nextReservationId = 1;
    private EarlyGameMetrics? _metrics;

    internal CityResourceLedger(
        IReadOnlyDictionary<BuildingId, Building> buildings,
        CityInventory inventory)
    {
        _buildings = buildings;
        _inventory = inventory;
    }

    /// <summary>
    /// Attaches (or, with null, detaches) the EG-0 measurement. Every resource
    /// that enters or leaves the city passes through this class, so it is the
    /// one place where "gathered" and "spent" can be counted without
    /// sprinkling counters over each call site and eventually missing one. The
    /// observer is write-only: it cannot affect availability, so measuring a
    /// run cannot change it.
    ///
    /// <para>Detaching matters for restore. Rehydrating a save re-deposits
    /// every stored resource through this ledger, which would otherwise book
    /// the player's entire stockpile as freshly gathered on every single load
    /// — and the more the player relaunched, the more inflated the numbers
    /// would get.</para>
    /// </summary>
    internal void ObserveFlows(EarlyGameMetrics? metrics)
    {
        _metrics = metrics;
    }

    public IReadOnlyCollection<ResourceReservation> Reservations => _reservations.Values;

    public int Total(ResourceType resource)
    {
        int total = _inventory.AmountOf(resource);
        foreach (Building building in _buildings.Values)
        {
            total += StoredAmount(building, resource);
        }
        return total;
    }

    public int Available(ResourceType resource)
    {
        int reserved = 0;
        foreach (ResourceReservation reservation in _reservations.Values)
        {
            if (reservation.Resource == resource) reserved += reservation.Amount;
        }
        return Total(resource) - reserved;
    }

    public IReadOnlyList<(ResourceLocation Location, ResourceType Resource, int Amount)> Entries()
    {
        var entries = new List<(ResourceLocation, ResourceType, int)>();
        foreach ((ResourceType resource, int amount) in _inventory.Amounts)
        {
            entries.Add((
                new ResourceLocation(
                    ResourceLocationKind.CityInventory,
                    new BuildingId(0)),
                resource,
                amount));
        }
        foreach (Building building in _buildings.Values)
        {
            if (building.Stock > 0)
            {
                entries.Add((new ResourceLocation(ResourceLocationKind.BuildingStock, building.Id),
                    building.ProducedResourceType, building.Stock));
            }
            if (building.IronStock > 0)
            {
                entries.Add((new ResourceLocation(ResourceLocationKind.BuildingInputReserve, building.Id),
                    ResourceType.Iron, building.IronStock));
            }
            if (building.WoodReserve > 0)
            {
                entries.Add((new ResourceLocation(ResourceLocationKind.NaturalReserve, building.Id),
                    ResourceType.Wood, building.WoodReserve));
            }
        }
        return entries;
    }

    public bool TryConsume(ResourceType resource, int amount)
    {
        if (amount < 0 || Available(resource) < amount) return false;
        if (amount == 0) return true;
        Drain(resource, amount);
        return true;
    }

    /// <summary>Consumes a recipe atomically, aggregating duplicate resource rows.</summary>
    public bool TryConsume(IReadOnlyList<RecipeInput> inputs, out ResourceType? missing)
    {
        var required = new Dictionary<ResourceType, int>();
        foreach (RecipeInput input in inputs)
        {
            if (input.Amount <= 0) continue;
            required.TryGetValue(input.Resource, out int current);
            required[input.Resource] = checked(current + input.Amount);
        }
        foreach (var pair in required)
        {
            if (Available(pair.Key) < pair.Value)
            {
                missing = pair.Key;
                return false;
            }
        }
        foreach (var pair in required) Drain(pair.Key, pair.Value);
        missing = null;
        return true;
    }

    public int Deposit(ResourceType resource, int amount)
    {
        if (amount <= 0) return 0;
        int remaining = amount;
        foreach (Building building in _buildings.Values)
        {
            if (resource == ResourceType.Iron)
            {
                building.DepositIron(remaining);
                _metrics?.RecordGathered(resource, amount);
                return amount;
            }
            if (building.ProducedResourceType != resource) continue;
            int added = building.AddStock(remaining);
            remaining -= added;
            if (remaining == 0) break;
        }
        // What the city actually kept, not what it was offered: stock refused
        // by a full building never entered the economy and must not appear as
        // gathered.
        _metrics?.RecordGathered(resource, amount - remaining);
        return amount - remaining;
    }

    public int DepositToCityInventory(ResourceType resource, int amount)
    {
        int deposited = _inventory.Deposit(resource, amount);
        _metrics?.RecordGathered(resource, deposited);
        return deposited;
    }

    public bool TryReserve(
        ResourceType resource,
        int amount,
        ResourceReservationOwner owner,
        out ResourceReservation? reservation)
    {
        if (amount <= 0 || Available(resource) < amount)
        {
            reservation = null;
            return false;
        }
        reservation = new ResourceReservation(
            new ResourceReservationId(_nextReservationId++), resource, amount, owner);
        _reservations.Add(reservation.Id, reservation);
        return true;
    }

    public bool Release(ResourceReservationId id) => _reservations.Remove(id);

    public bool TransferReservation(
        ResourceReservationId id,
        ResourceReservationOwner newOwner)
    {
        if (!_reservations.TryGetValue(id, out ResourceReservation? reservation)) return false;
        _reservations[id] = reservation with { Owner = newOwner };
        return true;
    }

    public bool Commit(ResourceReservationId id)
    {
        if (!_reservations.Remove(id, out ResourceReservation? reservation)) return false;
        if (Total(reservation.Resource) < reservation.Amount)
        {
            _reservations.Add(id, reservation);
            return false;
        }
        Drain(reservation.Resource, reservation.Amount);
        return true;
    }

    internal void ClearReservations()
    {
        _reservations.Clear();
        _nextReservationId = 1;
    }

    internal void RestoreReservations(IEnumerable<ResourceReservation> reservations)
    {
        _reservations.Clear();
        int largestId = 0;
        foreach (ResourceReservation reservation in reservations)
        {
            _reservations.Add(reservation.Id, reservation);
            largestId = Math.Max(largestId, reservation.Id.Value);
        }
        _nextReservationId = largestId + 1;
    }

    private int StoredAmount(Building building, ResourceType resource) =>
        resource == ResourceType.Iron
            ? building.IronStock
            : building.ProducedResourceType == resource ? building.Stock : 0;

    private void Drain(ResourceType resource, int amount)
    {
        // Both TryConsume overloads funnel here after their availability
        // check, so recording once at the top counts every spend exactly once.
        _metrics?.RecordConsumed(resource, amount);
        int remaining = amount;
        int inventoryAmount = _inventory.AmountOf(resource);
        int inventoryTake = Math.Min(inventoryAmount, remaining);
        if (inventoryTake > 0)
        {
            _inventory.TryConsume(resource, inventoryTake);
            remaining -= inventoryTake;
        }
        foreach (Building building in _buildings.Values)
        {
            if (remaining == 0) break;
            int stored = StoredAmount(building, resource);
            int take = Math.Min(stored, remaining);
            if (take == 0) continue;
            bool consumed = resource == ResourceType.Iron
                ? building.TryConsumeIron(take)
                : building.TryConsumeStock(take);
            if (consumed) remaining -= take;
        }
    }
}
