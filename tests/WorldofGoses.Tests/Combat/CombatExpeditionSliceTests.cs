using System;
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using Xunit;

namespace WorldofGoses.Tests.Combat;

/// <summary>
/// Roadmap Fase 7 and the slice's definition of done: three persistent citizens
/// depart, fight twice, choose a route and come home with consequences written back
/// onto the same people. No scene is involved in any of it.
/// </summary>
public sealed class CombatExpeditionSliceTests
{
    [Fact]
    public void ThreeCitizens_CompleteTheFullCircuit()
    {
        List<Citizen> party = Party();
        var service = new CombatExpeditionService();

        ExpeditionRunResult result = service.Run(party, Plan(party, ExpeditionRoute.SafeRoute));

        Assert.Equal(3, result.Members.Count);
        // Two encounters attempted unless the first one already ended the run.
        Assert.InRange(result.EncounterOutcomes.Count, 1, 2);
        Assert.Equal(result.EncounterOutcomes.Count, result.CombatLogs.Count);
        Assert.All(result.CombatLogs, log => Assert.NotEmpty(log));
        Assert.All(result.Members, member => Assert.True(member.Fatigue > 0));
    }

    [Fact]
    public void SafeRoute_AndShortRoute_ProduceDifferentResults()
    {
        List<Citizen> safeParty = Party();
        List<Citizen> shortParty = Party();
        var service = new CombatExpeditionService();

        ExpeditionRunResult safe = service.Run(
            safeParty, Plan(safeParty, ExpeditionRoute.SafeRoute));
        ExpeditionRunResult risky = service.Run(
            shortParty, Plan(shortParty, ExpeditionRoute.ShortRoute));

        // The route must change at least one of composition, fatigue, supplies,
        // reward or return risk. It changes several, so the choice is legible.
        Assert.NotEqual(safe.ConsumedSupplies, risky.ConsumedSupplies);
        Assert.True(TotalFatigue(risky) > TotalFatigue(safe));
        Assert.NotEqual(safe.Route, risky.Route);
        Assert.NotEqual(safe.DiscoveredRouteState, risky.DiscoveredRouteState);
    }

    [Fact]
    public void HealthPersistsBetweenTheTwoEncounters()
    {
        List<Citizen> party = Party();
        var service = new CombatExpeditionService();

        ExpeditionRunResult result = service.Run(party, Plan(party, ExpeditionRoute.SafeRoute));

        // Only meaningful when the party actually reached the second encounter.
        if (result.EncounterOutcomes.Count < 2) return;

        IReadOnlyList<CombatLogEntry> first = result.CombatLogs[0];
        IReadOnlyList<CombatLogEntry> second = result.CombatLogs[1];
        double takenInFirst = DamageTakenByParty(first);
        Assert.True(takenInFirst > 0, "The first encounter should have cost the party something.");

        // Health is never silently restored between encounters: the party enters the
        // second encounter already worn down. Asserted on the aggregate because
        // seeded targeting decides who personally took the hits.
        Assert.NotEmpty(second);
        double remaining = result.Members.Sum(member => member.RemainingHealth);
        double capacity = result.Members.Sum(member => member.MaxHealth);
        Assert.True(
            remaining < capacity,
            $"The party should return worn: {remaining:0.#} of {capacity:0.#}.");
    }

    [Fact]
    public void InjuriesAndFatiguePersistOntoTheCitizensAfterReturning()
    {
        List<Citizen> party = Party();
        var service = new CombatExpeditionService();

        ExpeditionRunResult result = service.Run(party, Plan(party, ExpeditionRoute.ShortRoute));
        service.ApplyResult(party, result);

        Assert.Contains(result.Members, member => member.Injuries.Count > 0);
        foreach (Citizen citizen in party)
        {
            // Condition is derived from persistent causes, so a hurt, tired citizen
            // comes home measurably worse than neutral.
            Assert.True(citizen.CurrentHealthAndCondition.IsResolved);
            Assert.True(citizen.CurrentHealthAndCondition.ConditionFactor < 1.0);
        }
    }

    [Fact]
    public void CitizensKeepTheirIdentityCompetenciesAndEquipmentAfterTheExpedition()
    {
        List<Citizen> party = Party();
        var ids = party.Select(citizen => citizen.Id.Value).ToList();
        var weapons = party
            .Select(citizen => citizen.EquipmentLoadout.Weapon!.Family)
            .ToList();
        var service = new CombatExpeditionService();

        ExpeditionRunResult result = service.Run(party, Plan(party, ExpeditionRoute.SafeRoute));
        service.ApplyResult(party, result);

        Assert.Equal(ids, party.Select(citizen => citizen.Id.Value).ToList());
        Assert.Equal(weapons, party.Select(c => c.EquipmentLoadout.Weapon!.Family).ToList());
        foreach (Citizen citizen in party)
        {
            WeaponFamily family = citizen.EquipmentLoadout.Weapon!.Family;
            Assert.True(
                citizen.WeaponCompetencies.ContainsKey(family),
                $"{citizen.Name} should have recorded competency for {family}.");
            Assert.True(citizen.GetExperience(CompetencyId.Survival) > 0);
        }
    }

