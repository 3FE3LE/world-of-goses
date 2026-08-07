using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// The schema v32 rename of the third cube face from
/// <c>"Mastery"</c> to <c>"Domain"</c> had to be a hard disk migration:
/// without one, a v31 JSON loaded by the new code would deserialize the
/// legacy key as a discardable unknown member and the founder's cube would
/// arrive at the constructor as <c>0</c>, tripping
/// <see cref="FounderCubeProfile"/>'s pair-sum invariant and silently
/// removing the player from the world.
///
/// These tests pin the bridge: a save written by hand with the legacy key
/// migrates to v32 with the canonical field populated, the bridge cleared,
/// and the founder's cube unchanged.
/// </summary>
public sealed class MigrateV31ToV32Tests
{
    private const string LegacyJsonWithMastery = """
{
  "Version": 31,
  "EconomicBalanceVersion": 1,
  "LastSeenAtUnixMillis": 0,
  "CurrentTick": 240,
  "Buildings": [],
  "Citizens": [
    {
      "Id": 1,
      "Name": "Aster",
      "AppearanceSeed": 42,
      "Origin": "AstralFounder",
      "AppearanceVariant": "Standard",
      "Profile": {
        "Lineage": "Kovari",
        "Gender": "Masculine",
        "ElementalAffinity": "fire",
        "CombatStyle": "DefensiveSupport",
        "PoliticalOrientation": "Communitarian",
        "SpiritualPosture": "Contemplative",
        "Aptitudes": [ "Observation", "ManualPrecision", "SelfControl" ],
        "ProfessionalAffinities": [ "Extraction", "EngineeringManufacturing", "MedicineCare" ],
        "WeaponPreferences": [ "Polearm", "Shield" ],
        "PersonalityTraits": [ "Patient", "Protective", "Reflective" ],
        "CubeProfile": {
          "Body": 68,
          "Bond": 32,
          "Stability": 52,
          "Impulse": 48,
          "Mastery": 56,
          "Reach": 44
        },
        "NarrativeMemory": {
          "AnswerIds": [],
          "BelievedFinalWordId": null,
          "PreservedDetailId": null,
          "EchoIds": []
        }
      },
      "CurrentAssignment": null,
      "CommitmentKind": "None",
      "CommitmentEntityId": null,
      "WorkOrderKind": null,
      "WorkOrderEntityId": null,
      "VitalStatus": "Healthy",
      "TransitStartedAtTick": 0,
      "CurrentLocation": "AtHome",
      "ResumeWorkNotBeforeTick": 0,
      "IsReturningHome": false,
      "WoundSeverity": null,
      "WoundOriginatingEventId": null,
      "WoundRecoveryTicksRemaining": 0,
      "StaminaCurrent": 100,
      "StaminaMax": 100,
      "WellFedRemainingTicks": 0,
      "EquipmentLoadout": {
        "Weapon": null,
        "Helmet": { "Body": 0, "Bond": 0, "Stability": 0, "Impulse": 0, "Domain": 0, "Reach": 0 },
        "Chest":  { "Body": 0, "Bond": 0, "Stability": 0, "Impulse": 0, "Domain": 0, "Reach": 0 },
        "Legs":   { "Body": 0, "Bond": 0, "Stability": 0, "Impulse": 0, "Domain": 0, "Reach": 0 },
        "Boots":  { "Body": 0, "Bond": 0, "Stability": 0, "Impulse": 0, "Domain": 0, "Reach": 0 },
        "Gloves": { "Body": 0, "Bond": 0, "Stability": 0, "Impulse": 0, "Domain": 0, "Reach": 0 }
      },
      "CurrentHealthAndCondition": { "CurrentHealth": 100.0, "ConditionFactor": 1.0 },
      "Competencies": [],
      "WeaponCompetencies": [],
      "Roles": [ { "Id": "hero", "GrantedAtTick": 0 } ],
      "LastVisitedResourceBuildingId": null,
      "LastVisitedResourcePatchId": null,
      "LastVisitedResourceUnitId": null,
      "LastVisitedResourcePositionIndex": null
    }
  ],
  "ConstructionProjects": [],
  "CultivationSites": [],
  "Events": [],
  "ResourceReservations": [],
  "Parcels": [
    {
      "Id": 1,
      "LogicalColumn": 0,
      "LogicalRow": 0,
      "IsUnlocked": true,
      "TerritoryState": "Available"
    }
  ],
  "NaturalResourcePatches": [],
  "ParcelPlacements": [],
  "CorridorReservations": [],
  "CityInventory": {},
  "Tools": [],
  "Expeditions": [],
  "ResourceOpportunities": [],
  "PendingProspectSeed": null,
  "PendingProspectName": null,
  "FirstNight": {
    "Stage": "Concluded",
    "CurrentDialogueNodeId": null,
    "StartedAtTick": 0,
    "ConcludedAtTick": 240
  }
}
""";

