using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public class ConstructionProjectTests
{
    [Fact]
    public void Constructor_DefaultsToBasicShelterZeroProgress()
    {
        var project = new ConstructionProject(
            new BuildingId(1), ConstructionKind.BasicShelter, "Basic Shelter",
            requiredWork: ConstructionRules.RequiredWork,
            workerCapacity: ConstructionRules.WorkerCapacity);

        Assert.Equal(0, project.Progress);
        Assert.Equal(ConstructionKind.BasicShelter, project.Kind);
        Assert.Equal(ConstructionStopCause.NoWorkers, project.StopCause);
        Assert.False(project.IsComplete);
        Assert.Equal(0, project.AssignedCount);
    }

    [Fact]
    public void TryAssign_RespectsCapacity()
    {
        var project = NewProject(workerCapacity: 2);
        Assert.True(project.TryAssign(new CitizenId(1)));
        Assert.True(project.TryAssign(new CitizenId(2)));
        Assert.False(project.TryAssign(new CitizenId(3)));
    }

    [Fact]
    public void TryAssign_AlreadyAssigned_Fails()
    {
        var project = NewProject();
        project.TryAssign(new CitizenId(1));
        Assert.False(project.TryAssign(new CitizenId(1)));
    }

    [Fact]
    public void TryUnassign_NotAssigned_Fails()
    {
        var project = NewProject();
        Assert.False(project.TryUnassign(new CitizenId(1)));
    }

    [Fact]
    public void TryUnassign_RemovesContributor()
    {
        var project = NewProject();
        project.TryAssign(new CitizenId(1));
        Assert.True(project.TryUnassign(new CitizenId(1)));
        Assert.False(project.IsAssigned(new CitizenId(1)));
    }

    private static ConstructionProject NewProject(int workerCapacity = 4) =>
        new(new BuildingId(1), ConstructionKind.BasicShelter, "Basic Shelter",
            requiredWork: 100, workerCapacity: workerCapacity);
}
