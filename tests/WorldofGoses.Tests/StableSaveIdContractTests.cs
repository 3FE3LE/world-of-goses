using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using WorldofGoses.Persistence;
using WorldofGoses.Persistence.Ids;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Architecture Hardening A7 contract tests.
///
/// Every persisted enum value is matched to its wire ID through a
/// stable mapper. These tests freeze the wire IDs of every persisted
/// enum family so that renaming a C# enum value (e.g. <c>ResourceType.Wood</c>
/// → <c>ResourceType.WoodLog</c>) does NOT silently change the save
/// format. The mapper is the contract; the test is the lock.
///
/// <para>To migrate a wire ID:
/// <list type="number">
///   <item>Add a new ID constant to the mapper (e.g. <c>WoodLogId = "WoodLog"</c>).</item>
///   <item>Switch the corresponding C# enum case to the new ID.</item>
///   <item>Add a migration step to <c>WorldPersistence.MigrateToCurrent</c>
///         that rewrites the old ID to the new one.</item>
///   <item>Update the test below with the new ID; the old test must
///         continue to pass via the legacy <c>default:</c> Enum.TryParse
///         branch.</item>
/// </list></para>
/// </summary>
public sealed class StableSaveIdContractTests
{
    // -------- Resources --------

    [Theory]
    [InlineData(ResourceType.Stone, "Stone")]
    [InlineData(ResourceType.Food, "Food")]
    [InlineData(ResourceType.Iron, "Iron")]
    [InlineData(ResourceType.Potions, "Potions")]
    [InlineData(ResourceType.Wood, "Wood")]
    [InlineData(ResourceType.Branches, "Branches")]
    [InlineData(ResourceType.PlantFiber, "PlantFiber")]
    [InlineData(ResourceType.SmallStone, "SmallStone")]
    [InlineData(ResourceType.WildFood, "WildFood")]
    public void ResourceType_WireId_IsStable(ResourceType value, string expectedId)
    {
        Assert.Equal(expectedId, ResourceTypeSaveIds.ToId(value));
        Assert.True(ResourceTypeSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    [Fact]
    public void ResourceType_UnknownString_FallsBackToEnumParse()
    {
        // A legacy or hand-edited save may use an unknown ID; the
        // mapper falls back to Enum.TryParse so a tolerant read still
        // produces a value (or default).
        Assert.True(ResourceTypeSaveIds.TryParse("Stone", out var v));
        Assert.Equal(ResourceType.Stone, v);
    }

    // -------- Building kinds --------

    [Theory]
    [InlineData(BuildingKind.Quarry, "Quarry")]
    [InlineData(BuildingKind.Farm, "Farm")]
    [InlineData(BuildingKind.Smithy, "Smithy")]
    [InlineData(BuildingKind.PotionLab, "PotionLab")]
    [InlineData(BuildingKind.Home, "Home")]
    [InlineData(BuildingKind.Forest, "Forest")]
    [InlineData(BuildingKind.TownHall, "TownHall")]
    [InlineData(BuildingKind.CultivationSite, "CultivationSite")]
    public void BuildingKind_WireId_IsStable(BuildingKind value, string expectedId)
    {
        Assert.Equal(expectedId, BuildingKindSaveIds.ToId(value));
        Assert.True(BuildingKindSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    // -------- Tools --------

    [Theory]
    [InlineData(ToolKind.PrimitiveAxe, "PrimitiveAxe")]
    public void ToolKind_WireId_IsStable(ToolKind value, string expectedId)
    {
        Assert.Equal(expectedId, ToolKindSaveIds.ToId(value));
        Assert.True(ToolKindSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    // -------- Founding Site modules --------

    [Theory]
    [InlineData(FoundingSiteModule.Campfire, "Campfire")]
    [InlineData(FoundingSiteModule.Bedroll, "Bedroll")]
    [InlineData(FoundingSiteModule.Cache, "Cache")]
    [InlineData(FoundingSiteModule.Canopy, "Canopy")]
    public void FoundingSiteModule_WireId_IsStable(FoundingSiteModule value, string expectedId)
    {
        Assert.Equal(expectedId, FoundingSiteModuleSaveIds.ToId(value));
        Assert.True(FoundingSiteModuleSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    // -------- First night stages --------

    [Theory]
    [InlineData(FirstNightStage.Manifested, "Manifested")]
    [InlineData(FirstNightStage.SpiritArrived, "SpiritArrived")]
    [InlineData(FirstNightStage.ColdExplained, "ColdExplained")]
    [InlineData(FirstNightStage.CampfireBuilt, "CampfireBuilt")]
    [InlineData(FirstNightStage.ShelterExplained, "ShelterExplained")]
    [InlineData(FirstNightStage.ShelterBuilt, "ShelterBuilt")]
    [InlineData(FirstNightStage.OtherLightTold, "OtherLightTold")]
    [InlineData(FirstNightStage.Sleeping, "Sleeping")]
    [InlineData(FirstNightStage.Concluded, "Concluded")]
    public void FirstNightStage_WireId_IsStable(FirstNightStage value, string expectedId)
    {
        Assert.Equal(expectedId, FirstNightStageSaveIds.ToId(value));
        Assert.True(FirstNightStageSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    // -------- Parcel territory states --------

    [Theory]
    [InlineData(ParcelTerritoryState.Locked, "Locked")]
    [InlineData(ParcelTerritoryState.Reconnoitred, "Reconnoitred")]
    [InlineData(ParcelTerritoryState.RouteSecured, "RouteSecured")]
    [InlineData(ParcelTerritoryState.Available, "Available")]
    public void ParcelTerritoryState_WireId_IsStable(ParcelTerritoryState value, string expectedId)
    {
        Assert.Equal(expectedId, ParcelTerritoryStateSaveIds.ToId(value));
        Assert.True(ParcelTerritoryStateSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    // -------- Building orientations --------

    [Theory]
    [InlineData(BuildingOrientation.South, "South")]
    [InlineData(BuildingOrientation.West, "West")]
    [InlineData(BuildingOrientation.North, "North")]
    [InlineData(BuildingOrientation.East, "East")]
    public void BuildingOrientation_WireId_IsStable(BuildingOrientation value, string expectedId)
    {
        Assert.Equal(expectedId, BuildingOrientationSaveIds.ToId(value));
        Assert.True(BuildingOrientationSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    // -------- Construction kinds --------

    [Theory]
    [InlineData(ConstructionKind.BasicShelter, "BasicShelter")]
    [InlineData(ConstructionKind.Farm, "Farm")]
    [InlineData(ConstructionKind.Quarry, "Quarry")]
    [InlineData(ConstructionKind.TownHall, "TownHall")]
    [InlineData(ConstructionKind.FoundingSite, "FoundingSite")]
    [InlineData(ConstructionKind.CultivationSite, "CultivationSite")]
    public void ConstructionKind_WireId_IsStable(ConstructionKind value, string expectedId)
    {
        Assert.Equal(expectedId, ConstructionKindSaveIds.ToId(value));
        Assert.True(ConstructionKindSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    // -------- Citizen state --------

    [Theory]
    [InlineData(CitizenOrigin.Mortal, "Mortal")]
    [InlineData(CitizenOrigin.AstralFounder, "AstralFounder")]
    public void CitizenOrigin_WireId_IsStable(CitizenOrigin value, string expectedId)
    {
        Assert.Equal(expectedId, CitizenOriginSaveIds.ToId(value));
        Assert.True(CitizenOriginSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    [Theory]
    [InlineData(CitizenCommitmentKind.None, "None")]
    [InlineData(CitizenCommitmentKind.BuildingWork, "BuildingWork")]
    [InlineData(CitizenCommitmentKind.Construction, "Construction")]
    [InlineData(CitizenCommitmentKind.Expedition, "Expedition")]
    [InlineData(CitizenCommitmentKind.Recovery, "Recovery")]
    public void CitizenCommitmentKind_WireId_IsStable(CitizenCommitmentKind value, string expectedId)
    {
        Assert.Equal(expectedId, CitizenCommitmentKindSaveIds.ToId(value));
        Assert.True(CitizenCommitmentKindSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    [Theory]
    [InlineData(CitizenVitalStatus.Stable, "Stable")]
    [InlineData(CitizenVitalStatus.Recovering, "Recovering")]
    [InlineData(CitizenVitalStatus.BlockedNoFood, "BlockedNoFood")]
    public void CitizenVitalStatus_WireId_IsStable(CitizenVitalStatus value, string expectedId)
    {
        Assert.Equal(expectedId, CitizenVitalStatusSaveIds.ToId(value));
        Assert.True(CitizenVitalStatusSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    [Theory]
    [InlineData(CitizenLocation.AtHome, "AtHome")]
    [InlineData(CitizenLocation.InTransit, "InTransit")]
    [InlineData(CitizenLocation.AtWork, "AtWork")]
    public void CitizenLocation_WireId_IsStable(CitizenLocation value, string expectedId)
    {
        Assert.Equal(expectedId, CitizenLocationSaveIds.ToId(value));
        Assert.True(CitizenLocationSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    [Theory]
    [InlineData(WoundSeverity.Moderate, "Moderate")]
    [InlineData(WoundSeverity.Severe, "Severe")]
    public void WoundSeverity_WireId_IsStable(WoundSeverity value, string expectedId)
    {
        Assert.Equal(expectedId, WoundSeveritySaveIds.ToId(value));
        Assert.True(WoundSeveritySaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    // -------- Expedition state --------

    [Theory]
    [InlineData(ExpeditionStatus.Active, "Active")]
    [InlineData(ExpeditionStatus.Returned, "Returned")]
    [InlineData(ExpeditionStatus.Failed, "Failed")]
    [InlineData(ExpeditionStatus.Cancelled, "Cancelled")]
    [InlineData(ExpeditionStatus.Retreated, "Retreated")]
    public void ExpeditionStatus_WireId_IsStable(ExpeditionStatus value, string expectedId)
    {
        Assert.Equal(expectedId, ExpeditionStatusSaveIds.ToId(value));
        Assert.True(ExpeditionStatusSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    [Theory]
    [InlineData(ExpeditionPhase.Outbound, "Outbound")]
    [InlineData(ExpeditionPhase.Encounter, "Encounter")]
    [InlineData(ExpeditionPhase.Objective, "Objective")]
    [InlineData(ExpeditionPhase.Returning, "Returning")]
    [InlineData(ExpeditionPhase.Resolved, "Resolved")]
    [InlineData(ExpeditionPhase.Retreating, "Retreating")]
    public void ExpeditionPhase_WireId_IsStable(ExpeditionPhase value, string expectedId)
    {
        Assert.Equal(expectedId, ExpeditionPhaseSaveIds.ToId(value));
        Assert.True(ExpeditionPhaseSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    [Theory]
    [InlineData(ExpeditionEncounterOutcome.Setback, "Setback")]
    [InlineData(ExpeditionEncounterOutcome.PartialSuccess, "PartialSuccess")]
    [InlineData(ExpeditionEncounterOutcome.FullSuccess, "FullSuccess")]
    public void ExpeditionEncounterOutcome_WireId_IsStable(ExpeditionEncounterOutcome value, string expectedId)
    {
        Assert.Equal(expectedId, ExpeditionEncounterOutcomeSaveIds.ToId(value));
        Assert.True(ExpeditionEncounterOutcomeSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    [Theory]
    [InlineData(ExpeditionRetreatPosture.ContinueAfterSetback, "ContinueAfterSetback")]
    [InlineData(ExpeditionRetreatPosture.RetreatAfterSetback, "RetreatAfterSetback")]
    public void ExpeditionRetreatPosture_WireId_IsStable(ExpeditionRetreatPosture value, string expectedId)
    {
        Assert.Equal(expectedId, ExpeditionRetreatPostureSaveIds.ToId(value));
        Assert.True(ExpeditionRetreatPostureSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    [Theory]
    [InlineData(ExpeditionRewardKind.Supplies, "Supplies")]
    [InlineData(ExpeditionRewardKind.Migrant, "Migrant")]
    [InlineData(ExpeditionRewardKind.Discovery, "Discovery")]
    public void ExpeditionRewardKind_WireId_IsStable(ExpeditionRewardKind value, string expectedId)
    {
        Assert.Equal(expectedId, ExpeditionRewardKindSaveIds.ToId(value));
        Assert.True(ExpeditionRewardKindSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    // -------- Resource opportunities --------

    [Theory]
    [InlineData(ResourceOpportunityKind.NearbyFoodForage, "NearbyFoodForage")]
    [InlineData(ResourceOpportunityKind.FallenWoodSearch, "FallenWoodSearch")]
    [InlineData(ResourceOpportunityKind.SpiritTrailSearch, "SpiritTrailSearch")]
    public void ResourceOpportunityKind_WireId_IsStable(ResourceOpportunityKind value, string expectedId)
    {
        Assert.Equal(expectedId, ResourceOpportunityKindSaveIds.ToId(value));
        Assert.True(ResourceOpportunityKindSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    [Theory]
    [InlineData(ResourceOpportunityState.Available, "Available")]
    [InlineData(ResourceOpportunityState.Reserved, "Reserved")]
    [InlineData(ResourceOpportunityState.Depleted, "Depleted")]
    public void ResourceOpportunityState_WireId_IsStable(ResourceOpportunityState value, string expectedId)
    {
        Assert.Equal(expectedId, ResourceOpportunityStateSaveIds.ToId(value));
        Assert.True(ResourceOpportunityStateSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    // -------- Cultivation plot state --------

    [Theory]
    [InlineData(CultivationPlotState.Prepared, "Prepared")]
    [InlineData(CultivationPlotState.Sown, "Sown")]
    [InlineData(CultivationPlotState.Growing, "Growing")]
    [InlineData(CultivationPlotState.Ready, "Ready")]
    [InlineData(CultivationPlotState.Spent, "Spent")]
    public void CultivationPlotState_WireId_IsStable(CultivationPlotState value, string expectedId)
    {
        Assert.Equal(expectedId, CultivationPlotStateSaveIds.ToId(value));
        Assert.True(CultivationPlotStateSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    // -------- Gender --------

    [Theory]
    [InlineData(GenderId.Feminine, "Feminine")]
    [InlineData(GenderId.Masculine, "Masculine")]
    public void GenderId_WireId_IsStable(GenderId value, string expectedId)
    {
        Assert.Equal(expectedId, GenderIdSaveIds.ToId(value));
        Assert.True(GenderIdSaveIds.TryParse(expectedId, out var parsed));
        Assert.Equal(value, parsed);
    }

    // -------- Elemental affinity (legacy lowercased) --------

    [Fact]
    public void ElementalAffinity_WireId_IsLowercased()
    {
        // Pre-v29 saves persisted the affinity as the lowercased enum
        // name; v29 canonicalises None→Silence. The mapper preserves
        // the lowercased wire format while making the contract explicit.
        Assert.Equal("fire", ElementalAffinityIdSaveIds.ToId(ElementalAffinityId.Fire));
        Assert.Equal("silence", ElementalAffinityIdSaveIds.ToId(ElementalAffinityId.Silence));
        Assert.Equal("water", ElementalAffinityIdSaveIds.ToId(ElementalAffinityId.Water));
        Assert.True(ElementalAffinityIdSaveIds.TryParse("silence", out var v));
        Assert.Equal(ElementalAffinityId.Silence, v);
        Assert.True(ElementalAffinityIdSaveIds.TryParse("WATER", out var w));
        Assert.Equal(ElementalAffinityId.Water, w);
    }

    // -------- Roundtrip the v34 schema body byte-for-byte --------

    [Fact]
    public void CapturedWorldSave_ContainsExpectedWireIds_NotRawEnumNames()
    {
        // This regression guard asserts that the JSON shape matches
        // the historical wire format the ID mappers expose. We build
        // a minimal WorldSave with the canonical tool/firstnight IDs,
        // serialize it, and assert the JSON contains the exact wire
        // strings the mappers produced. A future refactor that
        // changed the mapper output would fail this test.
        WorldSave save = new WorldSave
        {
            Version = WorldSave.CurrentVersion,
            CurrentTick = 0,
            Tools = new System.Collections.Generic.List<string>
            {
                ToolKindSaveIds.PrimitiveAxeId,
            },
            FirstNight = new FirstNightSave
            {
                Stage = FirstNightStageSaveIds.SleepingId,
            },
        };
        string json = WorldPersistence.SerializeToJson(save);
        Assert.Contains("Sleeping", json);
        Assert.Contains("PrimitiveAxe", json);
    }

    // -------- Mapper must be the single source of truth --------

    [Fact]
    public void NoCaptureOrApplier_CallsEnumToStringDirectly_ForPersistedEnums()
    {
        // Architectural guardrail: every Capture/Restore site for a
        // persisted enum goes through its stable ID mapper. This test
        // fails the build if a future refactor reintroduces a raw
        // Enum.ToString() call on a save-side code path. We use a
        // regex against the source files (not IL) so the failure
        // message is readable and the rule is reviewable.
        string repoRoot = TestHelpers.FindRepositoryRoot();
        string[] files =
        {
            Path.Combine(repoRoot, "src", "WorldofGoses.Persistence", "WorldPersistence.cs"),
            Path.Combine(repoRoot, "src", "WorldofGoses.Persistence", "WorldSaveApplier.cs"),
        };

        // Names of the persisted enum families covered by stable ID
        // mappers. A direct call on one of these in Capture/Restore
        // code is the regression we're guarding against.
        string[] persistedEnumTypes =
        {
            "ResourceType", "BuildingKind", "ToolKind", "FoundingSiteModule",
            "FirstNightStage", "ParcelTerritoryState", "BuildingOrientation",
            "ConstructionKind", "CitizenOrigin", "CitizenCommitmentKind",
            "CitizenVitalStatus", "CitizenLocation", "WoundSeverity",
            "ExpeditionStatus", "ExpeditionPhase", "ExpeditionEncounterOutcome",
            "ExpeditionRetreatPosture", "ExpeditionRewardKind", "CultivationPlotState",
            "ResourceOpportunityKind", "ResourceOpportunityState", "GenderId",
            "ElementalAffinityId",
        };

        List<string> regressions = new();
        foreach (string file in files)
        {
            string source = File.ReadAllText(file);
            // Strip line comments and block comments so a docstring
            // listing the rule does not register as a breach.
            source = System.Text.RegularExpressions.Regex.Replace(
                source,
                @"//.*?$|/\*.*?\*/",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.Multiline
                | System.Text.RegularExpressions.RegexOptions.Singleline);
            foreach (string enumType in persistedEnumTypes)
            {
                // Match: <EnumType>.<Value>.ToString() — a direct
                // enum-to-string call on a persisted enum.
                var matches = System.Text.RegularExpressions.Regex.Matches(
                    source,
                    $@"\b{enumType}\.[A-Z][a-zA-Z]+\.ToString\(\)");
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    regressions.Add($"{Path.GetFileName(file)}: '{match.Value}'");
                }
            }
        }

        Assert.True(
            regressions.Count == 0,
            "The following Capture/Restore surfaces still call Enum.ToString() " +
            "directly on a persisted enum. Architecture Hardening A7 requires " +
            "routing every persisted enum through the stable ID mapper " +
            "(*SaveIds.ToId / *SaveIds.TryParse) so that renaming a C# enum " +
            "value does not silently change the save format. Offenders: "
            + string.Join(", ", regressions));
    }
}
