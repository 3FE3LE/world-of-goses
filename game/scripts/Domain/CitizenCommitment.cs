using System;

namespace WorldofGoses.Domain;

/// <summary>
/// One authoritative, mutually-exclusive citizen commitment. EntityId is the
/// building, construction project, expedition, or future recovery-plan id.
/// </summary>
public readonly record struct CitizenCommitment
{
    public static CitizenCommitment None => new(CitizenCommitmentKind.None, null);

    public CitizenCommitment(CitizenCommitmentKind kind, int? entityId)
    {
        if (kind == CitizenCommitmentKind.None && entityId.HasValue)
        {
            throw new ArgumentException("An available citizen cannot reference a commitment entity.", nameof(entityId));
        }
        if (kind != CitizenCommitmentKind.None && (!entityId.HasValue || entityId.Value <= 0))
        {
            throw new ArgumentException("A committed citizen requires a positive entity id.", nameof(entityId));
        }

        Kind = kind;
        EntityId = entityId;
    }

    public CitizenCommitmentKind Kind { get; }
    public int? EntityId { get; }
    public bool IsAvailable => Kind == CitizenCommitmentKind.None;
}
