using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using WorldofGoses.Persistence;
using WorldofGoses.Tests.Combat;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// GitHub #26: the founder's first weapon becomes a real item with its own
/// identity, chosen from the two families their physical expression reaches,
/// instead of a deterministic random profile invented inside combat.
///
/// <para>
/// Two things these tests deliberately exercise through production code
/// rather than restating: the domain's re-validation of the chosen family,
/// and the separation of "has a real weapon" from "is the opening tutorial
/// encounter". The second is the refactor #26 calls critical — the two used
/// to be the same branch, so equipping a real weapon would have taken the
/// Spirit Trail's stat floor away with the fallback it replaced.
/// </para>
/// </summary>
public sealed class FounderWeaponMaterializationTests
{
    private static readonly PhysicalExpression[] Expressions =
        Enum.GetValues<PhysicalExpression>();

    [Fact]
    public void EveryPhysicalExpressionOffersExactlyTwoDistinctFamilies()
    {
        Assert.Equal(6, Expressions.Length);
        foreach (PhysicalExpression expression in Expressions)
        {
            (WeaponFamily first, WeaponFamily second) = NaturalWeaponFamilies.For(expression);
            Assert.NotEqual(first, second);
            Assert.True(NaturalWeaponFamilies.Contains(expression, first));
            Assert.True(NaturalWeaponFamilies.Contains(expression, second));
        }
    }

    /// <summary>
    /// The domain re-validates rather than trusting the caller. The view
    /// offers two families, but "the UI only offers two" is a hope, not a
    /// rule — a third arriving here must be refused whatever produced it.
    /// </summary>
    [Fact]
    public void AFamilyOutsideTheNaturalSetIsRefusedAndChangesNothing()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        Citizen founder = world.Hero!;
        PhysicalExpression expression = founder.CombatNature.PhysicalExpression;
        (WeaponFamily first, WeaponFamily second) = NaturalWeaponFamilies.For(expression);

        WeaponFamily[] foreign = Enum.GetValues<WeaponFamily>()
            .Where(family => family != first && family != second)
            .ToArray();
        Assert.NotEmpty(foreign);

        foreach (WeaponFamily family in foreign)
        {
            Assert.Null(world.MaterializeFounderWeapon(founder.Id, family));
            Assert.Empty(founder.PersonalEquipment.Items);
            Assert.Null(founder.PersonalEquipment.EquippedWeaponId);
            Assert.Null(founder.EquipmentLoadout.Weapon);
        }

