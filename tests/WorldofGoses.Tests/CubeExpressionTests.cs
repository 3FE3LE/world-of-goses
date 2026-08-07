using System;
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// The physical expression comes from the Kovari Cube, not from the elemental
/// affinity. These tests pin the correction: the two are independent axes, the
/// cube alone decides, and each lineage vertex admits exactly three expressions.
/// </summary>
public sealed class CubeExpressionTests
{
    /// <summary>The §5 invariant, stated as data so the algorithm has to earn it.</summary>
    private static readonly Dictionary<LineageId, PhysicalExpression[]> ExpectedByLineage = new()
    {
        [LineageId.Ardhen] = new[] { PhysicalExpression.Fracture, PhysicalExpression.Paralysis, PhysicalExpression.Bleeding },
        [LineageId.Eirune] = new[] { PhysicalExpression.Fracture, PhysicalExpression.Paralysis, PhysicalExpression.Knockdown },
        [LineageId.Kovari] = new[] { PhysicalExpression.Fracture, PhysicalExpression.Stunning, PhysicalExpression.Bleeding },
        [LineageId.Vaelun] = new[] { PhysicalExpression.Fracture, PhysicalExpression.Stunning, PhysicalExpression.Knockdown },
        [LineageId.Orveth] = new[] { PhysicalExpression.Poisoning, PhysicalExpression.Paralysis, PhysicalExpression.Bleeding },
        [LineageId.Myrven] = new[] { PhysicalExpression.Poisoning, PhysicalExpression.Paralysis, PhysicalExpression.Knockdown },
        [LineageId.Theryn] = new[] { PhysicalExpression.Poisoning, PhysicalExpression.Stunning, PhysicalExpression.Bleeding },
        [LineageId.Caelith] = new[] { PhysicalExpression.Poisoning, PhysicalExpression.Stunning, PhysicalExpression.Knockdown },
    };

    [Theory]
    [InlineData(CubeFace.Body, PhysicalExpression.Fracture)]
    [InlineData(CubeFace.Bond, PhysicalExpression.Poisoning)]
    [InlineData(CubeFace.Stability, PhysicalExpression.Paralysis)]
    [InlineData(CubeFace.Impulse, PhysicalExpression.Stunning)]
    [InlineData(CubeFace.Domain, PhysicalExpression.Bleeding)]
    [InlineData(CubeFace.Reach, PhysicalExpression.Knockdown)]
    public void EveryCubeFaceMapsToItsExpression(CubeFace face, PhysicalExpression expected) =>
        Assert.Equal(expected, CubeExpression.ForFace(face));

    [Fact]
    public void TheExpressionIgnoresTheElementalAffinityEntirely()
    {
        // The obsolete rule made these six natures six different expressions.
        FounderCubeProfile cube = CubeScoring.ComputeCubeVertex(LineageId.Vaelun);
        PhysicalExpression fromCube = CubeExpression.Derive(cube);

        foreach (ElementalAffinity affinity in Enum.GetValues<ElementalAffinity>())
        {
            CombatNature nature = CombatNature.FromCube(affinity, cube);

            Assert.Equal(affinity, nature.ElementalAffinity);
            Assert.Equal(fromCube, nature.PhysicalExpression);
        }
    }

    [Fact]
    public void IdenticalCubesWithDifferentAffinitiesShareOneExpression()
    {
        var cube = new FounderCubeProfile(56, 44, 64, 36, 52, 48);

        CombatNature fire = CombatNature.FromCube(ElementalAffinity.Fire, cube);
        CombatNature aether = CombatNature.FromCube(ElementalAffinity.Aether, cube);

        Assert.NotEqual(fire.ElementalAffinity, aether.ElementalAffinity);
        Assert.Equal(fire.PhysicalExpression, aether.PhysicalExpression);
        Assert.Equal(PhysicalExpression.Paralysis, fire.PhysicalExpression);
    }

    [Fact]
    public void EveryLineageDeclaresExactlyItsThreeExpressions()
    {
        foreach ((LineageId lineage, PhysicalExpression[] expected) in ExpectedByLineage)
        {
            Assert.Equal(
                expected.OrderBy(e => e).ToArray(),
                CubeExpression.NaturallyAvailableTo(lineage).OrderBy(e => e).ToArray());
        }
    }

    [Fact]
    public void EveryReachableCubeLandsInsideItsLineageSet_AndAllThreeAreReachable()
    {
        // Walk the whole onboarding-reachable space: ±8 aggregate on each of the
        // three axes, which is exactly what CubeScoring.Recalculate can produce.
        foreach ((LineageId lineage, PhysicalExpression[] expected) in ExpectedByLineage)
        {
            var produced = new HashSet<PhysicalExpression>();
            FounderCubeProfile vertex = CubeScoring.ComputeCubeVertex(lineage);

            for (int body = -CubeScoring.MaximumOnboardingShift; body <= CubeScoring.MaximumOnboardingShift; body++)
            for (int stability = -CubeScoring.MaximumOnboardingShift; stability <= CubeScoring.MaximumOnboardingShift; stability++)
            for (int domain = -CubeScoring.MaximumOnboardingShift; domain <= CubeScoring.MaximumOnboardingShift; domain++)
            {
                var cube = new FounderCubeProfile(
                    vertex.Body + body, vertex.Bond - body,
                    vertex.Stability + stability, vertex.Impulse - stability,
                    vertex.Domain + domain, vertex.Reach - domain);

                PhysicalExpression derived = CubeExpression.Derive(cube);
                produced.Add(derived);
                Assert.Contains(derived, expected);
            }

            Assert.Equal(expected.OrderBy(e => e).ToArray(), produced.OrderBy(e => e).ToArray());
        }
    }

