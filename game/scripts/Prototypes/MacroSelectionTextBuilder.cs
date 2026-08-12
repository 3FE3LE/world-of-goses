#nullable enable
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Pure selection-text builders for the macro street view (A4). Translates
/// a <see cref="CityMacroSnapshot.CitizenItem"/> into the localized lines
/// the contextual inspector renders, and the keyed tuples a unit test can
/// assert without pulling Godot's translation runtime into the assert.
///
/// <see cref="MacroStreetLiveView"/> keeps one-line forwarders so the
/// existing in-class usage (<c>SelectCitizen</c>) and the
/// <c>MacroStreetLiveViewTests</c> surface keep compiling unchanged.
/// The <see cref="MacroStreetLiveView.SelectionLine"/> record stays nested
/// on the view because the test surface references it as
/// <c>MacroStreetLiveView.SelectionLine</c>.
/// </summary>
internal static class MacroSelectionTextBuilder
{
    /// <summary>Render the localized multi-line selection detail for a
    /// citizen. Each line's icon path is read but not displayed here —
    /// the caller is expected to render the icon list itself; the body's
    /// job is the joined text.</summary>
    public static string FormatCitizenSelectionDetail(CityMacroSnapshot.CitizenItem citizen)
    {
        var lines = new List<string>();
        foreach (MacroStreetLiveView.SelectionLine line in BuildCitizenSelectionKeys(citizen))
        {
            _ = line.IconPath;
            if (line.FormatArgs is null)
            {
                lines.Add(UiText.Get(line.TextKey));
                continue;
            }
            object[] translated = TranslateSelectionArgs(line.FormatArgs);
            lines.Add(UiText.Format(line.TextKey, translated));
        }
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Translates raw domain values into the strings the view layer formats
    /// via <see cref="UiText.Format"/>. The only translation needed today is
    /// the wound recovery duration (ticks → human-readable string); the
    /// severity key is already a localization key and passes through unchanged.
    /// </summary>
    public static object[] TranslateSelectionArgs(IReadOnlyList<object> formatArgs)
    {
        var translated = new object[formatArgs.Count];
        for (int index = 0; index < formatArgs.Count; index++)
        {
            translated[index] = formatArgs[index] is int ticks
                ? SimulationTimeText.FormatDurationLocalized(ticks)
                : formatArgs[index];
        }
        return translated;
    }

    /// <summary>
    /// Returns the same lines the bubble/body would render, but as raw
    /// (icon, key, formatArgs) so the structure can be unit-tested without
    /// pulling Godot's translation runtime into a Godot-free xUnit process.
    /// Translation happens at the view layer
    /// (<see cref="FormatCitizenSelectionDetail"/>). The remaining
    /// <c>citizen.WoundRecoveryTicksRemaining</c> slot is the raw tick count;
    /// the view layer resolves it to a localized duration string before
    /// passing it to <see cref="UiText.Format"/>.
    /// </summary>
    public static IReadOnlyList<MacroStreetLiveView.SelectionLine> BuildCitizenSelectionKeys(
        CityMacroSnapshot.CitizenItem citizen)
    {
        var lines = new List<MacroStreetLiveView.SelectionLine>();
        if (citizen.IsOnExpedition)
        {
            lines.Add(new MacroStreetLiveView.SelectionLine(
                IconPaths.Shield,
                "ui.world_status.expedition",
                null));
        }
        else
        {
            if (citizen.BlockReason == CitizenRoutineBlockReason.NoFood)
            {
                lines.Add(new MacroStreetLiveView.SelectionLine(
                    IconPaths.Warning,
                    "ui.world_status.no_food",
                    null));
            }
            else
            {
                string key = citizen.Activity switch
                {
                    CitizenRoutineActivity.Working => "ui.world_status.working",
                    CitizenRoutineActivity.TravellingToWork => "ui.world_status.travelling",
                    CitizenRoutineActivity.TravellingHome => "ui.world_status.travelling",
                    CitizenRoutineActivity.WaitingForStorage => "ui.world_status.waiting_storage",
                    CitizenRoutineActivity.WaitingForResources => "ui.world_status.waiting_resources",
                    CitizenRoutineActivity.WorkplaceIdle => "ui.world_status.work_paused",
                    CitizenRoutineActivity.OffDuty => "ui.world_status.off_duty",
                    CitizenRoutineActivity.Resting => "ui.world_status.resting",
                    CitizenRoutineActivity.Recovering => "ui.world_status.recovering",
                    CitizenRoutineActivity.Leisure => "ui.world_status.idle",
                    _ => "ui.world_status.unavailable",
                };
                lines.Add(new MacroStreetLiveView.SelectionLine(IconPaths.Cog, key, null));
            }
        }
        if (citizen.WoundSeverity is WoundSeverity severity)
        {
            string severityKey = severity == WoundSeverity.Severe
                ? "ui.wound.severe"
                : "ui.wound.moderate";
            lines.Add(new MacroStreetLiveView.SelectionLine(
                IconPaths.Heart,
                "ui.world_status.wound",
                new object[] { severityKey }));
            if (citizen.IsReceivingWoundTreatment)
            {
                lines.Add(new MacroStreetLiveView.SelectionLine(
                    IconPaths.Clock,
                    "ui.world_status.treatment",
                    new object[] { citizen.WoundRecoveryTicksRemaining }));
            }
        }
        return lines;
    }
}
