#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;

namespace WorldofGoses.Ui;

/// <summary>
/// Autoload that owns the project's localization state. Loads Godot-imported
/// .po translation resources for the active locale, registers them with
/// <see cref="TranslationServer"/>, exposes <see cref="SetLocale"/>,
/// and persists the player's choice in a sidecar
/// <c>settings.json</c> next to the world save slot.
///
/// <para>
/// The settings file lives outside <see cref="WorldPersistence"/>
/// because the locale is a presentation preference, not world
/// state. Resetting a city must not reset the language. The file is
/// loaded lazily on <see cref="_Ready"/>; the persistence is best
/// effort — a missing or malformed file falls back to
/// <see cref="DefaultLocale"/>.
/// </para>
/// </summary>
public partial class LocaleManager : Node
{
    /// <summary>Fallback locale when no setting is persisted or the file is unreadable.</summary>
    public const string DefaultLocale = "en";

    /// <summary>Filename of the sidecar settings file. Sits next to the world save slot.</summary>
    public const string SettingsFileName = "settings.json";

    /// <summary>Resource path where .po files live.</summary>
    public const string LocaleDirectory = "res://locale";

    [Signal] public delegate void LocaleChangedEventHandler(string locale);

    /// <summary>Locales the project ships translations for. Order is the cycle order for <see cref="ToggleLocale"/>.</summary>
    public IReadOnlyList<string> AvailableLocales { get; } = new[] { "en", "es" };

    /// <summary>Currently active locale code (e.g. "en", "es").</summary>
    public string CurrentLocale { get; private set; } = DefaultLocale;

    private Translation? _loadedTranslation;

    public override void _Ready()
    {
        string restored = TryRestoreLocale();
        LoadLocale(restored);
    }

    /// <summary>
    /// Switches the active locale. Removes the previously registered
    /// translation, loads the new <c>.po</c> through Godot's native resource
    /// importer, registers the resulting <see cref="Translation"/> with
    /// <see cref="TranslationServer"/>, and persists the choice.
    /// </summary>
    public void SetLocale(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale) || locale == CurrentLocale) return;
        if (!LoadLocale(locale)) return;
        TryPersistLocale();
        EmitSignal(SignalName.LocaleChanged, CurrentLocale);
    }

    /// <summary>
    /// Capture-only locale switch. It exercises the same catalog loading path
    /// without rewriting the player's persisted preference.
    /// </summary>
    internal void SetLocaleForVisualRegression(string locale)
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        if (!LoadLocale(locale)) return;
        EmitSignal(SignalName.LocaleChanged, CurrentLocale);
    }

    /// <summary>
    /// Cycles to the next locale in <see cref="AvailableLocales"/>.
    /// Loops back to the first after the last. Convenient for the
    /// pause-menu button.
    /// </summary>
    public void ToggleLocale()
    {
        if (AvailableLocales.Count == 0) return;
        string[] locales = AvailableLocales.ToArray();
        int next = (Array.IndexOf(locales, CurrentLocale) + 1) % locales.Length;
        SetLocale(locales[next]);
    }

    /// <summary>
    /// Resolves <paramref name="key"/> to the active locale's
    /// translation. Returns the key literal when the translation is
    /// missing (graceful degradation, not an error).
    /// </summary>
    public string Translate(string key)
    {
        if (string.IsNullOrEmpty(key)) return key ?? string.Empty;
        string translated = TranslationServer.Translate(key);
        // TranslationServer returns the key when no translation is
        // found. We keep that behavior so consumers always get a
        // string they can display.
        return string.IsNullOrEmpty(translated) ? key : translated;
    }

    private bool LoadLocale(string locale)
    {
        UnloadCurrent();

        string path = $"{LocaleDirectory}/{locale}.po";
        Translation? translation = ResourceLoader.Load<Translation>(path);
        if (translation is null)
        {
            if (locale != DefaultLocale)
            {
                return LoadLocale(DefaultLocale);
            }
            CurrentLocale = locale;
            return false;
        }

        TranslationServer.AddTranslation(translation);
        TranslationServer.SetLocale(locale);
        CurrentLocale = locale;
        _loadedTranslation = translation;
        return true;
    }

    private void UnloadCurrent()
    {
        if (_loadedTranslation is null) return;
        TranslationServer.RemoveTranslation(_loadedTranslation);
        _loadedTranslation = null;
    }

    /// <summary>
    /// Reads the locale from the sidecar settings file. Returns the
    /// default locale when the file is missing, malformed, or
    /// contains a value not in <see cref="AvailableLocales"/>.
    /// </summary>
    private string TryRestoreLocale()
    {
        try
        {
            string path = ResolveSettingsPath();
            if (!File.Exists(path)) return DefaultLocale;
            string json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<SettingsFile>(json);
            if (settings is null || string.IsNullOrWhiteSpace(settings.Locale)) return DefaultLocale;
            string[] locales = AvailableLocales.ToArray();
            if (Array.IndexOf(locales, settings.Locale) < 0) return DefaultLocale;
            return settings.Locale;
        }
        catch (Exception)
        {
            return DefaultLocale;
        }
    }

    private void TryPersistLocale()
    {
        try
        {
            string path = ResolveSettingsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            var settings = new SettingsFile { Locale = CurrentLocale };
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch (Exception)
        {
            // Persistence is best effort. A failed write does not
            // break the runtime locale change.
        }
    }

    /// <summary>
    /// Sidecar settings file path. The world save slot directory
    /// already exists by the time the autoload runs (the controller
    /// loads the slot in its own <c>_Ready</c>). Falling back to
    /// <c>user://</c> if the slot path is unavailable.
    /// </summary>
    private static string ResolveSettingsPath()
    {
        try
        {
            string slotFile = WorldPersistence.SlotPath(WorldPersistence.PrimarySaveSlot);
            string? slotDir = Path.GetDirectoryName(slotFile);
            if (!string.IsNullOrEmpty(slotDir))
            {
                return Path.Combine(slotDir, SettingsFileName);
            }
        }
        catch (Exception)
        {
            // Fall through to the user:// fallback.
        }
        return ProjectSettings.GlobalizePath($"user://{SettingsFileName}");
    }

    /// <summary>On-disk schema for the sidecar settings file.</summary>
    private sealed class SettingsFile
    {
        public string Locale { get; set; } = DefaultLocale;
    }
}
