#nullable enable
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Autoload that owns the long-lived sprite instances for every
/// citizen. One citizen, one sprite, period — see
/// `docs/engineering/architecture.md §7b` for the design rationale. Views
/// (building-detail slots, macro markers, future hero profile) ask
/// the bank for the carrier and position it; they never create
/// sprites of their own.
///
/// <para>Inactive carriers park under the bank. The active view mounts the
/// canonical carrier into its own visual host, so ordinary scene order,
/// clipping, modals and scrims apply without a global overlay layer.</para>
/// </summary>
public partial class CitizenSpriteBank : Node
{
    public static CitizenSpriteBank Instance { get; private set; } = null!;

    private readonly Dictionary<CitizenId, CitizenSpriteCarrier> _carriers = new();
    private Node2D _parking = null!;

    public override void _Ready()
    {
        Instance = this;
        _parking = new Node2D { Name = "Parking" };
        AddChild(_parking);
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null!;
    }

    /// <summary>
    /// Returns the canonical carrier for the citizen, creating it on
    /// first request. The carrier is created once per citizen and
    /// lives for the lifetime of the bank.
    /// </summary>
    public CitizenSpriteCarrier GetOrCreate(CitizenId citizenId, LineageId lineage, GenderId gender, AppearanceVariantId appearanceVariant)
    {
        if (_carriers.TryGetValue(citizenId, out var existing)
            && IsInstanceValid(existing)
            && existing.Lineage == lineage
            && existing.Gender == gender
            && existing.AppearanceVariant == appearanceVariant)
        {
            return existing;
        }

        if (existing is not null && IsInstanceValid(existing))
        {
            existing.CancelMotion();
            existing.SetState(CitizenSpriteCarrier.VisualState.Hidden);
            existing.QueueFree();
        }

        var carrier = new CitizenSpriteCarrier();
        carrier.Name = $"Carrier_{citizenId.Value}";
        carrier.Initialize(citizenId, lineage, gender, appearanceVariant);
        _parking.AddChild(carrier);
        _carriers[citizenId] = carrier;
        return carrier;
    }

    /// <summary>Re-skins an existing carrier to a new cosmetic appearance variant without recreating the carrier node.</summary>
    public void SetAppearance(CitizenId citizenId, AppearanceVariantId appearanceVariant)
    {
        if (!_carriers.TryGetValue(citizenId, out var existing) || !IsInstanceValid(existing))
        {
            return;
        }
        existing.ReinitializeAppearance(appearanceVariant);
    }

    /// <summary>
    /// Returns the carrier if it exists and is alive. Useful for views
    /// that want to reposition an already-known carrier without
    /// creating a new one.
    /// </summary>
    public bool TryGet(CitizenId id, out CitizenSpriteCarrier? carrier)
    {
        if (_carriers.TryGetValue(id, out var existing) && IsInstanceValid(existing))
        {
            carrier = existing;
            return true;
        }
        carrier = null;
        return false;
    }

    /// <summary>
    /// Discards the carrier when the citizen is removed from the
    /// world (e.g., death, emigration). The next GetOrCreate for the
    /// same id is unreachable, so the entry is freed.
    /// </summary>
    public void Remove(CitizenId id)
    {
        if (_carriers.TryGetValue(id, out var carrier))
        {
            if (IsInstanceValid(carrier))
            {
                carrier.CancelMotion();
                carrier.SetState(CitizenSpriteCarrier.VisualState.Hidden);
                carrier.QueueFree();
            }
            _carriers.Remove(id);
        }
    }

    /// <summary>
    /// Number of currently-tracked carriers. Diagnostics helper and
    /// invariant for performance tests.
    /// </summary>
    public int CarrierCount => _carriers.Count;

    /// <summary>
    /// Moves a canonical carrier into the active view's visual subtree while
    /// preserving its global transform. Registry ownership does not depend on
    /// scene-tree parentage.
    /// </summary>
    public void Mount(CitizenSpriteCarrier carrier, Node host)
    {
        if (!IsInstanceValid(carrier) || carrier.GetParent() == host) return;
        carrier.Reparent(host, keepGlobalTransform: true);
    }

    /// <summary>
    /// Removes carriers whose citizens no longer exist in the active world.
    /// This does not create missing carriers: allocation stays lazy, while
    /// the tracked set remains a subset of the domain's citizen identities.
    /// </summary>
    public void PruneExcept(IEnumerable<CitizenId> activeCitizenIds)
    {
        var active = new HashSet<CitizenId>(activeCitizenIds);
        var stale = new List<CitizenId>();
        foreach (var id in _carriers.Keys)
        {
            if (!active.Contains(id)) stale.Add(id);
        }
        foreach (var id in stale)
        {
            Remove(id);
        }
    }
}
