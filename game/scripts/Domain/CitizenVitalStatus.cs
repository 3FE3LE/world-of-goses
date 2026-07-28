namespace WorldofGoses.Domain;

/// <summary>
/// Survival-only interruption of player-authored work. It never chooses a
/// profession or productive target.
/// </summary>
public enum CitizenVitalStatus
{
    Stable = 0,
    Recovering = 1,
    BlockedNoFood = 2,
}
