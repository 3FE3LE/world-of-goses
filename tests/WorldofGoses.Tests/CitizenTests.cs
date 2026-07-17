using WorldofGoses.Domain;
using Xunit;
using static WorldofGoses.Tests.TestHelpers;

namespace WorldofGoses.Tests;

public class CitizenTests
{
    [Fact]
    public void Constructor_StartsWithNoCompetenciesOrRoles()
    {
        var c = NewCitizen(1);
        Assert.Empty(c.Competencies);
        Assert.Empty(c.Roles);
        Assert.Equal(Availability.Available, c.Availability);
        Assert.Null(c.CurrentAssignment);
    }

    [Fact]
    public void AddExperience_NewCompetency_CreatesEntry()
    {
        var c = NewCitizen(1);
        c.AddExperience(CompetencyId.Mining, 5);
        Assert.Equal(5, c.GetExperience(CompetencyId.Mining));
        Assert.True(c.Competencies.ContainsKey(CompetencyId.Mining));
    }

    [Fact]
    public void AddExperience_ExistingCompetency_Accumulates()
    {
        var c = NewCitizen(1);
        c.AddExperience(CompetencyId.Mining, 3);
        c.AddExperience(CompetencyId.Mining, 4);
        Assert.Equal(7, c.GetExperience(CompetencyId.Mining));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void AddExperience_NonPositive_NoChange(int amount)
    {
        var c = NewCitizen(1);
        c.AddExperience(CompetencyId.Mining, amount);
        Assert.Equal(0, c.GetExperience(CompetencyId.Mining));
        Assert.False(c.Competencies.ContainsKey(CompetencyId.Mining));
    }

    [Fact]
    public void GetExperience_UnknownCompetency_IsZero()
    {
        var c = NewCitizen(1);
        Assert.Equal(0, c.GetExperience(CompetencyId.Mining));
    }

    [Fact]
    public void GrantRole_FirstTime_CreatesEntry()
    {
        var c = NewCitizen(1);
        c.GrantRole(RoleId.Miner, atTick: 5);
        Assert.True(c.HasRole(RoleId.Miner));
        Assert.Single(c.Roles);
        Assert.Equal(5, c.Roles[0].GrantedAtTick);
    }

    [Fact]
    public void GrantRole_SameRole_RefreshesTick()
    {
        var c = NewCitizen(1);
        c.GrantRole(RoleId.Miner, atTick: 5);
        c.GrantRole(RoleId.Miner, atTick: 9);
        Assert.Single(c.Roles);
        Assert.Equal(9, c.Roles[0].GrantedAtTick);
    }

    [Fact]
    public void RevokeRole_Present_ReturnsTrue()
    {
        var c = NewCitizen(1);
        c.GrantRole(RoleId.Miner, atTick: 0);
        Assert.True(c.RevokeRole(RoleId.Miner));
        Assert.False(c.HasRole(RoleId.Miner));
        Assert.Empty(c.Roles);
    }

    [Fact]
    public void RevokeRole_Absent_ReturnsFalse()
    {
        var c = NewCitizen(1);
        Assert.False(c.RevokeRole(RoleId.Miner));
        Assert.Empty(c.Roles);
    }

    [Fact]
    public void Availability_FollowsCurrentAssignment()
    {
        var c = NewCitizen(1);
        Assert.Equal(Availability.Available, c.Availability);

        c.AssignTo(new BuildingId(1));
        Assert.Equal(Availability.Assigned, c.Availability);

        c.ClearAssignment();
        Assert.Equal(Availability.Available, c.Availability);
    }
}
