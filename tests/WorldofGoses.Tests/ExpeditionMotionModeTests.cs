using WorldofGoses;
using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// The expedition walks in steps and fights in a slide.
/// </summary>
/// <remarks>
/// The motion grammar is discrete everywhere — <see cref="PixelMotion"/> exists
/// to say so — but a fight is the one place that grammar is wrong: impact
/// reactions and camera pans need continuous motion to read at all. The switch
/// is not a setting; it falls out of which call the stage makes, so there is no
/// mode to leave enabled by mistake.
/// </remarks>
public sealed class ExpeditionMotionModeTests
{
    [Fact]
    public void TravelStepsInWholeLocomotionSteps()
    {
        var camera = new ExpeditionPathCamera();

        // A position mid-step must land on the grid, not near it.
        camera.FollowTravel(positionX: 101.0, seed: 1);

        Assert.Equal(ExpeditionMotionMode.Quantized, camera.MotionMode);
        Assert.Equal(0, camera.WorldOffset % ExpeditionPathCamera.StepUnits);
        Assert.Equal(camera.WorldOffsetUnits, camera.WorldOffset);
    }

    /// <summary>
    /// The real symptom: advancing by less than a step must not move the world
    /// at all. Rounding to the nearest world unit moved it every single pixel,
    /// which is continuous motion wearing the grammar's name.
    /// </summary>
    [Fact]
    public void TravelDoesNotMoveForAdvancesSmallerThanAStep()
    {
        var camera = new ExpeditionPathCamera();
        camera.FollowTravel(positionX: 200.0, seed: 1);
        double settled = camera.WorldOffset;

        camera.FollowTravel(positionX: 201.0, seed: 1);

        Assert.Equal(settled, camera.WorldOffset);
    }

    [Fact]
    public void TravelMovesOnceAFullStepIsCovered()
    {
        var camera = new ExpeditionPathCamera();
        camera.FollowTravel(positionX: 200.0, seed: 1);
        double settled = camera.WorldOffset;

        camera.FollowTravel(positionX: 200.0 + ExpeditionPathCamera.StepUnits, seed: 1);

        Assert.Equal(settled + ExpeditionPathCamera.StepUnits, camera.WorldOffset);
    }

    [Fact]
    public void CombatFramesContinuously()
    {
        var camera = new ExpeditionPathCamera();
        camera.FollowTravel(positionX: 500, seed: 1);

        // An arena whose centre is deliberately off the step grid.
        camera.FrameEncounter(arenaMinimumX: 101, arenaMaximumX: 104, seed: 1);

        Assert.Equal(ExpeditionMotionMode.Continuous, camera.MotionMode);
        Assert.Equal(102.5, camera.WorldOffset);
        Assert.NotEqual(0, camera.WorldOffset % ExpeditionPathCamera.StepUnits);
    }

    /// <summary>
    /// Combat ends by the stage going back to drawing travel, so the walk
    /// returns to stepping without anyone remembering to restore a flag.
    /// </summary>
    [Fact]
    public void TravelAfterCombatIsQuantizedAgain()
    {
        var camera = new ExpeditionPathCamera();
        camera.FollowTravel(positionX: 400, seed: 1);
        camera.FrameEncounter(arenaMinimumX: 101, arenaMaximumX: 104, seed: 1);
        Assert.Equal(ExpeditionMotionMode.Continuous, camera.MotionMode);

        camera.FollowTravel(positionX: 401.0, seed: 1);

        Assert.Equal(ExpeditionMotionMode.Quantized, camera.MotionMode);
        Assert.Equal(0, camera.WorldOffset % ExpeditionPathCamera.StepUnits);
    }

    /// <summary>
    /// The step the world walks by is the step a character walks by. Two
    /// cadences would show up as the ground sliding under the feet.
    /// </summary>
    [Fact]
    public void TheWorldStepIsTheLocomotionStep()
    {
        Assert.Equal(PixelMotion.StepPixels, ExpeditionPathCamera.StepUnits);
    }

    /// <summary>
    /// Framing must not rebuild the chunk window: the fight happens on the
    /// stretch of path the party walked in on.
    /// </summary>
    [Fact]
    public void SwitchingModeKeepsTheGroundItWalkedInOn()
    {
        var camera = new ExpeditionPathCamera();
        camera.FollowTravel(positionX: 640, seed: 7);
        ExpeditionPathChunkPool? walked = camera.Chunks;

        camera.FrameEncounter(arenaMinimumX: 0, arenaMaximumX: 1000, seed: 7);

        Assert.Same(walked, camera.Chunks);
    }
}