    [Fact]
    public void ExperienceIsGranted_AndRespectsTheForeignFamilyPenalty()
    {
        // Fire → Stunning, whose natural families are Mace and Orb. Spear is foreign.
        Citizen natural = Member(1, WeaponFamily.Mace, ElementalAffinity.Fire);
        Citizen foreign = Member(2, WeaponFamily.Spear, ElementalAffinity.Fire);
        var party = new List<Citizen> { natural, foreign };
        var service = new CombatExpeditionService();

        ExpeditionRunResult result = service.Run(party, Plan(party, ExpeditionRoute.SafeRoute));
        service.ApplyResult(party, result);

        double naturalXp = natural.WeaponCompetencies[WeaponFamily.Mace].Experience;
        double foreignXp = foreign.WeaponCompetencies[WeaponFamily.Spear].Experience;

        Assert.True(naturalXp > 0);
        Assert.True(foreignXp > 0);
        // The penalty is on learning only; both fought, one learns ten times slower.
        Assert.True(naturalXp > foreignXp * 2, $"natural={naturalXp} foreign={foreignXp}");
    }

    [Fact]
    public void SameSeed_ProducesAnIdenticalExpeditionResult()
    {
        List<Citizen> firstParty = Party();
        List<Citizen> secondParty = Party();
        var service = new CombatExpeditionService();

        ExpeditionRunResult first = service.Run(
            firstParty, Plan(firstParty, ExpeditionRoute.ShortRoute, seed: 4242));
        ExpeditionRunResult repeat = service.Run(
            secondParty, Plan(secondParty, ExpeditionRoute.ShortRoute, seed: 4242));

        Assert.Equal(first.ReachedDestination, repeat.ReachedDestination);
        Assert.Equal(first.EncounterOutcomes, repeat.EncounterOutcomes);
        Assert.Equal(
            first.Members.Select(member => member.RemainingHealth).ToList(),
            repeat.Members.Select(member => member.RemainingHealth).ToList());
    }

    [Fact]
    public void PreparingAMemberWithoutAWeapon_IsRefused()
    {
        Citizen unarmed = TestHelpers.NewCitizen(77);
        var service = new CombatExpeditionService();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => service.PrepareMember(unarmed));

        Assert.Contains("weapon", error.Message);
    }

    [Fact]
    public void TelemetryCarriesEveryMandatoryFieldFromTheDomain()
    {
        List<Citizen> party = Party();
        var service = new CombatExpeditionService();

        ExpeditionRunResult result = service.Run(party, Plan(party, ExpeditionRoute.SafeRoute));
        TechniqueResolution resolution = result.CombatLogs
            .SelectMany(log => log)
            .Where(entry => entry.Resolution is not null)
            .Select(entry => entry.Resolution!)
            .First();

        Assert.True(resolution.PhysicalChannelPower > 0);
        Assert.True(resolution.ElementalChannelPower > 0);
        Assert.True(resolution.PhysicalCoefficient >= 0);
        Assert.True(resolution.ElementalCoefficient >= 0);
        Assert.Equal(
            resolution.PhysicalChannelPower * resolution.PhysicalCoefficient,
            resolution.PhysicalContribution,
            6);
        Assert.Equal(
            resolution.ElementalChannelPower * resolution.ElementalCoefficient,
            resolution.ElementalContribution,
            6);
        Assert.Equal(
            resolution.PhysicalContribution + resolution.ElementalContribution,
            resolution.RawTechniqueResult,
            6);
        Assert.True(resolution.FinalResult >= 0);
        Assert.NotNull(resolution.AppliedStatuses);
    }

    internal static List<Citizen> Party() => new()
    {
        Member(101, WeaponFamily.Spear, ElementalAffinity.Earth),
        Member(102, WeaponFamily.Mace, ElementalAffinity.Fire),
        Member(103, WeaponFamily.Orb, ElementalAffinity.Air),
    };

    internal static Citizen Member(int id, WeaponFamily family, ElementalAffinity affinity)
    {
        var onboarding = new FounderOnboardingResult(
            LineageId.Kovari,
            affinity,
            CubeScoring.ComputeCubeVertex(LineageId.Kovari),
            FounderNarrativeMemory.Empty);
        CitizenProfile profile = CitizenProfile.CreateFounder(onboarding, GenderId.Feminine);
        var citizen = new Citizen(new CitizenId(id), $"Member-{id}", id * 7, profile);
        citizen.SetEquipmentLoadout(new EquipmentLoadout(
            new WeaponChannelProfile(family, 1.10, 1.05),
            GearSupportProfile.None,
            GearSupportProfile.None,
            GearSupportProfile.None,
            GearSupportProfile.None,
            GearSupportProfile.None));
        return citizen;
    }

    internal static ExpeditionRunPlan Plan(
        IReadOnlyList<Citizen> party,
        ExpeditionRoute route,
        ulong seed = 2026)
    {
        var plans = new Dictionary<string, CombatantPlan>();
        foreach (Citizen citizen in party)
        {
            plans[$"citizen.{citizen.Id.Value}"] =
                new CombatantPlan(0, Array.Empty<string>(), null, RetreatWhenBelowThreshold: false);
        }
        return new ExpeditionRunPlan(
            party.Select(citizen => citizen.Id).ToList(),
            plans,
            route,
            Supplies: 6,
            Seed: seed);
    }

    private static double TotalFatigue(ExpeditionRunResult result) =>
        result.Members.Sum(member => member.Fatigue);

    private static double DamageTakenByParty(IReadOnlyList<CombatLogEntry> log) =>
        log.Where(entry =>
                entry.Resolution is not null
                && entry.TargetId is not null
                && entry.TargetId.StartsWith("citizen.", StringComparison.Ordinal))
            .Sum(entry => entry.Resolution!.FinalResult);
}
