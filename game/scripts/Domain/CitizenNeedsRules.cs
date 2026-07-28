namespace WorldofGoses.Domain;

/// <summary>Provisional hysteresis for the first interrupt/resume slice.</summary>
public static class CitizenNeedsRules
{
    public const int InterruptAtStamina = 20;
    public const int ResumeAtStamina = 70;

    public static bool RequiresRecovery(Citizen citizen) =>
        citizen.CurrentStamina <= InterruptAtStamina;

    public static bool CanResume(Citizen citizen) =>
        citizen.CurrentStamina >= ResumeAtStamina
        && citizen.WellFedRemainingTicks > 0;
}
