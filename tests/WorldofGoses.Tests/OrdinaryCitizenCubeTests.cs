using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Every non-founder citizen needs a cube that varies by lineage and seed,
/// not the bare vertex that DEC-0018 flagged as M-29 (every Body lineage
/// landed on Fracture, every Bond lineage on Poisoning, and two migrants
/// with the same lineage were statistically the same person).
///
/// The generation is a pure function of <c>(lineage, seed)</c> on top of
/// the canonical 60/40 vertex and the ±8 onboarding cap. Same seed, same
/// cube. Two seeds, two cubes. The pair sum stays 100, every axis stays in
/// 32–68, and the highest face is always one of the lineage's three.
/// </summary>
public sealed class OrdinaryCitizenCubeTests
{
    public static IEnumerable<object[]> LineagesWithCube() =>
        ProfileCatalog.Lineages.Select(definition => new object[] { definition.Id });

    [Theory]
    [MemberData(nameof(LineagesWithCube))]
    public void GenerateOrdinaryProfile_IsDeterministicForSameSeed(LineageId lineage)
    {
        FounderCubeProfile a = CubeScoring.GenerateOrdinaryProfile(lineage, 17);
        FounderCubeProfile b = CubeScoring.GenerateOrdinaryProfile(lineage, 17);

        Assert.Equal(a, b);
    }

    [Theory]
    [MemberData(nameof(LineagesWithCube))]
    public void GenerateOrdinaryProfile_DiffersForDifferentSeeds(LineageId lineage)
    {
        FounderCubeProfile a = CubeScoring.GenerateOrdinaryProfile(lineage, 17);
        FounderCubeProfile b = CubeScoring.GenerateOrdinaryProfile(lineage, 23);

        // Distinct seeds must not collide. A cube is a 6-tuple of ints; the
        // chance of accidental equality by hash alone is negligible, but the
        // assertion is the safety belt the repo actually needs.
        Assert.NotEqual(a, b);
    }

    [Theory]
    [MemberData(nameof(LineagesWithCube))]
    public void GenerateOrdinaryProfile_StaysInsideTheOnboardingEnvelope(LineageId lineage)
    {
        for (int seed = -50; seed <= 50; seed++)
        {
            FounderCubeProfile cube = CubeScoring.GenerateOrdinaryProfile(lineage, seed);

            Assert.InRange(cube.Body, 32, 68);
            Assert.InRange(cube.Bond, 32, 68);
            Assert.InRange(cube.Stability, 32, 68);
            Assert.InRange(cube.Impulse, 32, 68);
            Assert.InRange(cube.Domain, 32, 68);
            Assert.InRange(cube.Reach, 32, 68);
            Assert.Equal(100, cube.Body + cube.Bond);
            Assert.Equal(100, cube.Stability + cube.Impulse);
            Assert.Equal(100, cube.Domain + cube.Reach);
        }
    }

    [Theory]
    [MemberData(nameof(LineagesWithCube))]
    public void GenerateOrdinaryProfile_LandsInsideItsLineagesExpressions(LineageId lineage)
    {
        PhysicalExpression[] allowed = CubeExpression.NaturallyAvailableTo(lineage).ToArray();

        for (int seed = -50; seed <= 50; seed++)
        {
            FounderCubeProfile cube = CubeScoring.GenerateOrdinaryProfile(lineage, seed);
            PhysicalExpression derived = CubeExpression.Derive(cube);

            Assert.Contains(derived, allowed);
        }
    }

    [Fact]
    public void GenerateOrdinaryProfile_SweepOverSeedsReachesEveryExpression_AndOnlyThose()
    {
        foreach ((LineageId lineage, PhysicalExpression[] expected) in
                 CubeExpressionTestsHelpers.ExpectedByLineage)
        {
            var produced = new HashSet<PhysicalExpression>();
            for (int seed = -100; seed <= 100; seed++)
            {
                FounderCubeProfile cube = CubeScoring.GenerateOrdinaryProfile(lineage, seed);
                produced.Add(CubeExpression.Derive(cube));
            }

            Assert.Equal(
                expected.OrderBy(e => e).ToArray(),
                produced.OrderBy(e => e).ToArray());
        }
    }

