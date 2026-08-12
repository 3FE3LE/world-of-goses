using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="FirstNightStage"/>.
/// Architecture Hardening A7. The authored first night stages were
/// introduced in schema v31.
/// </summary>
internal static class FirstNightStageSaveIds
{
    public const string ManifestedId = "Manifested";
    public const string SpiritArrivedId = "SpiritArrived";
    public const string ColdExplainedId = "ColdExplained";
    public const string CampfireBuiltId = "CampfireBuilt";
    public const string ShelterExplainedId = "ShelterExplained";
    public const string ShelterBuiltId = "ShelterBuilt";
    public const string OtherLightToldId = "OtherLightTold";
    public const string SleepingId = "Sleeping";
    public const string ConcludedId = "Concluded";

    public static string ToId(FirstNightStage value) => value switch
    {
        FirstNightStage.Manifested => ManifestedId,
        FirstNightStage.SpiritArrived => SpiritArrivedId,
        FirstNightStage.ColdExplained => ColdExplainedId,
        FirstNightStage.CampfireBuilt => CampfireBuiltId,
        FirstNightStage.ShelterExplained => ShelterExplainedId,
        FirstNightStage.ShelterBuilt => ShelterBuiltId,
        FirstNightStage.OtherLightTold => OtherLightToldId,
        FirstNightStage.Sleeping => SleepingId,
        FirstNightStage.Concluded => ConcludedId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out FirstNightStage value)
    {
        switch (id)
        {
            case ManifestedId: value = FirstNightStage.Manifested; return true;
            case SpiritArrivedId: value = FirstNightStage.SpiritArrived; return true;
            case ColdExplainedId: value = FirstNightStage.ColdExplained; return true;
            case CampfireBuiltId: value = FirstNightStage.CampfireBuilt; return true;
            case ShelterExplainedId: value = FirstNightStage.ShelterExplained; return true;
            case ShelterBuiltId: value = FirstNightStage.ShelterBuilt; return true;
            case OtherLightToldId: value = FirstNightStage.OtherLightTold; return true;
            case SleepingId: value = FirstNightStage.Sleeping; return true;
            case ConcludedId: value = FirstNightStage.Concluded; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
