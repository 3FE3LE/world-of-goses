namespace WorldofGoses.Domain;

/// <summary>
/// Player-authored standing work intent. It survives temporary engagements
/// such as expeditions and vital recovery; the scheduler re-evaluates it
/// instead of blindly resuming an obsolete action.
/// </summary>
public readonly record struct CitizenWorkOrder(
    CitizenCommitmentKind Kind,
    BuildingId TargetId)
{
    public static CitizenWorkOrder? FromCommitment(CitizenCommitment commitment) =>
        commitment.Kind is CitizenCommitmentKind.BuildingWork or CitizenCommitmentKind.Construction
            ? new CitizenWorkOrder(commitment.Kind, new BuildingId(commitment.EntityId!.Value))
            : null;
}
