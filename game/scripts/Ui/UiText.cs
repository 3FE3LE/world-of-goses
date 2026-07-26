#nullable enable
using System.Globalization;
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Presentation-only access to Godot's active translation catalog. Domain
/// types continue exposing stable values and identifiers without importing
/// Godot; UI adapters translate labels and format runtime values here.
/// </summary>
public static class UiText
{
    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        string translated = TranslationServer.Translate(key);
        return string.IsNullOrEmpty(translated) ? key : translated;
    }

    public static string Format(string key, params object?[] values) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), values);
}
