#nullable enable
using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Ui;

/// <summary>
/// Explicit mapper from <see cref="GenderId"/> to its i18n key, the
/// sibling of <see cref="ResourceTypeLocalizer"/>.
///
/// <para>The hero profile used to call
/// <c>UiText.Get(hero.Gender.ToString())</c>. That happened to work —
/// both catalogs carry "Feminine" and "Masculine" — which is precisely
/// what made it dangerous: renaming either C# member would have moved the
/// PO key with it and silently rendered the raw enum name, with nothing
/// failing until a player saw it. The keys below are deliberately
/// identical to the existing msgids, so this is a coupling change and not
/// a catalog change.</para>
/// </summary>
public static class GenderIdLocalizer
{
    /// <summary>Returns the localised label for <paramref name="gender"/>.</summary>
    public static string Label(GenderId gender) => UiText.Get(Key(gender));

    /// <summary>
    /// Returns the PO key for <paramref name="gender"/>. Exhaustive by
    /// design: no enum-name fallback, so a new member is a loud failure
    /// rather than an untranslated string in shipped UI.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value has no explicit localisation key.
    /// </exception>
    public static string Key(GenderId gender) => gender switch
    {
        GenderId.Feminine => "Feminine",
        GenderId.Masculine => "Masculine",
        _ => throw new ArgumentOutOfRangeException(
            nameof(gender),
            gender,
            "GenderId has no explicit i18n key. Add an arm to GenderIdLocalizer.Key "
                + "and the msgid to every game/locale catalog."),
    };
}
