using System;
using System.Collections.Generic;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class DerivedStatisticsTests
{
    private const double Tolerance = 0.0001;
    private readonly StatisticsBalanceConfig _balance = StatisticsBalanceConfig.Default;

    public static TheoryData<string, double, double> OffensiveFixtures => new()
    {
        { "Aren", 84.00, 30.75 },
        { "Seyra", 34.85, 80.50 },
        { "Mira", 51.30, 52.00 },
        { "Tovan", 39.00, 62.40 },
        { "Neris", 54.60, 51.30 },
        { "Vael", 58.30, 53.00 },
    };

    [Fact]
    public void CubeProfile_RejectsInvalidComplementaryPairs()
    {
        Assert.Throws<ArgumentException>(() => new FounderCubeProfile(60, 41, 50, 50, 50, 50));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FounderCubeProfile(-1, 101, 50, 50, 50, 50));
    }

    [Fact]
    public void EquipmentLoadout_SumsExactlyFiveArmorPieces()
    {
        var loadout = new EquipmentLoadout(
            null,
            new GearSupportProfile(1, 2, 0, 0, 0, 0),
            new GearSupportProfile(2, 0, 1, 0, 0, 0),
            new GearSupportProfile(3, 0, 0, 1, 0, 0),
            new GearSupportProfile(1, 0, 0, 0, 1, 0),
            new GearSupportProfile(2, 0, 0, 0, 0, 1));

        Assert.Equal(9, loadout.TotalGearSupport.Body);
        Assert.Equal(2, loadout.TotalGearSupport.Bond);
        Assert.Equal(1, loadout.TotalGearSupport.Stability);
        Assert.Equal(1, loadout.TotalGearSupport.Impulse);
        Assert.Equal(1, loadout.TotalGearSupport.Domain);
        Assert.Equal(1, loadout.TotalGearSupport.Reach);
    }

    [Theory]
    [InlineData(0, 1.00)]
    [InlineData(10, 1.25)]
    [InlineData(20, 1.50)]
    public void SkillFactor_UsesCanonicalCurve(int level, double expected) =>
        Assert.Equal(expected, _balance.SkillFactor(level), precision: 10);

    [Theory]
    [MemberData(nameof(OffensiveFixtures))]
    public void OffensivePower_MatchesSixReferenceCitizens(
        string name,
        double expectedPhysical,
        double expectedElemental)
    {
        ReferenceCitizen fixture = Reference(name);
        OffensiveStatistics stats = Calculator().Offense.Calculate(
            fixture.Cube,
            fixture.Loadout,
            NeutralContext());

        AssertClose(expectedPhysical, stats.PhysicalChannelPower.Value);
        AssertClose(expectedElemental, stats.ElementalChannelPower.Value);
        Assert.Equal(fixture.Loadout.Weapon!.PhysicalTransfer, stats.PhysicalChannelPower.Breakdown.WeaponCoefficient);
        Assert.NotEmpty(stats.PhysicalChannelPower.Breakdown.FaceCalculations);
    }

    [Fact]
    public void DefenseAndRecovery_MatchArenReference()
    {
        ReferenceCitizen aren = Reference("Aren");
        DefensiveStatistics defense = Calculator().Defense.Calculate(aren.Cube, aren.Loadout, NeutralContext());
        RecoveryStatistics recovery = Calculator().Recovery.Calculate(aren.Cube, aren.Loadout, NeutralContext());

        AssertClose(259.0, defense.MaxHealth.Value);
        AssertClose(61.20, defense.PhysicalDefenseScore.Value);
        AssertClose(0.504950495, defense.PhysicalMitigation.Value);
        AssertClose(48.15, defense.ElementalDefenseScore.Value);
        AssertClose(0.44521498, defense.ElementalMitigation.Value);
        AssertClose(0.07012987, defense.GeneralDamageReduction.Value);
        AssertClose(7.37780455, recovery.HealthRegenerationPerMinute.Value);
        AssertClose(123.7434209, recovery.HealingAppliedPercent.Value);
    }

    [Fact]
    public void Defense_MatchesMiraReference()
    {
        ReferenceCitizen mira = Reference("Mira");
        DefensiveStatistics defense = Calculator().Defense.Calculate(mira.Cube, mira.Loadout, NeutralContext());

        AssertClose(251.0, defense.MaxHealth.Value);
        AssertClose(0.51140065, defense.PhysicalMitigation.Value);
        AssertClose(0.50779327, defense.ElementalMitigation.Value);
    }

    [Fact]
    public void DefenseAndRecovery_MatchAllSixReferenceTables()
    {
        var expected = new Dictionary<string, double[]>
        {
            ["Aren"] = new[] { 259.0, 61.20, 0.5050, 48.15, 0.4452, 0.0701, 7.38, 123.74 },
            ["Seyra"] = new[] { 215.5, 48.15, 0.4452, 61.20, 0.5050, 0.0701, 5.65, 130.74 },
            ["Mira"] = new[] { 251.0, 62.80, 0.5114, 61.90, 0.5078, 0.0824, 7.38, 126.00 },
            ["Tovan"] = new[] { 219.0, 45.95, 0.4337, 45.95, 0.4337, 0.0582, 5.54, 126.00 },
            ["Neris"] = new[] { 230.0, 52.00, 0.4643, 52.90, 0.4686, 0.0684, 6.24, 130.74 },
            ["Vael"] = new[] { 231.5, 52.45, 0.4664, 52.45, 0.4664, 0.0684, 6.30, 123.59 },
        };

        foreach ((string name, double[] values) in expected)
        {
            ReferenceCitizen fixture = Reference(name);
            DefensiveStatistics defense = Calculator().Defense.Calculate(fixture.Cube, fixture.Loadout, NeutralContext());
            RecoveryStatistics recovery = Calculator().Recovery.Calculate(fixture.Cube, fixture.Loadout, NeutralContext());
            Assert.Equal(values[0], defense.MaxHealth.Value, 2);
            Assert.Equal(values[1], defense.PhysicalDefenseScore.Value, 2);
            Assert.Equal(values[2], defense.PhysicalMitigation.Value, 4);
            Assert.Equal(values[3], defense.ElementalDefenseScore.Value, 2);
            Assert.Equal(values[4], defense.ElementalMitigation.Value, 4);
            Assert.Equal(values[5], defense.GeneralDamageReduction.Value, 4);
            Assert.Equal(values[6], recovery.HealthRegenerationPerMinute.Value, 2);
            Assert.Equal(values[7], recovery.HealingAppliedPercent.Value, 2);
        }
    }

    [Fact]
    public void Tempo_MatchesTovanNerisAndVaelReferences()
    {
        TempoStatistics tovan = Tempo("Tovan");
        TempoStatistics neris = Tempo("Neris");
        TempoStatistics vael = Tempo("Vael");

        AssertClose(1.24444444, tovan.AttackSpeed.Value);
        AssertClose(0.2100, tovan.CooldownReduction.Value);
        AssertClose(0.27222222, neris.CriticalChance.Value);
        AssertClose(1.17037037, vael.MovementSpeed.Value);
        AssertClose(0.16120370, vael.PhysicalEvasion.Value);
        AssertClose(0.16120370, vael.ElementalEvasion.Value);
    }

    [Fact]
    public void Tempo_MatchesAllSixReferenceTables()
    {
        var expected = new Dictionary<string, double[]>
        {
            ["Aren"] = new[] { 0.9828, 0.9112, 0.1360, 0.1628, 0.0880, 0.0526, 0.9409 },
            ["Seyra"] = new[] { 0.9828, 1.1150, 0.1313, 0.1556, 0.0914, 0.1575, 0.9524 },
            ["Mira"] = new[] { 0.8531, 0.9112, 0.0741, 0.1414, 0.0556, 0.0914, 0.9524 },
            ["Tovan"] = new[] { 1.2444, 1.1150, 0.2100, 0.1414, 0.1650, 0.0985, 0.9760 },
            ["Neris"] = new[] { 0.9690, 0.9898, 0.2050, 0.2722, 0.0556, 0.0648, 0.8520 },
            ["Vael"] = new[] { 0.9969, 0.9969, 0.0822, 0.0812, 0.1612, 0.1612, 1.1704 },
        };

        foreach ((string name, double[] values) in expected)
        {
            TempoStatistics stats = Tempo(name);
            Assert.Equal(values[0], stats.AttackSpeed.Value, 4);
            Assert.Equal(values[1], stats.CastSpeed.Value, 4);
            Assert.Equal(values[2], stats.CooldownReduction.Value, 4);
            Assert.Equal(values[3], stats.CriticalChance.Value, 4);
            Assert.Equal(values[4], stats.PhysicalEvasion.Value, 4);
            Assert.Equal(values[5], stats.ElementalEvasion.Value, 4);
            Assert.Equal(values[6], stats.MovementSpeed.Value, 4);
        }
    }

    [Fact]
    public void DamageTaken_ComposesReductionsMultiplicatively()
    {
        DefensiveStatistics defense = Defense("Aren");
        CalculatedStatistic taken = Calculator().Defense.CalculateDamageTaken(
            100,
            defense.GeneralDamageReduction,
            defense.PhysicalMitigation);

        double expected = 100
            * (1 - defense.GeneralDamageReduction.Value)
            * (1 - defense.PhysicalMitigation.Value);
        AssertClose(expected, taken.Value);
    }

    [Fact]
    public void Smoothstep_ReachesMinimumCenterAndMaximum()
    {
        TempoStatistics minimum = TempoForImpulse(0);
        TempoStatistics center = TempoForImpulse(60);
        TempoStatistics maximum = TempoForImpulse(100);

        AssertClose(_balance.AttackSpeedMinimum, minimum.AttackSpeed.Value);
        AssertClose((_balance.AttackSpeedMinimum + _balance.AttackSpeedMaximum) / 2, center.AttackSpeed.Value);
        AssertClose(_balance.AttackSpeedMaximum, maximum.AttackSpeed.Value);
    }

    [Fact]
    public void Smoothstep_AdversarialInjectedCurveStillRespectsStatCaps()
    {
        StatisticsBalanceConfig balance = _balance with
        {
            SmoothstepQuadraticCoefficient = 100,
            SmoothstepCubicCoefficient = 2,
        };
        var calculator = new TempoStatisticsCalculator(balance);
        var cube = new FounderCubeProfile(50, 50, 0, 100, 50, 50);

        TempoStatistics result = calculator.Calculate(
            cube,
            Loadout(WeaponFamily.Orb, 1, 1),
            new StatCalculationContext(0, 1, 1, balance));

        Assert.InRange(result.AttackSpeed.Value, balance.AttackSpeedMinimum, balance.AttackSpeedMaximum);
        Assert.InRange(result.CastSpeed.Value, balance.CastSpeedMinimum, balance.CastSpeedMaximum);
        Assert.InRange(result.CooldownReduction.Value, balance.CooldownReductionMinimum, balance.CooldownReductionMaximum);
        Assert.InRange(result.CriticalChance.Value, balance.CriticalChanceMinimum, balance.CriticalChanceMaximum);
        Assert.InRange(result.PhysicalEvasion.Value, balance.PhysicalEvasionMinimum, balance.PhysicalEvasionMaximum);
        Assert.InRange(result.ElementalEvasion.Value, balance.ElementalEvasionMinimum, balance.ElementalEvasionMaximum);
        Assert.InRange(result.MovementSpeed.Value, balance.MovementSpeedMinimum, balance.MovementSpeedMaximum);
    }

    [Fact]
    public void TempoStats_RespectEveryConfiguredCap()
    {
        var cube = new FounderCubeProfile(0, 100, 0, 100, 0, 100);
        var gear = new GearSupportProfile(0, 12, 0, 12, 12, 12);
        var loadout = Loadout(WeaponFamily.Orb, 1.20, 1.20, gear);
        var context = new StatCalculationContext(20, 1.05, 1.10);
        TempoStatistics stats = Calculator().Tempo.Calculate(cube, loadout, context);

        Assert.Equal(_balance.AttackSpeedMaximum, stats.AttackSpeed.Value, 10);
        Assert.Equal(_balance.CastSpeedMaximum, stats.CastSpeed.Value, 10);
        Assert.Equal(_balance.CooldownReductionMaximum, stats.CooldownReduction.Value, 10);
        Assert.Equal(_balance.PhysicalEvasionMaximum, stats.PhysicalEvasion.Value, 10);
        Assert.Equal(_balance.ElementalEvasionMaximum, stats.ElementalEvasion.Value, 10);
        Assert.Equal(_balance.MovementSpeedMaximum, stats.MovementSpeed.Value, 10);

        var domainCube = new FounderCubeProfile(50, 50, 0, 100, 100, 0);
        var domainGear = new GearSupportProfile(0, 0, 0, 12, 12, 0);
        TempoStatistics domainStats = Calculator().Tempo.Calculate(
            domainCube,
            Loadout(WeaponFamily.Daggers, 1.20, 1.20, domainGear),
            context);
        Assert.Equal(_balance.CriticalChanceMaximum, domainStats.CriticalChance.Value, 10);
        Assert.Equal(_balance.CooldownReductionMaximum, domainStats.CooldownReduction.Value, 10);
    }

    [Fact]
    public void ChannelAndMitigationCapsReportWhenApplied()
    {
        var cube = new FounderCubeProfile(100, 0, 100, 0, 100, 0);
        var support = new GearSupportProfile(12, 0, 12, 0, 12, 0);
        EquipmentLoadout loadout = Loadout(WeaponFamily.Hammer, 1.20, 1.20, support);
        var context = new StatCalculationContext(20, 1.05, 1.10);

        OffensiveStatistics offense = Calculator().Offense.Calculate(cube, loadout, context);
        DefensiveStatistics defense = Calculator().Defense.Calculate(cube, loadout, context);

        Assert.Equal(_balance.MaximumChannelPower, offense.PhysicalChannelPower.Value, 10);
        Assert.True(offense.PhysicalChannelPower.Breakdown.WasCapped);
        Assert.Equal(_balance.MaximumChannelPower, offense.PhysicalChannelPower.Breakdown.AppliedCap);
        Assert.Equal(_balance.MaximumSpecificMitigation, defense.PhysicalMitigation.Value, 10);
        Assert.True(defense.PhysicalMitigation.Breakdown.WasCapped);
    }

    [Fact]
    public void InvalidFactorsChannelsLevelsAndSupportAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new StatCalculationContext(-1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StatCalculationContext(21, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StatCalculationContext(0, 0.49, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StatCalculationContext(0, 1, 1.11));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WeaponChannelProfile(WeaponFamily.Sword, 0.74, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CompetencyProgress(WeaponFamily.Sword, 21, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GearSupportProfile(-1, 0, 0, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Loadout(
            WeaponFamily.Sword,
            1,
            1,
            new GearSupportProfile(13, 0, 0, 0, 0, 0)));
    }

    [Fact]
    public void WeaponExperienceFollowsTheThreeLearningTiers()
    {
        // Ardhen reaches Fracture, Paralysis and Bleeding. For a Fracture
        // citizen: Hammer is their own, Sword belongs to Bleeding which their
        // people know, and Bow belongs to Poisoning which no Ardhen cube reaches.
        var nature = new CombatNature(ElementalAffinity.Earth, PhysicalExpression.Fracture);
        var natural = new CompetencyProgress(WeaponFamily.Hammer, 0, 0)
            .GrantGeneratedExperience(100, LineageId.Ardhen, nature);
        var familiar = new CompetencyProgress(WeaponFamily.Sword, 0, 0)
            .GrantGeneratedExperience(100, LineageId.Ardhen, nature);
        var foreign = new CompetencyProgress(WeaponFamily.Bow, 0, 0)
            .GrantGeneratedExperience(100, LineageId.Ardhen, nature);

        Assert.Equal(100, natural.Experience);
        Assert.Equal(50, familiar.Experience);
        Assert.Equal(10, foreign.Experience);
    }

    [Fact]
    public void EquipmentNeverMutatesPersistedCube()
    {
        Citizen citizen = TestCitizen(ElementalAffinity.Earth);
        FounderCubeProfile original = citizen.CubeProfile;
        citizen.SetEquipmentLoadout(Reference("Aren").Loadout);
        _ = new CitizenStatisticsService().Calculate(citizen, 1);
        citizen.SetEquipmentLoadout(EquipmentLoadout.Empty);

        DefensiveStatistics defenseWithoutWeapon =
            new CitizenStatisticsService().CalculateDefense(citizen, 1);

        Assert.Same(original, citizen.CubeProfile);
        Assert.Equal(100, citizen.CubeProfile.Body + citizen.CubeProfile.Bond);
        Assert.True(defenseWithoutWeapon.MaxHealth.Value > 0);
        Assert.Null(defenseWithoutWeapon.MaxHealth.Breakdown.SkillFactor);
        Assert.Null(defenseWithoutWeapon.MaxHealth.Breakdown.ConditionFactor);
        Assert.Null(defenseWithoutWeapon.MaxHealth.Breakdown.CitySupportFactor);
    }

    [Fact]
    public void AffinityChangesNeitherChannelPowerNorPhysicalExpression()
    {
        // Two citizens off the same Ardhen vertex, differing only in affinity.
        // The expression is a function of the cube, so it cannot move here — and
        // neither channel power reacts to the affinity either.
        Citizen earth = TestCitizen(ElementalAffinity.Earth);
        Citizen air = TestCitizen(ElementalAffinity.Air);
        EquipmentLoadout loadout = Reference("Aren").Loadout;
        earth.SetEquipmentLoadout(loadout);
        air.SetEquipmentLoadout(loadout);

        DerivedStatistics earthStats = new CitizenStatisticsService().Calculate(earth, 1);
        DerivedStatistics airStats = new CitizenStatisticsService().Calculate(air, 1);

        Assert.NotEqual(earth.CombatNature.ElementalAffinity, air.CombatNature.ElementalAffinity);
        Assert.Equal(earth.CombatNature.PhysicalExpression, air.CombatNature.PhysicalExpression);
        Assert.Equal(earthStats.Offense.PhysicalChannelPower.Value, airStats.Offense.PhysicalChannelPower.Value);
        Assert.Equal(earthStats.Offense.ElementalChannelPower.Value, airStats.Offense.ElementalChannelPower.Value);
    }

    [Fact]
    public void PhysicalExpressionComesFromTheCubeNotTheAffinity()
    {
        // The obsolete rule read Earth as Fracture, Fire as Stunning and so on.
        // The cube decides now: this Ardhen citizen is Fracture because Body is
        // their highest face, whichever element they resonate with.
        foreach (ElementalAffinity affinity in Enum.GetValues<ElementalAffinity>())
        {
            Citizen citizen = TestCitizen(affinity);

            Assert.Equal(affinity, citizen.CombatNature.ElementalAffinity);
            Assert.Equal(
                CubeExpression.Derive(citizen.CubeProfile),
                citizen.CombatNature.PhysicalExpression);
        }
    }

    private StatisticsCalculator Calculator() => new(_balance);
    private StatCalculationContext NeutralContext() => new(0, 1, 1, _balance);

    private DefensiveStatistics Defense(string name)
    {
        ReferenceCitizen fixture = Reference(name);
        return Calculator().Defense.Calculate(fixture.Cube, fixture.Loadout, NeutralContext());
    }

    private TempoStatistics Tempo(string name)
    {
        ReferenceCitizen fixture = Reference(name);
        return Calculator().Tempo.Calculate(fixture.Cube, fixture.Loadout, NeutralContext());
    }

    private TempoStatistics TempoForImpulse(int impulse)
    {
        var cube = new FounderCubeProfile(50, 50, 100 - impulse, impulse, 50, 50);
        return Calculator().Tempo.Calculate(cube, Loadout(WeaponFamily.Orb, 1, 1), NeutralContext());
    }

    private static Citizen TestCitizen(ElementalAffinity affinity)
    {
        ElementalAffinityId affinityId = affinity switch
        {
            ElementalAffinity.Earth => ElementalAffinityId.Earth,
            ElementalAffinity.Aether => ElementalAffinityId.Aether,
            ElementalAffinity.Water => ElementalAffinityId.Water,
            ElementalAffinity.Fire => ElementalAffinityId.Fire,
            ElementalAffinity.Silence => ElementalAffinityId.Silence,
            ElementalAffinity.Air => ElementalAffinityId.Air,
            _ => throw new ArgumentOutOfRangeException(nameof(affinity)),
        };
        bool created = CitizenProfile.TryCreate(
            LineageId.Ardhen,
            GenderId.Masculine,
            new[] { AptitudeId.Observation, AptitudeId.Empathy, AptitudeId.ManualPrecision },
            new[] { ProfessionFamilyId.Extraction, ProfessionFamilyId.MedicineCare, ProfessionFamilyId.ResearchEducation },
            affinityId,
            CombatStyleId.DefensiveSupport,
            new[] { WeaponPreferenceId.Polearm },
            new[] { PersonalityTraitId.Patient, PersonalityTraitId.Protective, PersonalityTraitId.Reflective },
            PoliticalOrientationId.Communitarian,
            SpiritualPostureId.Contemplative,
            out CitizenProfile? profile,
            out string error);
        if (!created) throw new InvalidOperationException(error);
        return new Citizen(new CitizenId(99), "Fixture", 99, profile!);
    }

    private static ReferenceCitizen Reference(string name) => name switch
    {
        "Aren" => Fixture(name, 60, 40, 50, 50, 50, 50, 70, 41, 54, 52, 55, 51, WeaponFamily.Hammer, 1.20, 0.75),
        "Seyra" => Fixture(name, 40, 60, 50, 50, 50, 50, 41, 70, 54, 52, 54, 52, WeaponFamily.Bow, 0.85, 1.15),
        "Mira" => Fixture(name, 50, 50, 60, 40, 50, 50, 54, 52, 70, 41, 52, 52, WeaponFamily.Whip, 0.95, 1.00),
        "Tovan" => Fixture(name, 50, 50, 40, 60, 50, 50, 52, 52, 41, 70, 52, 54, WeaponFamily.Orb, 0.75, 1.20),
        "Neris" => Fixture(name, 50, 50, 50, 50, 60, 40, 52, 54, 52, 51, 70, 42, WeaponFamily.Daggers, 1.05, 0.95),
        "Vael" => Fixture(name, 50, 50, 50, 50, 40, 60, 53, 53, 52, 53, 42, 70, WeaponFamily.Spear, 1.10, 1.00),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
    };

    private static ReferenceCitizen Fixture(
        string name,
        int body,
        int bond,
        int stability,
        int impulse,
        int domain,
        int reach,
        double effectiveBody,
        double effectiveBond,
        double effectiveStability,
        double effectiveImpulse,
        double effectiveDomain,
        double effectiveReach,
        WeaponFamily family,
        double physicalTransfer,
        double elementalResonance)
    {
        var cube = new FounderCubeProfile(body, bond, stability, impulse, domain, reach);
        var support = new GearSupportProfile(
            effectiveBody - body,
            effectiveBond - bond,
            effectiveStability - stability,
            effectiveImpulse - impulse,
            effectiveDomain - domain,
            effectiveReach - reach);
        return new ReferenceCitizen(name, cube, Loadout(family, physicalTransfer, elementalResonance, support));
    }

    private static EquipmentLoadout Loadout(
        WeaponFamily family,
        double physicalTransfer,
        double elementalResonance,
        GearSupportProfile? support = null) =>
        new(
            new WeaponChannelProfile(family, physicalTransfer, elementalResonance),
            support ?? GearSupportProfile.None,
            GearSupportProfile.None,
            GearSupportProfile.None,
            GearSupportProfile.None,
            GearSupportProfile.None);

    private static void AssertClose(double expected, double actual) =>
        Assert.InRange(actual, expected - Tolerance, expected + Tolerance);

    private sealed record ReferenceCitizen(
        string Name,
        FounderCubeProfile Cube,
        EquipmentLoadout Loadout);
}
