#nullable enable
using System;

namespace WorldofGoses.Domain;

/// <summary>
/// The material provision reserved when an expedition departs. A short route
/// may explicitly require none; absence is domain state, not a zero-sized fake
/// reservation or a sentinel resource.
/// </summary>
public readonly record struct ExpeditionSupplyRequirement
{
    private ExpeditionSupplyRequirement(ResourceType? resource, int amount)
    {
        if (resource.HasValue != (amount > 0))
        {
            throw new ArgumentException(
                "A supply requirement must be either None or one positive resource amount.");
        }
        Resource = resource;
        Amount = amount;
    }

    public ResourceType? Resource { get; }
    public int Amount { get; }
    public bool IsNone => !Resource.HasValue;

    public static ExpeditionSupplyRequirement None { get; } = new(null, 0);

    public static ExpeditionSupplyRequirement Required(ResourceType resource, int amount) =>
        new(resource, amount);
}
