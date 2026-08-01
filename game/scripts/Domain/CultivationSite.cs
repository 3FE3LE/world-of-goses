using System;

namespace WorldofGoses.Domain;

/// <summary>
/// One prepared EG-3 agricultural plot. It is city infrastructure with a
/// stable placement identity, but not yet a production building: sowing,
/// readiness and harvest are explicit domain transitions.
/// </summary>
public sealed class CultivationSite
{
    public CultivationSite(
        BuildingId id,
        CultivationPlotState state = CultivationPlotState.Prepared,
        int? plantedTick = null,
        int? readyAtTick = null)
    {
        Id = id;
        State = state;
        PlantedTick = plantedTick;
        ReadyAtTick = readyAtTick;
        ValidateState();
    }

    public BuildingId Id { get; }
    public CultivationPlotState State { get; private set; }
    public int? PlantedTick { get; private set; }
    public int? ReadyAtTick { get; private set; }

    public bool TrySow(int currentTick)
    {
        if (State != CultivationPlotState.Prepared || currentTick < 0) return false;
        State = CultivationPlotState.Sown;
        PlantedTick = currentTick;
        ReadyAtTick = checked(currentTick + CultivationRules.GrowthTicks);
        return true;
    }

    public bool AdvanceTo(int currentTick)
    {
        if (State == CultivationPlotState.Sown
            && PlantedTick is int plantedTick
            && currentTick > plantedTick)
        {
            State = CultivationPlotState.Growing;
        }
        if (State == CultivationPlotState.Growing
            && ReadyAtTick is int readyAtTick
            && currentTick >= readyAtTick)
        {
            State = CultivationPlotState.Ready;
            return true;
        }
        return false;
    }

    public bool TryHarvest()
    {
        if (State != CultivationPlotState.Ready) return false;
        State = CultivationPlotState.Spent;
        return true;
    }

    private void ValidateState()
    {
        if (!Enum.IsDefined(State))
        {
            throw new ArgumentOutOfRangeException(nameof(State), State, "Unknown cultivation state.");
        }
        bool requiresTiming = State is CultivationPlotState.Sown
            or CultivationPlotState.Growing
            or CultivationPlotState.Ready
            or CultivationPlotState.Spent;
        if (requiresTiming != (PlantedTick.HasValue && ReadyAtTick.HasValue)
            || (requiresTiming && (PlantedTick < 0
                || ReadyAtTick != PlantedTick + CultivationRules.GrowthTicks)))
        {
            throw new ArgumentException("Cultivation plot timing does not match its state.");
        }
    }
}
