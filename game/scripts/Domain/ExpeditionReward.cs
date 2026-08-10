#nullable enable
using System;

namespace WorldofGoses.Domain;

/// <summary>
/// What completing an expedition can produce. Discovery is deliberately
/// non-material: it records progress without inventing a resource payout.
/// </summary>
public readonly record struct ExpeditionReward
{
    private ExpeditionReward(
        ExpeditionRewardKind kind,
        ResourceType? resource,
        int amount)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        bool material = kind == ExpeditionRewardKind.Supplies;
        if (material != (resource.HasValue && amount > 0))
        {
            throw new ArgumentException(
                "Only a Supplies reward carries a positive resource amount.");
        }
        Kind = kind;
        Resource = resource;
        Amount = amount;
    }

    public ExpeditionRewardKind Kind { get; }
    public ResourceType? Resource { get; }
    public int Amount { get; }
    public bool IsMaterial => Kind == ExpeditionRewardKind.Supplies;

    public static ExpeditionReward Supplies(ResourceType resource, int amount) =>
        new(ExpeditionRewardKind.Supplies, resource, amount);

    public static ExpeditionReward Migrant { get; } =
        new(ExpeditionRewardKind.Migrant, null, 0);

    public static ExpeditionReward Discovery { get; } =
        new(ExpeditionRewardKind.Discovery, null, 0);
}
