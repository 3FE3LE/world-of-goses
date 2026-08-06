using System.Linq;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// The <see cref="ResourceOpportunityKind.SpiritTrailSearch"/>
/// opportunity is the post-dawn motivation that
/// <c>docs/19_FIRST_NIGHT_AND_FIRE_SPIRIT.md</c> §12 promises: the
/// trail the spirit left leads to fire-blackened wood. These tests
/// assert the definition fits the existing
/// <see cref="ResourceExpeditionDefinition"/> shape without
/// introducing a new field, and that the kind round-trips through
/// the string-serialised opportunity log.
/// </summary>
public sealed class SpiritTrailOpportunityTests
{
    [Fact]
    public void Definition_ProducesWoodReward()
    {
        ResourceExpeditionDefinition definition =
            ResourceExpeditionRules.Definition(ResourceOpportunityKind.SpiritTrailSearch);

        Assert.Equal(ResourceType.Wood, definition.RewardResource);
        Assert.Equal(ResourceType.Food, definition.SupplyResource);
        Assert.Equal(1, definition.SupplyAmount);
    }

    [Fact]
    public void Definition_MatchesFallenWoodReturnCurve()
    {
        // The trail mirrors FallenWoodSearch's return curve: the only
        // thing that differs between the two opportunities is the
        // narrative framing, so a player who learnt one learns the other.
        ResourceExpeditionDefinition definition =
            ResourceExpeditionRules.Definition(ResourceOpportunityKind.SpiritTrailSearch);

        Assert.Equal(180, definition.DurationTicks);
        Assert.Equal(4, definition.SetbackReturn);
        Assert.Equal(6, definition.PartialReturn);
        Assert.Equal(8, definition.FullReturn);
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