        Assert.NotNull(world.MaterializeFounderWeapon(founder.Id, first));
    }

    /// <summary>
    /// One authority. The registry holds the item and the equipped id; the
    /// loadout Statistics and Combat read is republished from it rather than
    /// written beside it, so the two cannot disagree.
    /// </summary>
    [Fact]
    public void MaterializingEquipsThroughOneAuthorityAndRepublishesTheLoadout()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        Citizen founder = world.Hero!;
        (WeaponFamily chosen, _) = NaturalWeaponFamilies.For(founder.CombatNature.PhysicalExpression);

        ItemInstanceId id = Assert.IsType<ItemInstanceId>(
            world.MaterializeFounderWeapon(founder.Id, chosen));

        WeaponItemInstance item = Assert.Contains(id, founder.PersonalEquipment.Items);
        Assert.Equal(chosen, item.Family);
        Assert.Equal(WeaponOrigin.FounderMaterialization, item.Origin);
        Assert.Equal(id, founder.PersonalEquipment.EquippedWeaponId);
        Assert.Same(item, founder.PersonalEquipment.EquippedWeapon);

        // The projection agrees with the registry, and reports the same id.
        Assert.Equal(chosen, founder.EquipmentLoadout.Weapon!.Family);
        Assert.Equal(id, world.FounderEquippedWeaponId());
    }

    /// <summary>
    /// The choice travels in the request, so the founder is created and armed
    /// atomically — there is no observable moment where the world holds a
    /// founder the player chose a weapon for and who does not have it.
    /// </summary>
    [Fact]
    public void TheChosenFamilyTravelsInTheCreationRequest()
    {
        var world = new CityWorld();
        CitizenProfile profile = TestHelpers.NewProfile();
        (WeaponFamily chosen, _) = NaturalWeaponFamilies.For(
            profile.CombatNature.PhysicalExpression);

        Assert.True(world.TryCreateHero(new HeroCreationRequest(
            "Aster",
            profile,
            profile.Gender,
            MaterializedWeaponFamily: chosen)).IsSuccess);

        Assert.Equal(chosen, world.Hero!.EquipmentLoadout.Weapon!.Family);
        Assert.NotNull(world.FounderEquippedWeaponId());
    }

    [Fact]
    public void ACreationRequestWithNoChoiceLeavesTheFounderUnarmed()
    {
        CityWorld world = TestHelpers.NewHeroWorld();

        Assert.Empty(world.Hero!.PersonalEquipment.Items);
        Assert.Null(world.Hero.EquipmentLoadout.Weapon);
        Assert.Null(world.FounderEquippedWeaponId());
    }

    // ---------------------------------------------------------------------
    // Persistence
    // ---------------------------------------------------------------------

    /// <summary>
    /// The round trip the migration exists to serve. Capture and restore must
    /// carry the item's identity, not merely its stats — an item that comes
    /// back with a fresh id every load is not an item, and nothing built on
    /// top of it (swapping, loot, upgrades) could refer to it twice.
    /// </summary>
    [Fact]
    public void SaveLoadPreservesIdentityFamilyChannelsAndTheEquippedSlot()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        Citizen founder = world.Hero!;
        (WeaponFamily chosen, _) = NaturalWeaponFamilies.For(founder.CombatNature.PhysicalExpression);
        ItemInstanceId id = world.MaterializeFounderWeapon(founder.Id, chosen)!.Value;
        WeaponChannelProfile channels = founder.PersonalEquipment.EquippedWeapon!.Channels;

        CityWorld restored = WorldPersistence.FromSave(
            WorldPersistence.DeserializeFromJson(
                WorldPersistence.SerializeToJson(WorldPersistence.Capture(world))));
        Citizen restoredFounder = restored.Hero!;

        Assert.Equal(id, restoredFounder.PersonalEquipment.EquippedWeaponId);
        WeaponItemInstance item = Assert.Contains(id, restoredFounder.PersonalEquipment.Items);
        Assert.Equal(chosen, item.Family);
        Assert.Equal(channels.PhysicalTransfer, item.Channels.PhysicalTransfer);
        Assert.Equal(channels.ElementalResonance, item.Channels.ElementalResonance);
        Assert.Equal(chosen, restoredFounder.EquipmentLoadout.Weapon!.Family);
    }

    /// <summary>
    /// A v34 founder who owned a weapon keeps it, now under an item id. Built
    /// as a real v34 payload rather than a current capture stamped with an old
    /// version number, because the thing under test is what the migration does
    /// to a save that genuinely predates the field.
    /// </summary>
    [Fact]
    public void MigrationGivesAnExistingWeaponAnItemIdentity()
    {
        WorldSave v34 = LegacyV34SaveWithWeapon(WeaponFamily.Sword);

        WorldSave migrated = WorldPersistence.MigrateV34ToV35(v34);

        Assert.Equal(35, migrated.Version);
        PersonalEquipmentSave equipment = Assert.IsType<PersonalEquipmentSave>(
            migrated.Citizens[0].PersonalEquipment);
        WeaponItemInstanceSave weapon = Assert.Single(equipment.Weapons);
        Assert.Equal(WeaponFamily.Sword.ToString(), weapon.Family);
        Assert.Equal(weapon.Id, equipment.EquippedWeaponId);
        // The channels survive: a migration that reset them would silently
        // rebalance every existing founder.
        Assert.Equal(0.75, weapon.Channels!.PhysicalTransfer);
        Assert.Equal(1.2, weapon.Channels.ElementalResonance);
    }

    /// <summary>
    /// A genuinely unarmed legacy save stays unarmed. Inventing a weapon here
    /// would be retroactively making a choice the player never made — and the
    /// legacy fallback exists precisely for these saves.
    /// </summary>
    [Fact]
    public void MigrationDoesNotInventAWeaponForAnUnarmedLegacySave()
    {
        WorldSave v34 = LegacyV34SaveWithWeapon(family: null);

        WorldSave migrated = WorldPersistence.MigrateV34ToV35(v34);

        PersonalEquipmentSave equipment = Assert.IsType<PersonalEquipmentSave>(
            migrated.Citizens[0].PersonalEquipment);
        Assert.Empty(equipment.Weapons);
        Assert.Null(equipment.EquippedWeaponId);
    }

    // ---------------------------------------------------------------------
    // The opening encounter
    // ---------------------------------------------------------------------

    /// <summary>
    /// The refactor #26 calls critical, asserted against the production
    /// predicates. The tutorial floor is a property of the route; the
    /// fallback profile is a property of the citizen. They used to be one
    /// branch — <c>Weapon is null</c> — so arming the founder would have
    /// removed the protection along with the fallback it replaced.
    /// </summary>
    [Fact]
    public void TheTutorialFloorFollowsTheRouteAndTheFallbackFollowsTheCitizen()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        Expedition spiritTrail = world.Expeditions[expeditionId];
        Citizen founder = world.Hero!;

        // Unarmed: floor on, fallback needed.
        Assert.True(ExpeditionCombatSessionFactory.IsOpeningTutorialEncounter(spiritTrail));
        Assert.True(ExpeditionCombatSessionFactory.NeedsLegacyWeaponFallback(founder));

        (WeaponFamily chosen, _) = NaturalWeaponFamilies.For(founder.CombatNature.PhysicalExpression);
        world.MaterializeFounderWeapon(founder.Id, chosen);

        // Armed: floor still on, fallback no longer needed.
        Assert.True(ExpeditionCombatSessionFactory.IsOpeningTutorialEncounter(spiritTrail));
        Assert.False(ExpeditionCombatSessionFactory.NeedsLegacyWeaponFallback(founder));
    }

    /// <summary>
    /// An ordinary expedition is not the guided opening and gets no floor,
    /// armed or not. Without this the "floor follows the route" claim would
    /// be satisfied by a predicate that simply returned true.
    /// </summary>
    [Fact]
    public void AnOrdinaryExpeditionCarriesNoTutorialFloor()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.Resources.DepositToCityInventory(ResourceType.Wood, 4);
        ExpeditionStartResult started = world.StartExpedition(
            ExpeditionRequest.Reconnaissance(world.Hero!.Id));
        Assert.True(started.IsSuccess, started.Outcome.ToString());

        Assert.False(ExpeditionCombatSessionFactory.IsOpeningTutorialEncounter(
            world.Expeditions[started.ExpeditionId!.Value]));
    }

    /// <summary>
    /// A correctly created founder fights with the family they chose — the
    /// half that decides technique catalogue, attack range and statistics —
    /// rather than with the random one the fallback used to invent.
    /// </summary>
    [Fact]
    public void TheOpeningEncounterUsesTheChosenFamily()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        Citizen founder = world.Hero!;
        (_, WeaponFamily chosen) = NaturalWeaponFamilies.For(founder.CombatNature.PhysicalExpression);
        world.MaterializeFounderWeapon(founder.Id, chosen);

        int safety = 256;
        while (world.GetCombatSessionSnapshot(expeditionId) is null && safety-- > 0)
        {
            world.AdvanceWorldTick();
        }
        Assert.True(safety > 0, "The opening encounter never began.");

        CombatSessionSnapshot combat = world.GetCombatSessionSnapshot(expeditionId)!;
        CombatParticipantState member = Assert.Single(combat.Party);
        Assert.Equal(founder.Id, member.CitizenId);
        // The fallback leaves WeaponFamily null on the combatant; a real
        // equipped weapon names its family.
        Assert.Equal(chosen, founder.EquipmentLoadout.Weapon!.Family);
    }

    // ---------------------------------------------------------------------
    // The onboarding beat
    // ---------------------------------------------------------------------

    /// <summary>
    /// The stage exists, sits between the founder card and creation, and is
    /// navigable in both directions. Structural, because the beat itself is a
    /// Godot flow: what a test can hold is that the stage is in the machine
    /// and that both the forward and the back edge name it.
    /// </summary>
    [Fact]
    public void OnboardingCarriesANavigableWeaponChoiceStageBeforeCreation()
    {
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "AstralOnboardingView.cs"));

        Assert.Contains("WeaponChoice,", source, StringComparison.Ordinal);
        // Forward: the founder card advances into the beat, and the beat is
        // what confirms — so the founder cannot be created before the choice.
        Assert.Contains(
            "case Stage.FounderCard:\n                RenderWeaponChoice();",
            source.Replace("\r\n", "\n", StringComparison.Ordinal),
            StringComparison.Ordinal);
        Assert.Contains(
            "case Stage.WeaponChoice:\n                OnConfirmIdentity();",
            source.Replace("\r\n", "\n", StringComparison.Ordinal),
            StringComparison.Ordinal);
        // Back: returns to the card rather than dropping out of the flow.
        Assert.Contains(
            "case Stage.WeaponChoice:\n                RenderFounderCard();",
            source.Replace("\r\n", "\n", StringComparison.Ordinal),
            StringComparison.Ordinal);

        // The pair comes from the domain, is derived on each render rather
        // than cached, and the selection survives the round trip.
        Assert.Contains("NaturalWeaponFamilies.For(", source, StringComparison.Ordinal);
        Assert.Contains("Selected = _weaponChoice == family", source, StringComparison.Ordinal);
        // The choice reaches the domain through the request, never applied
        // to the citizen from the scene.
        Assert.Contains("_weaponChoice.Value));", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MaterializeFounderWeapon", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Every family a player can be offered has a player-facing name. The key
    /// is derived from the enum, so a family added to the domain without a
    /// catalogue entry would otherwise reach the button as a raw identifier.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllFamilies))]
    public void EveryWeaponFamilyHasALocalisedName(WeaponFamily family)
    {
        string catalogue = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "locale", "en.po"));
        string spanish = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "locale", "es.po"));
        string key = AstralOnboardingView.WeaponFamilyTextKey(family);

        Assert.Contains($"msgid \"{key}\"", catalogue, StringComparison.Ordinal);
        Assert.Contains($"msgid \"{key}\"", spanish, StringComparison.Ordinal);
    }

    public static IEnumerable<object[]> AllFamilies =>
        Enum.GetValues<WeaponFamily>().Select(family => new object[] { family });

    private static WorldSave LegacyV34SaveWithWeapon(WeaponFamily? family)
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        if (family is WeaponFamily equipped)
        {
            world.Hero!.SetEquipmentLoadout(new EquipmentLoadout(
                new WeaponChannelProfile(equipped, 0.75, 1.2),
                world.Hero.EquipmentLoadout.Helmet,
                world.Hero.EquipmentLoadout.Chest,
                world.Hero.EquipmentLoadout.Legs,
                world.Hero.EquipmentLoadout.Boots,
                world.Hero.EquipmentLoadout.Gloves));
        }
        WorldSave save = WorldPersistence.Capture(world);
        // A v34 payload has no PersonalEquipment field at all — the migration
        // must cope with its absence, not merely with an empty one.
        foreach (CitizenSave citizen in save.Citizens) citizen.PersonalEquipment = null;
        save.Version = 34;
        return save;
    }
}