    [Fact]
    public void MigrateV31ToV32_CarriesLegacyMasteryIntoCanonicalDomain()
    {
        WorldSave loaded = WorldPersistence.DeserializeFromJson(LegacyJsonWithMastery);

        // The legacy "Mastery" key arrives in the bridge field; the canonical
        // "Domain" field has no source on disk yet, so it stays at the int
        // default of 0. This is the silent failure the migration exists to fix.
        FounderCubeProfileSave? raw = loaded.Citizens[0].Profile?.CubeProfile;
        Assert.NotNull(raw);
        Assert.Equal(0, raw!.Domain);
#pragma warning disable CS0618
        Assert.Equal(56, raw.Mastery);
#pragma warning restore CS0618

        WorldSave migrated = WorldPersistence.MigrateV31ToV32(loaded);

        Assert.Equal(32, migrated.Version);
        FounderCubeProfileSave? cube = migrated.Citizens[0].Profile?.CubeProfile;
        Assert.NotNull(cube);
        Assert.Equal(56, cube!.Domain);
#pragma warning disable CS0618
        Assert.Null(cube.Mastery);
#pragma warning restore CS0618
    }

    [Fact]
    public void MigrateV31ToV32_PreservesTheFounderCubeThroughFullRoundTrip()
    {
        // The full round trip proves the migration path the live loader takes:
        // load a v31 disk file, walk MigrateToCurrent, then restore the world.
        // Without the bridge, the founder's cube would arrive at zero and the
        // pair invariant would throw during restore, leaving the player
        // without their city.
        WorldSave migrated = WorldPersistence.MigrateToCurrent(
            WorldPersistence.DeserializeFromJson(LegacyJsonWithMastery));

        Citizen restoredFounder = Assert.Single(
            CityWorld.FromSave(migrated).Citizens.Values,
            citizen => citizen.Origin == CitizenOrigin.AstralFounder);

        FounderCubeProfile cube = restoredFounder.Profile.CubeProfile;
        Assert.Equal(68, cube.Body);
        Assert.Equal(32, cube.Bond);
        Assert.Equal(52, cube.Stability);
        Assert.Equal(48, cube.Impulse);
        Assert.Equal(56, cube.Domain);
        Assert.Equal(44, cube.Reach);
    }

    [Fact]
    public void MigrateV31ToV32_NoOpForSavesWithoutLegacyMastery()
    {
        // A v31 save never written with the legacy key — migrated by a
        // hypothetical v32-aware writer that already uses "Domain" — must
        // still pass through the migration without losing data.
        WorldSave base_ = WorldPersistence.MigrateToCurrent(
            WorldPersistence.DeserializeFromJson(LegacyJsonWithMastery));
        base_.Version = 31;
#pragma warning disable CS0618 // The whole point of this test is to write null to the bridge.
        base_.Citizens[0].Profile!.CubeProfile!.Mastery = null;
#pragma warning restore CS0618

        WorldSave migrated = WorldPersistence.MigrateV31ToV32(base_);

        Assert.Equal(32, migrated.Version);
        Assert.Equal(56, migrated.Citizens[0].Profile!.CubeProfile!.Domain);
#pragma warning disable CS0618
        Assert.Null(migrated.Citizens[0].Profile!.CubeProfile!.Mastery);
#pragma warning restore CS0618
    }

    [Fact]
    public void MigrateToCurrent_BridgesLegacySavesEndToEnd()
    {
        // The exhaustive route: a save that comes off disk at version 31,
        // runs through MigrateToCurrent and lands at version 32 with the
        // cube intact. This is the test that catches a future regression
        // where someone removes the v31→v32 entry from the switch.
        WorldSave loaded = WorldPersistence.DeserializeFromJson(LegacyJsonWithMastery);
        Assert.Equal(31, loaded.Version);

        WorldSave migrated = WorldPersistence.MigrateToCurrent(loaded);

        Assert.Equal(32, migrated.Version);
        FounderCubeProfileSave cube = migrated.Citizens[0].Profile!.CubeProfile!;
        Assert.Equal(56, cube.Domain);
#pragma warning disable CS0618
        Assert.Null(cube.Mastery);
#pragma warning restore CS0618
    }
}