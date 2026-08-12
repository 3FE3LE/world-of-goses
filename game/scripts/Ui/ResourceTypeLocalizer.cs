#nullable enable
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
    /// Falls back to the lowercased enum name when the resource is
    /// unknown so a future slice that adds a value cannot produce a
    /// silent "missing translation" in shipped UI.
    /// </summary>
    public static string Label(ResourceType resource) => UiText.Get(Key(resource));

    /// <summary>
    /// Returns the PO key for <paramref name="resource"/> without
    /// going through <see cref="UiText.Get"/>. Useful for tooltips
    /// that compose a translation themselves.
    /// </summary>
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
        _ => resource.ToString().ToLowerInvariant(),
    };
}
