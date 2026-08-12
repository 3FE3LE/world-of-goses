#nullable enable
using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Ui;

/// <summary>
/// Explicit mapper from <see cref="ResourceType"/> to its i18n key. Lives
/// in Presentation so the Domain enum stays free of PO knowledge;
/// every call site that wanted a localised resource label was reaching
/// for <c>UiText.Get(resourceType.ToString().ToLowerInvariant())</c>,
/// which silently broke whenever an enum value was renamed.
///
/// <para>Architecture Hardening A12 codifies the rule: Domain and
/// Application pass the typed enum value, Presentation chooses the
/// PO key. A future slice that renames a resource or adds one adds
/// a single switch arm here, not a grep across every view.</para>
/// </summary>
public static class ResourceTypeLocalizer
{
    /// <summary>
    /// Returns the localised label for <paramref name="resource"/>.
    /// </summary>
    public static string Label(ResourceType resource) => UiText.Get(Key(resource));

    /// <summary>
    /// Returns the PO key for <paramref name="resource"/> without
    /// going through <see cref="UiText.Get"/>. Useful for tooltips
    /// that compose a translation themselves.
    ///
    /// <para>The mapping is exhaustive and has no enum-name fallback.
    /// It used to end in <c>_ =&gt; resource.ToString().ToLowerInvariant()</c>,
    /// which quietly reinstated the exact coupling this class exists to
    /// remove: a new resource compiled, ran, and shipped a raw enum name
    /// into the UI, and renaming an existing one silently changed a PO
    /// key. Throwing is the point — an unmapped value must be a loud,
    /// early failure, and
    /// <c>ResourceTypeLocalizationContractTests</c> turns it into a
    /// failing test the moment the enum grows rather than a defect a
    /// player finds.</para>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The resource has no explicit localisation key. Add one arm here
    /// and the matching msgid to every catalog under
    /// <c>game/locale/</c>.
    /// </exception>
    public static string Key(ResourceType resource) => resource switch
    {
        ResourceType.Stone => "stone",
        ResourceType.Food => "food",
        ResourceType.Iron => "iron",
        ResourceType.Potions => "potions",
        ResourceType.Wood => "wood",
        ResourceType.Branches => "branches",
        ResourceType.PlantFiber => "plantfiber",
        ResourceType.SmallStone => "smallstone",
        ResourceType.WildFood => "wildfood",
        _ => throw new ArgumentOutOfRangeException(
            nameof(resource),
            resource,
            "ResourceType has no explicit i18n key. Add an arm to "
                + "ResourceTypeLocalizer.Key and the msgid to every game/locale catalog; "
                + "deriving the key from the enum name is what A12 removed."),
    };
}
