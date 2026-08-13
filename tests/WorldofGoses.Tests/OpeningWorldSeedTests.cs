#nullable enable
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// GitHub #29 closure tests.
///
/// <para>The opening of a fresh world is two operations, not one:
/// <see cref="CityWorld.TryCreateHero"/> authors the Founder and starts
/// the first night, and the use case that wraps it is also responsible
/// for seeding the terrain the next step depends on. The two seeder
/// calls used to live on <c>CityWorldController.TryCompleteOnboarding</c>
/// and were correctly removed when persistence left the domain; the
/// regression this slice closes is that
/// <see cref="CityGameSession.CompleteOnboarding"/> never absorbed
/// them. A player reaching Founder arrival in the new flow saw an
/// empty world with no terrain, no forests, and no rudimentary
/// resources to gather — a city without a city.</para>
///
/// <para>The tests below fail loudly if the regression returns. They
/// guard three properties:</para>
/// <list type="number">
///   <item>A fresh <c>CompleteOnboarding</c> leaves a world the player
///         can act on: founding parcels, the two Forests, and one
///         patch of each EG-A0 ground resource.</item>
///   <item>The opener still honours the prior invariants: the
///         materialised weapon from <c>HeroCreationRequest</c> is
///         equipped, the first night is active, and the
///         seeder does not double the topology on a second call.</item>
///   <item>Save and load round-trip preserves what the opener seeded
///         and remains idempotent when the load path re-runs the
///         same seeder.</item>
/// </list>
/// </summary>
public sealed class OpeningWorldSeedTests
{
    [Fact]
    public void CompleteOnboarding_SeedsForestsAndGroundPatchesBeforeReturning()
    {
        // Regression guard for the opening bug: a successful
        // CompleteOnboarding must leave a city the player can act on.
        // Before #29 the seeder calls were dropped during the
        // Application/Persistence split and the world came back empty.
        CityGameSession session = new();
        CitizenProfile profile = TestHelpers.NewProfile();
        // The two natural families the cube reaches depend on the
        // fixture's lineage and element. Pick the first one so the
        // request names a real choice; the domain re-validates
        // anything else and a regression there would surface here.
        (WeaponFamily chosen, _) = NaturalWeaponFamilies.For(
            profile.CombatNature.PhysicalExpression);
        HeroCreationRequest request = new(
            "Aster",
            profile,
            profile.Gender,
            MaterializedWeaponFamily: chosen);

        HeroCreationResult result = session.CompleteOnboarding(request);

        Assert.True(result.IsSuccess, result.Outcome.ToString());
        CityWorld world = session.World;

        // Founding parcels must be readable. A fresh city exposes the
        // three horizontal parcels the natural-resource layout planner
        // needs before it can place the EG-A0 patches below.
        Assert.True(world.Parcels.Count >= 3,
            $"Expected at least 3 founding parcels after CompleteOnboarding, "
            + $"got {world.Parcels.Count}.");

        // Two Forests is the documented opener. The same number the
        // load-time repair in CityWorldController.TryLoadFromPrimarySlot
        // produces, and the same number a save/load round-trip must
        // preserve. An empty world returns 0 here.
        int forestCount = world.Buildings.Values
            .Count(building => building.Kind == BuildingKind.Forest);
        Assert.Equal(2, forestCount);

        // The EG-A0 ground distribution: 14 Branches, 6 Plant Fiber,
        // 6 Small Stone, 8 Wild Food. Each type appears at least once.
        // The two Wood patches come from SeedStartingForests and are
        // checked separately below.
        Assert.True(HasPatchOfType(world, ResourceType.Branches));
        Assert.True(HasPatchOfType(world, ResourceType.PlantFiber));
        Assert.True(HasPatchOfType(world, ResourceType.SmallStone));
        Assert.True(HasPatchOfType(world, ResourceType.WildFood));

        // The opener is the same use case that landed the founder's
        // weapon (#26). A regression that broke one seam often breaks
        // the other; assert both so the next refactor cannot trade
        // them off.
        Assert.NotNull(world.Hero);
        Assert.NotNull(world.Hero.EquipmentLoadout.Weapon);
        Assert.Equal(chosen, world.Hero.EquipmentLoadout.Weapon.Family);

        // The first night must be active. The Founder arrival script
        // gates the spirit on this; a session that returns
        // success without starting the night is a regression even
        // when the world is correctly seeded.
        Assert.True(world.IsFirstNightActive);
    }

    [Fact]
    public void CompleteOnboarding_RoundTripsThroughSaveAndLoadWithoutDoublingTopology()
    {
        // The seeder calls are now part of CompleteOnboarding, but
        // TryLoadFromPrimarySlot still re-runs them. A regression
        // that turned the seeders into a non-idempotent appender
        // would double the topology here.
        CityGameSession first = new();
        CitizenProfile profile = TestHelpers.NewProfile();
        Assert.True(first.CompleteOnboarding(new HeroCreationRequest(
            "Aster",
            profile,
            profile.Gender)).IsSuccess);

        CityWorld restored = WorldPersistence.FromSave(
            WorldPersistence.DeserializeFromJson(
                WorldPersistence.SerializeToJson(WorldPersistence.Capture(first.World))));

        // Re-running the same seeders — what the load path does — must
        // not add a third Forest, must not duplicate the four EG-A0
        // patches, and must keep the existing parcels intact.
        restored.SeedStartingForests();
        restored.SeedStartingOpportunities();

        int forestCount = restored.Buildings.Values
            .Count(building => building.Kind == BuildingKind.Forest);
        Assert.Equal(2, forestCount);

        // 4 EG-A0 patches + 2 Wood patches from the forests = 6.
        // A regression that turned SeedStartingForests into an
        // appender would push this past 6.
        Assert.Equal(6, restored.NaturalResourcePatches.Count);
    }

    [Fact]
    public void CompleteOnboarding_ProducesEnoughBranchesAndStoneForFirstCampfire()
    {
        // The acceptance criterion named in #29: a fresh opener must
        // hand the player the 3 Branches + 2 Small Stone the Campfire
        // recipe debits up front. The seeder distributes 14 Branches
        // (7 units × 2 each) and 6 Small Stone (3 × 2) — more than
        // the recipe needs, so the camp can be authorised without
        // gathering a third patch.
        CityGameSession session = new();
        CitizenProfile profile = TestHelpers.NewProfile();
        Assert.True(session.CompleteOnboarding(new HeroCreationRequest(
            "Aster",
            profile,
            profile.Gender)).IsSuccess);

        CityWorld world = session.World;
        int branches = world.NaturalResourcePatches.Values
            .Where(patch => patch.ResourceType == ResourceType.Branches)
            .Sum(patch => patch.TotalReserve);
        int smallStone = world.NaturalResourcePatches.Values
            .Where(patch => patch.ResourceType == ResourceType.SmallStone)
            .Sum(patch => patch.TotalReserve);

        Assert.True(branches >= 3, $"Branches={branches}, need >= 3 for the Campfire recipe.");
        Assert.True(smallStone >= 2, $"SmallStone={smallStone}, need >= 2 for the Campfire recipe.");
    }

    private static bool HasPatchOfType(CityWorld world, ResourceType type) =>
        world.NaturalResourcePatches.Values
            .Any(patch => patch.ResourceType == type);
}
