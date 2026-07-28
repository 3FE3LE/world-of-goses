#nullable enable
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>Shared display text for <see cref="AssignmentOutcome"/> rejections — used
/// by both the construction project assignment flow and building worker assignment.</summary>
public static class AssignmentErrorText
{
    public static string Format(AssignmentOutcome outcome) => outcome switch
    {
        AssignmentOutcome.AtCapacity => UiText.Get("Project is at worker capacity."),
        AssignmentOutcome.AlreadyAssigned => UiText.Get("Citizen is already a contributor."),
        AssignmentOutcome.CitizenUnavailable => UiText.Get("Citizen is assigned elsewhere."),
        AssignmentOutcome.NotAssigned => UiText.Get("Citizen is not assigned here."),
        AssignmentOutcome.CitizenNotFound => UiText.Get("Citizen no longer exists."),
        AssignmentOutcome.BuildingNotFound => UiText.Get("Worksite no longer exists."),
        _ => UiText.Get("Assignment rejected."),
    };
}