    [Fact]
    public void NoLineageEverProducesOneOfItsOppositeExpressions()
    {
        // The complement of the expected set. Under 60/40 with the ±8 cap a
        // favoured face never drops below 52 and an opposite never rises above
        // 48, so this holds by construction — there is no blacklist enforcing it,
        // and this test is what proves none is needed.
        foreach ((LineageId lineage, PhysicalExpression[] expected) in ExpectedByLineage)
        {
            IEnumerable<PhysicalExpression> forbidden =
                Enum.GetValues<PhysicalExpression>().Except(expected);

            foreach (PhysicalExpression unreachable in forbidden)
            {
                Assert.DoesNotContain(unreachable, CubeExpression.NaturallyAvailableTo(lineage));
            }
        }
    }

    [Fact]
    public void EveryExpressionBelongsToExactlyFourLineages()
    {
        foreach (PhysicalExpression expression in Enum.GetValues<PhysicalExpression>())
        {
            int lineages = ProfileCatalog.Lineages
                .Count(definition => CubeExpression.NaturallyAvailableTo(definition.Id).Contains(expression));

            Assert.Equal(4, lineages);
        }
    }

    [Fact]
    public void EveryExpressionKeepsExactlyTwoNaturalWeaponFamilies()
    {
        foreach (PhysicalExpression expression in Enum.GetValues<PhysicalExpression>())
        {
            (WeaponFamily first, WeaponFamily second) = NaturalWeaponFamilies.For(expression);

            Assert.NotEqual(first, second);
            Assert.True(NaturalWeaponFamilies.Contains(expression, first));
            Assert.True(NaturalWeaponFamilies.Contains(expression, second));
        }

        // Twelve families, six expressions, no family shared by two of them.
        WeaponFamily[] all = Enum.GetValues<PhysicalExpression>()
            .SelectMany(expression =>
            {
                (WeaponFamily first, WeaponFamily second) = NaturalWeaponFamilies.For(expression);
                return new[] { first, second };
            })
            .ToArray();

        Assert.Equal(12, all.Length);
        Assert.Equal(12, all.Distinct().Count());
    }

    [Fact]
    public void TheTieOrderIsExplicitAndDecidesEveryDraw()
    {
        Assert.Equal(
            new[] { CubeFace.Body, CubeFace.Bond, CubeFace.Stability, CubeFace.Impulse, CubeFace.Domain, CubeFace.Reach },
            CubeExpression.CanonicalTieOrder);

        // A bare vertex is a three-way draw on 60, which is the ordinary case for
        // every citizen who never went through onboarding — not an edge case.
        Assert.Equal(CubeFace.Body, CubeExpression.HighestFace(CubeScoring.ComputeCubeVertex(LineageId.Ardhen)));
        Assert.Equal(CubeFace.Bond, CubeExpression.HighestFace(CubeScoring.ComputeCubeVertex(LineageId.Caelith)));

        // Two-way draws resolve to whichever face comes first in that order.
        Assert.Equal(
            CubeFace.Stability,
            CubeExpression.HighestFace(new FounderCubeProfile(44, 56, 64, 36, 64, 36)));
        Assert.Equal(
            CubeFace.Impulse,
            CubeExpression.HighestFace(new FounderCubeProfile(44, 56, 36, 64, 64, 36)));
    }

    [Fact]
    public void TheSameCubeAlwaysAnswersTheSame()
    {
        var cube = new FounderCubeProfile(60, 40, 60, 40, 60, 40);

        for (int repetition = 0; repetition < 50; repetition++)
        {
            Assert.Equal(PhysicalExpression.Fracture, CubeExpression.Derive(cube));
        }
    }

    [Fact]
    public void EquipmentSupportNeverReachesTheExpression()
    {
        // The effective profile adds gear support and can outrank the persisted
        // faces. The expression must read the persisted cube only, or a helmet
        // would change who someone is.
        var cube = new FounderCubeProfile(56, 44, 52, 48, 52, 48);
        var support = new GearSupportProfile(0, 0, 12, 0, 0, 0);
        EffectiveCubeProfile effective = EffectiveCubeProfile.From(cube, support);

        Assert.True(effective.Stability > effective.Body);
        Assert.Equal(PhysicalExpression.Fracture, CubeExpression.Derive(cube));
    }
}
