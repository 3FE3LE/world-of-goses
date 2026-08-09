#nullable enable

using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses.Ui;

/// <summary>
/// Compact, read-only construction entry: project identity, explicit state,
/// and authored work progress. It estimates no duration because construction
/// rate depends on contributors and their current conditions.
/// </summary>
[GlobalClass]
public partial class ConstructionQueueItem : VBoxContainer
{
    public ConstructionQueueItem(CityStatusSnapshot.ProjectItem project)
    {
        MouseFilter = MouseFilterEnum.Pass;
        AddThemeConstantOverride("separation", Tokens.SpacingTight);

        var heading = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, Tokens.HudRowHeight),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        heading.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        AddChild(heading);

        heading.AddChild(new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(IconPaths.Building),
            StretchMode = TextureRect.StretchModeEnum.Keep,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.IconInline),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = LineageThemeRegistry.IconAccent,
        });
        heading.AddChild(new Label
        {
            Text = project.DisplayName,
            ThemeTypeVariation = "HudBody",
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        });

        string state = StatusText(project.StopCause);
        AddChild(new Label
        {
            Text = state,
            ThemeTypeVariation = "HudCaption",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
        });

        double ratio = project.RequiredWork <= 0
            ? 0.0
            : (double)project.Progress / project.RequiredWork;
        AddChild(new HudProgressBar(ratio));
        TooltipText = UiText.Format(
            "ui.city_summary.project_tooltip",
            project.DisplayName,
            project.Progress,
            project.RequiredWork,
            state);
    }

    internal static string StatusText(ConstructionStopCause cause) => cause switch
    {
        ConstructionStopCause.Authorized => UiText.Get("ui.city_summary.in_progress"),
        ConstructionStopCause.Paused => UiText.Get("ui.city_summary.paused"),
        ConstructionStopCause.NoWorkers => UiText.Get("ui.city_summary.waiting_contributors"),
        ConstructionStopCause.MissingMaterials => UiText.Get("ui.city_summary.waiting_materials"),
        ConstructionStopCause.WorkersInTransit => UiText.Get("ui.city_summary.contributor_travelling"),
        ConstructionStopCause.WorkersExhausted => UiText.Get("ui.city_summary.contributors_exhausted"),
        ConstructionStopCause.Night => UiText.Get("ui.city_summary.resting_night"),
        ConstructionStopCause.Completed => UiText.Get("ui.city_summary.completed"),
        ConstructionStopCause.AwaitingModule => UiText.Get("ui.city_summary.awaiting_module"),
        ConstructionStopCause.NoHero => UiText.Get("ui.city_summary.no_hero"),
        _ => cause.ToString(),
    };
}
