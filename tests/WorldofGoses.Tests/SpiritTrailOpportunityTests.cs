using System.Linq;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// The <see cref="ResourceOpportunityKind.SpiritTrailSearch"/>
/// opportunity is the post-dawn motivation that
/// <c>docs/systems/first-night.md</c> §12 promises: the
/// trail the spirit left becomes the first narrative expedition objective,
/// not a resource conversion. The kind still round-trips through the
/// string-serialised opportunity log.
/// </summary>
public sealed class SpiritTrailOpportunityTests
{
    [Fact]
    public void Definition_RequiresNoSupplyAndProducesDiscovery()
    {
        ResourceExpeditionDefinition definition =
            ResourceExpeditionRules.Definition(ResourceOpportunityKind.SpiritTrailSearch);

        Assert.Equal(ExpeditionSupplyRequirement.None, definition.SupplyRequirement);
        Assert.Equal(ExpeditionReward.Discovery, definition.Reward);
        Assert.Null(definition.SupplyResource);
        Assert.Null(definition.RewardResource);
    }

    [Fact]
    public void Definition_UsesNamedFourHourRouteWithoutMaterialReturnCurve()
    {
        ResourceExpeditionDefinition definition =
            ResourceExpeditionRules.Definition(ResourceOpportunityKind.SpiritTrailSearch);

        Assert.Equal(4 * GameClock.TicksPerInGameHour, definition.DurationTicks);
        Assert.Equal(ExpeditionTiming.SpiritTrailDurationTicks, definition.DurationTicks);
        Assert.Equal(0, definition.SetbackReturn);
        Assert.Equal(0, definition.PartialReturn);
        Assert.Equal(0, definition.FullReturn);
    }

    [Fact]
    public void Definition_ExposesADisplayName()
    {
        ResourceExpeditionDefinition definition =
            ResourceExpeditionRules.Definition(ResourceOpportunityKind.SpiritTrailSearch);

        Assert.False(string.IsNullOrWhiteSpace(definition.DisplayName));
    }

    [Fact]
    public void TheKindEnumSerializesAsAStringAndParsesBack()
    {
        // Resource opportunities persist as their enum name string
        // (see WorldPersistence). Enum.TryParse must round-trip the
        // new value so saves already on disk do not need a schema bump.
        string serialized = ResourceOpportunityKind.SpiritTrailSearch.ToString();
        Assert.True(
            System.Enum.TryParse(serialized, true, out ResourceOpportunityKind parsed));
        Assert.Equal(ResourceOpportunityKind.SpiritTrailSearch, parsed);
    }

    [Fact]
    public void Snapshot_IsLocked_BeforeSpiritDeparts()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        ExpeditionPlanningSnapshot snapshot = ExpeditionPlanningSnapshot.From(world);
        Assert.False(snapshot.SpiritTrailUnlocked);
    }

    [Fact]
    public void Snapshot_IsUnlocked_AfterSpiritDeparts()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();
        DriveNightToSleep(world);
        Assert.True(world.TryCloseFirstNightDialogue());
        ExpeditionPlanningSnapshot snapshot = ExpeditionPlanningSnapshot.From(world);
        Assert.True(snapshot.SpiritTrailUnlocked);
    }

    [Fact]
    public void OpportunityIsSeededOnTheSameTickAsTheDawn()
    {
        // Without this, the SpiritTrail button would be visible but the
        // player could not dispatch — no ResourceOpportunity of that
        // kind would exist in the world yet.
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();
        DriveNightToSleep(world);

        // Before the night concludes, the spirit-trail opportunity must
        // not exist (the trail is unreadable while the spirit is in
        // the flame).
        Assert.DoesNotContain(
            world.ResourceOpportunities.Values,
            o => o.Kind == ResourceOpportunityKind.SpiritTrailSearch);

        Assert.True(world.TryCloseFirstNightDialogue());

        Assert.Contains(
            world.ResourceOpportunities.Values,
            o => o.Kind == ResourceOpportunityKind.SpiritTrailSearch);
    }

    private static void DriveNightToSleep(CityWorld world)
    {
        FirstNightState night = world.FirstNight!;
        ConstructionProject? project = null;
        int safety = 32;
        while (night.Stage < FirstNightStage.Sleeping && safety-- > 0)
        {
            if (FirstNightRules.WaitsForModule(night.Stage))
            {
                FoundingSiteModule module = FirstNightRules.ModuleFor(night.Stage);
                if (project is null)
                {
                    project = AuthorizeCampfire(world);
                    CompleteActiveModule(world, project);
                }
                else
                {
                    CompleteModule(world, project, module);
                }
                continue;
            }
            Assert.True(world.TryCloseFirstNightDialogue(), $"Stalled at {night.Stage}.");
        }
    }

    private static ConstructionProject AuthorizeCampfire(CityWorld world)
    {
        DepositCost(world, FoundingSiteModule.Campfire);
        ConstructionAuthorizationResult result =
            world.TryAuthorizeConstruction(ConstructionKind.FoundingSite);
        Assert.True(result.IsSuccess, result.Outcome.ToString());
        return world.Projects.Values.Single();
    }

    private static void CompleteModule(
        CityWorld world,
        ConstructionProject project,
        FoundingSiteModule module)
    {
        DepositCost(world, module);
        ConstructionAuthorizationResult result =
            world.TryAuthorizeFoundingSiteModule(project.Id, module);
        Assert.True(result.IsSuccess, result.Outcome.ToString());
        CompleteActiveModule(world, project);
    }

    private static void CompleteActiveModule(CityWorld world, ConstructionProject project)
    {
        project.Progress = project.RequiredWork;
        world.AdvanceWorldTick();
    }

    private static void DepositCost(CityWorld world, FoundingSiteModule module)
    {
        foreach (RecipeInput input in FoundingSiteRules.InputsFor(module))
        {
            world.Resources.DepositToCityInventory(input.Resource, input.Amount);
        }
    }
}
