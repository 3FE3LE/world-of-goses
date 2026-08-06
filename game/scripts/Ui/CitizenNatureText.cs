#nullable enable
using System.Collections.Generic;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses.Presentation;

/// <summary>
/// Shared player-facing block for the identity a citizen carries under
/// DEC-0013: the Kovari cube axes, the lineage signature, and the combat
/// nature the cube face implies. Professional affinities are no longer produced
/// by onboarding, so panels render this instead.
///
/// Derived statistics are deliberately absent. They require an equipped weapon
/// and a resolved condition factor, neither of which has a source yet; see
/// CitizenStatisticsService.
///
/// <see cref="Format"/> returns raw English and touches no Godot API so it is
/// exercisable in Godot-free tests; <see cref="FormatLocalized"/> is the only
/// path that reaches the translation catalog.
///
/// Lines are joined with an explicit "\n". StringBuilder.AppendLine would emit
/// "\r\n" on Windows and Godot's Label renders the stray carriage return as an
/// extra blank line, double-spacing the whole block.
/// </summary>
public static class CitizenNatureText
{
    private const string RowIndent = "  ";

    public static string Format(
        FounderCubeProfile cube,
        LineageId lineage,
        CombatNature nature)
    {
        (WeaponFamily first, WeaponFamily second) =
            NaturalWeaponFamilies.For(nature.PhysicalExpression);
        return Join(new[]
        {
            "Combat nature",
            $"{RowIndent}Affinity: {ProfileCatalog.DisplayName(nature.ElementalAffinity)}"
                + $" · Physical expression: {ProfileCatalog.DisplayName(nature.PhysicalExpression)}",
            $"{RowIndent}Natural weapons: {ProfileCatalog.DisplayName(first)}"
                + $", {ProfileCatalog.DisplayName(second)}",
            $"Embodiment profile · Signature {CubeScoring.Signature(lineage)}",
            Pair("Body", cube.Body, cube.Bond, "Bond"),
            Pair("Stability", cube.Stability, cube.Impulse, "Impulse"),
            Pair("Mastery", cube.Mastery, cube.Reach, "Reach"),
        });
    }

    public static string FormatLocalized(
        FounderCubeProfile cube,
        LineageId lineage,
        CombatNature nature)
    {
        (WeaponFamily first, WeaponFamily second) =
            NaturalWeaponFamilies.For(nature.PhysicalExpression);
        return Join(new[]
        {
            UiText.Get("ui.citizen.combat_nature_heading"),
            RowIndent
                + UiText.Format(
                    "ui.citizen.affinity",
                    UiText.Get(ProfileCatalog.DisplayName(nature.ElementalAffinity)))
                + " · "
                + UiText.Format(
                    "ui.citizen.physical_expression",
                    UiText.Get(ProfileCatalog.DisplayName(nature.PhysicalExpression))),
            RowIndent + UiText.Format(
                "ui.citizen.natural_weapons",
                UiText.Get(ProfileCatalog.DisplayName(first)),
                UiText.Get(ProfileCatalog.DisplayName(second))),
            $"{UiText.Get("Perfil de encarnación")} · {UiText.Get("Firma")}"
                + $" {UiText.Get(CubeScoring.Signature(lineage))}",
            Pair(UiText.Get("Cuerpo"), cube.Body, cube.Bond, UiText.Get("Vínculo")),
            Pair(UiText.Get("Estabilidad"), cube.Stability, cube.Impulse, UiText.Get("Impulso")),
            Pair(UiText.Get("Dominio"), cube.Mastery, cube.Reach, UiText.Get("Alcance")),
        });
    }

    /// <summary>
    /// Two-line variant for surfaces that are read at a glance rather than
    /// studied — the founder arrival card holds for about two seconds, where the
    /// full block would be noise. Still satisfies DEC-0013's requirement that the
    /// card state the affinity and the three cube axes.
    /// </summary>
    public static string FormatCompactLocalized(
        FounderCubeProfile cube,
        LineageId lineage,
        CombatNature nature)
    {
        _ = lineage;
        return UiText.Format(
                "ui.citizen.affinity",
                UiText.Get(ProfileCatalog.DisplayName(nature.ElementalAffinity)))
            + " · "
            + UiText.Format(
                "ui.citizen.physical_expression",
                UiText.Get(ProfileCatalog.DisplayName(nature.PhysicalExpression)))
            + "\n"
            + $"{UiText.Get("Cuerpo")} {cube.Body}/{cube.Bond} · "
            + $"{UiText.Get("Estabilidad")} {cube.Stability}/{cube.Impulse} · "
            + $"{UiText.Get("Dominio")} {cube.Mastery}/{cube.Reach}";
    }

    private static string Pair(
        string leadingName,
        int leadingValue,
        int trailingValue,
        string trailingName) =>
        $"{RowIndent}{leadingName} {leadingValue} / {trailingValue} {trailingName}";

    private static string Join(IEnumerable<string> lines) => string.Join("\n", lines);
}
