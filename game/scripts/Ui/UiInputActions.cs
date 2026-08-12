#nullable enable
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Named input actions for UI surfaces. The engine ships
/// <c>ui_cancel</c>, <c>ui_accept</c>, <c>ui_left</c>, <c>ui_right</c>,
/// <c>ui_up</c>, <c>ui_down</c> by default; A12 centralises the
/// string IDs so a future input remap or rename lives in one
/// place. Code that asks <c>IsActionPressed("ui_cancel")</c>
/// directly reads as a regression that the static guard
/// <c>ArchitectureBoundaryTests.Ui_DoesNotHardcodeInputActionStrings</c>
/// catches.
/// </summary>
public static class UiInputActions
{
    public const string Cancel = "ui_cancel";
    public const string Accept = "ui_accept";
    public const string Left = "ui_left";
    public const string Right = "ui_right";
    public const string Up = "ui_up";
    public const string Down = "ui_down";
    public const string TextCompletion = "ui_text_completion";
    public const string TextNewline = "ui_text_newline";
}
