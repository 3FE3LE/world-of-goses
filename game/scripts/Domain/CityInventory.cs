using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// City-owned gathered resources that are no longer tied to the lifetime of a
/// producing building. Natural reserves remain on their resource patches.
/// </summary>
public sealed class CityInventory
{
    private readonly Dictionary<ResourceType, int> _amounts = new();

    public IReadOnlyDictionary<ResourceType, int> Amounts => _amounts;

    public int AmountOf(ResourceType resource) =>
        _amounts.TryGetValue(resource, out int amount) ? amount : 0;

    public int Deposit(ResourceType resource, int amount)
    {
        if (amount <= 0) return 0;
        _amounts.TryGetValue(resource, out int current);
        _amounts[resource] = checked(current + amount);
        return amount;
    }

    public bool TryConsume(ResourceType resource, int amount)
    {
        if (amount < 0) return false;
        int current = AmountOf(resource);
        if (current < amount) return false;
        int remaining = current - amount;
        if (remaining == 0) _amounts.Remove(resource);
        else _amounts[resource] = remaining;
        return true;
    }

    internal void Restore(IReadOnlyDictionary<ResourceType, int> amounts)
    {
        _amounts.Clear();
        foreach ((ResourceType resource, int amount) in amounts)
        {
            if (amount > 0) _amounts[resource] = amount;
        }
    }
}
