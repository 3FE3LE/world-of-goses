using System;

namespace WorldofGoses.Domain;

/// <summary>
/// The bounded rules of the authored first night. Everything quantitative here
/// derives from the real construction rules, so a recipe change cannot leave the
/// sequence — or the spirit's dialogue — describing a world that no longer
/// exists. That drift is exactly what retired the previous hand-written tutorial.
/// </summary>
public static class FirstNightRules
{
    /// <summary>
    /// Narrative dawn, `06:00`. A fresh world starts at tick 0 = Day 1 `00:00`,
    /// so the night owns the first <c>900</c> ticks of the world.
    ///
    /// It is a presentation ceiling, not a deadline: crossing it does not end
    /// the night, because the night ends on <see cref="FirstNightStage"/>
    /// milestones. Nothing expires when the clock passes it.
    /// </summary>
    public const int DawnTick = GameClock.TicksPerInGameDay / 4;

    /// <summary>
    /// The last tick the night is willing to show, `05:59`. A slow player's
    /// clock parks here rather than rolling into daylight while the spirit is
    /// still present and the world is still authored night.
    /// </summary>
    public const int LatestDisplayedNightTick = DawnTick - 1;

    /// <summary>
    /// The two modules the night requires, in narrative order: heat, then
    /// cover and sleep. Cache and Canopy are deliberately excluded — they are
    /// post-dawn consolidation, and the doc calls the night's shelter
    /// "deliberadamente rudimentario".
    /// </summary>
    public static FoundingSiteModule ModuleFor(FirstNightStage stage) => stage switch
    {
        FirstNightStage.ColdExplained => FoundingSiteModule.Campfire,
        FirstNightStage.ShelterExplained => FoundingSiteModule.Bedroll,
        _ => throw new ArgumentOutOfRangeException(
            nameof(stage),
            stage,
            "Only the two build stages of the first night map to a module."),
    };

    /// <summary>Whether <paramref name="stage"/> waits on the player finishing a module.</summary>
    public static bool WaitsForModule(FirstNightStage stage) =>
        stage is FirstNightStage.ColdExplained or FirstNightStage.ShelterExplained;

    /// <summary>
    /// Whether <paramref name="stage"/> waits on the player closing a main
    /// dialogue node. These are the stages the spirit drives.
    /// </summary>
    public static bool WaitsForDialogue(FirstNightStage stage) => stage is
        FirstNightStage.SpiritArrived
        or FirstNightStage.CampfireBuilt
        or FirstNightStage.ShelterBuilt;

    /// <summary>
    /// The stage that follows <paramref name="stage"/> once its condition is
    /// met. <see cref="FirstNightStage.Concluded"/> is absorbing.
    /// </summary>
    public static FirstNightStage Next(FirstNightStage stage) => stage switch
    {
        FirstNightStage.Manifested => FirstNightStage.SpiritArrived,
        FirstNightStage.SpiritArrived => FirstNightStage.ColdExplained,
        FirstNightStage.ColdExplained => FirstNightStage.CampfireBuilt,
        FirstNightStage.CampfireBuilt => FirstNightStage.ShelterExplained,
        FirstNightStage.ShelterExplained => FirstNightStage.ShelterBuilt,
        FirstNightStage.ShelterBuilt => FirstNightStage.OtherLightTold,
        FirstNightStage.OtherLightTold => FirstNightStage.Sleeping,
        FirstNightStage.Sleeping => FirstNightStage.Concluded,
        FirstNightStage.Concluded => FirstNightStage.Concluded,
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null),
    };

    /// <summary>
    /// Whether the spirit is present in the world at <paramref name="stage"/>.
    /// It arrives after the manifestation and is gone before the founder wakes,
    /// which invariant 9 of the design doc requires.
    /// </summary>
    public static bool SpiritIsPresent(FirstNightStage stage) =>
        stage is > FirstNightStage.Manifested and < FirstNightStage.Sleeping;

    /// <summary>
    /// Whether the calendar must stay held. While the night runs, the daily
    /// Food ration and the day/night boundary are deferred: a player who reads
    /// slowly can cross tick 1200 (`08:00`) mid-sequence, and the authored
    /// night must not start charging rations behind the narration.
    /// </summary>
    public static bool DefersCalendar(FirstNightStage stage) =>
        stage != FirstNightStage.Concluded;
}
