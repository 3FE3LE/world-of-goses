#nullable enable
using System.Collections.Generic;

namespace WorldofGoses.Domain.Persistence;

public sealed class ExpeditionSave
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The full team (1-2 citizens). v14 and earlier saves only had
    /// <see cref="LeadCitizenId"/>; <c>MigrateV14ToV15</c> copies that single
    /// id in here. New saves always populate this and keep
    /// <see cref="LeadCitizenId"/> in sync as its first entry for any tool
    /// still reading the old field.
    /// </summary>
    public List<int> MemberCitizenIds { get; set; } = new();

    public int LeadCitizenId { get; set; }
    public int StartTick { get; set; }
    public int EndTick { get; set; }
    public string SupplyResource { get; set; } = string.Empty;
    public int SupplyAmount { get; set; }
    public string RewardResource { get; set; } = string.Empty;
    public int RewardAmount { get; set; }
    public int ReservationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ReturnedAmount { get; set; }
    public string RewardKind { get; set; } = string.Empty;
    public int? DeliveredMigrantId { get; set; }

    /// <summary>
    /// docs/FIRST_PLAYABLE_LOOP_AUDIT.md §G4. Absent/empty on v15 and
    /// earlier saves; <c>MigrateV15ToV16</c> defaults every still-active
    /// expedition to <see cref="ExpeditionPhase.Outbound"/> (its encounter
    /// simply re-resolves — deterministically, from the same persisted id
    /// and start tick — on the next tick that world advances) and every
    /// already-finished one to <see cref="ExpeditionPhase.Resolved"/>.
    /// </summary>
    public string Phase { get; set; } = string.Empty;

    public string? EncounterOutcome { get; set; }
    public string RetreatPosture { get; set; } = string.Empty;
    public int? DispatchEventId { get; set; }
    public int? TargetParcelId { get; set; }
}
