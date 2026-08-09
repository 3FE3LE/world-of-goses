#nullable enable
using System;
using System.Collections.Generic;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>Read-only city-HUD projection of the expedition system.</summary>
public sealed record ExpeditionRailSnapshot(
    int CurrentTick,
    IReadOnlyList<ExpeditionRailSnapshot.Item> ActiveExpeditions,
    IReadOnlyList<WorldEvent> Events)
{
    public sealed record Item(
        ExpeditionId Id,
        string DisplayName,
        ExpeditionPhase Phase,
        IReadOnlyList<string> MemberNames,
        ResourceType SupplyResource,
        int SupplyAmount,
        int StartTick,
        int EndTick,
        bool CanCancel)
    {
        public int MemberCount => MemberNames.Count;
        public int RemainingTicks(int currentTick) => Math.Max(0, EndTick - currentTick);
        public double Progress(int currentTick)
        {
            int duration = EndTick - StartTick;
            return duration <= 0
                ? 1d
                : Math.Clamp((currentTick - StartTick) / (double)duration, 0d, 1d);
        }
    }

    public static ExpeditionRailSnapshot From(CityWorld world)
    {
        var active = new List<Item>();
        foreach (Expedition expedition in world.Expeditions.Values)
        {
            if (expedition.Status != ExpeditionStatus.Active) continue;

            var memberNames = new List<string>(expedition.MemberIds.Count);
            foreach (CitizenId memberId in expedition.MemberIds)
            {
                Citizen? citizen = world.GetCitizen(memberId);
                if (citizen is not null) memberNames.Add(citizen.Name);
            }

            active.Add(new Item(
                expedition.Id,
                expedition.DisplayName,
                expedition.Phase,
                memberNames,
                expedition.SupplyResource,
                expedition.SupplyAmount,
                expedition.StartTick,
                expedition.EndTick,
                CanCancel: expedition.Phase == ExpeditionPhase.Outbound
                    && world.CurrentTick == expedition.StartTick));
        }

        return new ExpeditionRailSnapshot(
            world.CurrentTick,
            active,
            new List<WorldEvent>(world.Log.Events));
    }
}
