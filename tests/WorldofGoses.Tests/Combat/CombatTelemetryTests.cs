using System.Collections.Generic;
using System.IO;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using WorldofGoses.Presentation;
using Xunit;
using Xunit.Abstractions;

namespace WorldofGoses.Tests.Combat;

/// <summary>
/// Proves the mandatory telemetry is complete and comes from the domain. Also
/// writes the two required textual captures to the test output so a reviewer can
/// read a real technique breakdown and a real expedition result.
/// </summary>
public sealed class CombatTelemetryTests
{
    private readonly ITestOutputHelper _output;

    public CombatTelemetryTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void TechniqueTelemetry_NamesEveryMandatoryField()
    {
        List<Citizen> party = CombatExpeditionSliceTests.Party();
        var service = new CombatExpeditionService();
        ExpeditionRunResult result = service.Run(
            party, CombatExpeditionSliceTests.Plan(party, ExpeditionRoute.SafeRoute));

        TechniqueResolution resolution = result.CombatLogs
            .SelectMany(log => log)
            .Where(entry => entry.Resolution is not null)
            .Select(entry => entry.Resolution!)
            .First();
        string text = CombatTelemetryText.Describe(resolution);
        _output.WriteLine(text);

        foreach (string field in new[]
        {
            "PhysicalChannelPower",
            "ElementalChannelPower",
            "PhysicalCoefficient",
            "ElementalCoefficient",
            "PhysicalContribution",
            "ElementalContribution",
            "RawTechniqueResult",
            "PhysicalMitigation",
            "ElementalMitigation",
            "CriticalResult",
            "FinalResult",
            "AppliedStatuses",
        })
        {
            Assert.Contains(field, text);
        }
        // Channel power is a capacity, never labelled as damage.
        Assert.DoesNotContain("PhysicalDamage", text);
        Assert.DoesNotContain("ElementalDamage", text);
    }

    [Fact]
    public void ExpeditionTelemetry_NamesEveryMandatoryField()
    {
        List<Citizen> party = CombatExpeditionSliceTests.Party();
        var service = new CombatExpeditionService();
        ExpeditionRunResult result = service.Run(
            party, CombatExpeditionSliceTests.Plan(party, ExpeditionRoute.ShortRoute));
        service.ApplyResult(party, result);

        string text = CombatTelemetryText.Describe(result);
        _output.WriteLine(text);

        foreach (string field in new[]
        {
            "route",
            "consumedSupplies",
            "encounterOutcomes",
            "acquiredResources",
            "injuries",
            "weaponExperience",
            "survivalExperience",
            "fatigue",
            "discoveredRoute",
        })
        {
            Assert.Contains(field, text);
        }
        Assert.Equal(3, result.Members.Count);
    }

    [Fact]
    public void ConditionTelemetry_ExplainsItsCauses()
    {
        ConditionFactorBreakdown condition = CombatConditionFactor.Derive(
            currentHealth: 40,
            maxHealth: 100,
            fatigue: 25,
            injuries: new[] { InjuryKind.OpenWound });

        string text = CombatTelemetryText.Describe(condition);
        _output.WriteLine(text);

        Assert.Contains("health", text);
        Assert.Contains("fatigue", text);
        Assert.Contains("injury OpenWound", text);
        Assert.True(condition.Value < 1.0);
    }

    /// <summary>
    /// Writes the two required captures next to the test assembly so they can be
    /// pasted into a report without re-running the game.
    /// </summary>
    [Fact]
    public void WritesTheTextualCapturesForReview()
    {
        List<Citizen> party = CombatExpeditionSliceTests.Party();
        var service = new CombatExpeditionService();
        ExpeditionRunResult result = service.Run(
            party, CombatExpeditionSliceTests.Plan(party, ExpeditionRoute.ShortRoute, seed: 2026));
        service.ApplyResult(party, result);

        var text = new System.Text.StringBuilder();
        text.AppendLine("=== TECHNIQUE TELEMETRY (first resolution) ===");
        TechniqueResolution first = result.CombatLogs
            .SelectMany(log => log)
            .Where(entry => entry.Resolution is not null)
            .Select(entry => entry.Resolution!)
            .First();
        text.AppendLine(CombatTelemetryText.Describe(first));
        text.AppendLine("=== EXPEDITION RESULT ===");
        text.AppendLine(CombatTelemetryText.Describe(result));

        string path = Path.Combine(Path.GetTempPath(), "wog-combat-telemetry.txt");
        File.WriteAllText(path, text.ToString());
        _output.WriteLine(path);
        _output.WriteLine(text.ToString());

        Assert.True(File.Exists(path));
    }
}