    [Theory]
    [MemberData(nameof(GoldenVectors))]
    // Golden vectors: the FNV-1a mixing of seed, lineage, axis pins the
    // exact cube. A change in any of those three inputs would move at least
    // one axis; if a future refactor silently rewrites the hash, this is
    // the test that catches it before the save format ships.
    public void GenerateOrdinaryProfile_HasFixedCrossProcessVectors(
        int seed, LineageId lineage,
        int body, int bond, int stability, int impulse, int domain, int reach)
    {
        FounderCubeProfile cube = CubeScoring.GenerateOrdinaryProfile(lineage, seed);

        Assert.Equal(body, cube.Body);
        Assert.Equal(bond, cube.Bond);
        Assert.Equal(stability, cube.Stability);
        Assert.Equal(impulse, cube.Impulse);
        Assert.Equal(domain, cube.Domain);
        Assert.Equal(reach, cube.Reach);
    }

    public static TheoryData<int, LineageId, int, int, int, int, int, int> GoldenVectors => new()
    {
        { 2, LineageId.Kovari, 55, 45, 39, 61, 63, 37 },
        { 7, LineageId.Ardhen, 67, 33, 63, 37, 58, 42 },
        { 13, LineageId.Caelith, 35, 65, 48, 52, 44, 56 },
    };

    [Fact]
    public void ProspectSurvivesSaveAndLoadWithTheSameCube()
    {
        // The PendingProspect persisted on disk must round-trip to the same
        // cube — otherwise a save/load would shuffle a pending migrant's
        // nature out from under the player. The cube and the seed are the
        // two halves that have to survive together.
        CityWorld world = TestHelpers.WorldWithHome();
        AddTownHall(world);
        Assert.Equal(CityWorld.MigrantOutcome.Success,
            world.TryHostExpeditionProspect("Inara"));

        int seed = world.PendingProspect!.Seed;
        FounderCubeProfile cubeBefore = world.PendingProspect.Profile.CubeProfile;

        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.DeserializeFromJson(
            WorldPersistence.SerializeToJson(WorldPersistence.Capture(world))));

        Assert.Equal(seed, restored.PendingProspect!.Seed);
        Assert.Equal(cubeBefore, restored.PendingProspect.Profile.CubeProfile);
    }

    [Fact]
    public void MigrantNameIsOutOfPhaseWithLineageOverSeeds()
    {
        // Pre-rename, both the name index and the lineage index cycled modulo
        // 8 with the same length and a constant shift, so every (name,
        // lineage) pair was a deterministic one-to-one. After the mix change
        // the cycles are out of phase: a small sweep must surface several
        // distinct pairings, not one.
        var pairings = new HashSet<(string Name, LineageId Lineage)>();
        for (int seed = 2; seed <= 12; seed++)
        {
            LineageId lineage = ProfileCatalog.Lineages[
                seed % ProfileCatalog.Lineages.Count].Id;
            pairings.Add((CityWorld.MigrantNameForSeed(seed), lineage));
        }

        Assert.True(
            pairings.Count >= 4,
            "Expected at least four distinct (name, lineage) pairings across " +
            "eleven consecutive seeds; the cycles are still too tightly coupled.");
    }

    private static void AddTownHall(CityWorld world)
    {
        world.RegisterBuilding(TestHelpers.NewBuilding(
            id: new BuildingId(51),
            kind: BuildingKind.TownHall,
            workerCapacity: 0,
            visualCapacity: 0,
            baseProductionPerWorker: 0,
            storageCapacity: 0,
            displayName: "Test Town Hall",
            resourceLabel: "Prospect",
            resourceUnit: "prospect"));
    }
}

internal static class CubeExpressionTestsHelpers
{
    public static readonly IReadOnlyDictionary<LineageId, PhysicalExpression[]> ExpectedByLineage =
        new Dictionary<LineageId, PhysicalExpression[]>
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
}