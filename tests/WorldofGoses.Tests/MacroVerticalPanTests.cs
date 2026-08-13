using System.IO;
using System.Text.RegularExpressions;
using WorldofGoses.Prototypes;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// GitHub #14. <c>MacroCameraController.VerticalPanDirection</c> documented
/// "<c>-1</c> up, <c>1</c> down" while every call site passed <c>+1</c> for
/// <c>PanUp</c> — the written convention and the executed one were exact
/// opposites, and the first press and the hold-repeat each restated it
/// separately. At the ends of the street range the clamp then swallows one of
/// the two directions, which is how an inverted reading surfaces to a player
/// as "vertical pan does nothing" rather than as "vertical pan goes the wrong
/// way".
///
/// <para>
/// These assert the direction against the projection that actually puts a
/// street on screen, not merely against the sign of an integer.
/// </para>
/// </summary>
public sealed class MacroVerticalPanTests
{
    private const int StreetCount = 13;

    [Fact]
    public void TheTwoVerticalStepsAreExactOpposites()
    {
        Assert.Equal(
            -MacroCameraController.PanTowardViewer,
            MacroCameraController.PanAwayFromViewer);
        Assert.NotEqual(0, MacroCameraController.PanAwayFromViewer);
    }

    [Theory]
    [InlineData(true, false, MacroCameraController.PanAwayFromViewer)]
    [InlineData(false, true, MacroCameraController.PanTowardViewer)]
    [InlineData(false, false, 0)]
    // Both keys held cancel rather than letting whichever branch is tested
    // first decide. Two opposing steps in one frame are not a direction.
    [InlineData(true, true, 0)]
    public void HeldKeysResolveToOneStep(bool panAway, bool panToward, int expected)
    {
        Assert.Equal(expected, MacroCameraController.VerticalStepFor(panAway, panToward));
    }

    /// <summary>
    /// The acceptance criterion that a number cannot answer: pressing "up"
    /// must move the viewpoint the way the player reads as up.
    ///
    /// <para>
    /// Every street renders at <c>street - cameraAnchor</c>, and
    /// <see cref="StreetDepthProjection.RowScreenY"/> converts a smaller depth
    /// into a <em>larger</em> screen Y — further down the screen. So advancing
    /// the anchor by <see cref="MacroCameraController.PanAwayFromViewer"/>
    /// slides the streets already on screen downward while the viewpoint
    /// travels up the perspective toward the horizon, which is what the player
    /// sees as panning up. The opposite step must move it the other way by the
    /// same amount.
    /// </para>
    /// </summary>
    [Fact]
    public void PanningAwayMovesTheWorldDownTheScreenAndPanningTowardMovesItUp()
    {
        const float baseY = 620f;
        const int observedStreet = 6;
        const int anchor = 4;

        float restingY = StreetDepthProjection.RowScreenY(observedStreet - anchor, baseY);
        float afterPanAway = StreetDepthProjection.RowScreenY(
            observedStreet - (anchor + MacroCameraController.PanAwayFromViewer),
            baseY);
        float afterPanToward = StreetDepthProjection.RowScreenY(
            observedStreet - (anchor + MacroCameraController.PanTowardViewer),
            baseY);

        Assert.True(
            afterPanAway > restingY,
            $"PanUp must slide the world down the screen: {restingY} -> {afterPanAway}.");
        Assert.True(
            afterPanToward < restingY,
            $"PanDown must slide the world up the screen: {restingY} -> {afterPanToward}.");
    }

    [Fact]
    public void AtTheNearEdgeOnlyTheOutOfRangeDirectionIsRefused()
    {
        const int nearest = 0;

        Assert.Equal(
            nearest,
            MacroCameraController.ClampStreetStep(
                nearest, MacroCameraController.PanTowardViewer, StreetCount));
        Assert.Equal(
            nearest + 1,
            MacroCameraController.ClampStreetStep(
                nearest, MacroCameraController.PanAwayFromViewer, StreetCount));
    }

    [Fact]
    public void AtTheFarEdgeOnlyTheOutOfRangeDirectionIsRefused()
    {
        int farthest = StreetCount - 1;

        Assert.Equal(
            farthest,
            MacroCameraController.ClampStreetStep(
                farthest, MacroCameraController.PanAwayFromViewer, StreetCount));
        Assert.Equal(
            farthest - 1,
            MacroCameraController.ClampStreetStep(
                farthest, MacroCameraController.PanTowardViewer, StreetCount));
    }

    /// <summary>
    /// From an interior street both directions move, and they move to
    /// opposite streets — the "desde una calle interior, ambas direcciones
    /// cambian de calle y el resultado visual es opuesto" criterion.
    /// </summary>
    [Fact]
    public void FromAnInteriorStreetBothDirectionsMoveAndDisagree()
    {
        const int interior = 5;

        int away = MacroCameraController.ClampStreetStep(
            interior, MacroCameraController.PanAwayFromViewer, StreetCount);
        int toward = MacroCameraController.ClampStreetStep(
            interior, MacroCameraController.PanTowardViewer, StreetCount);

        Assert.NotEqual(interior, away);
        Assert.NotEqual(interior, toward);
        Assert.Equal(2, away - toward);
    }

    /// <summary>
    /// A degenerate world must not be steppable into a negative street. The
    /// clamp is the only thing standing between a not-yet-sized view and an
    /// index that no renderer can project.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void AStreetCountWithNoRoomNeverMoves(int streetCount)
    {
        Assert.Equal(
            0,
            MacroCameraController.ClampStreetStep(
                0, MacroCameraController.PanAwayFromViewer, streetCount));
        Assert.Equal(
            0,
            MacroCameraController.ClampStreetStep(
                0, MacroCameraController.PanTowardViewer, streetCount));
    }

    /// <summary>
    /// The first press and the key-repeat must share the convention rather
    /// than each spelling out a sign. The repeat path reads its step through
    /// <c>MacroCameraController.VerticalStepFor</c>; the press path names the
    /// same two constants. A literal <c>1</c> or <c>-1</c> handed to
    /// <c>BeginVerticalCameraPan</c> is the shape that drifted.
    /// </summary>
    [Fact]
    public void PressAndRepeatShareOneConvention()
    {
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(),
            "game", "scripts", "Prototypes", "MacroStreetLiveView.cs"));

        Assert.Contains(
            "BeginVerticalCameraPan(MacroCameraController.PanAwayFromViewer)",
            source,
            System.StringComparison.Ordinal);
        Assert.Contains(
            "BeginVerticalCameraPan(MacroCameraController.PanTowardViewer)",
            source,
            System.StringComparison.Ordinal);
        Assert.Contains(
            "MacroCameraController.VerticalStepFor(",
            source,
            System.StringComparison.Ordinal);
        Assert.DoesNotMatch(
            new Regex(@"BeginVerticalCameraPan\(\s*-?\d+\s*\)"),
            source);
    }

    /// <summary>
    /// The half of #14 a sign convention could not explain: <c>_Input</c>
    /// claimed the arrow keys for the world with
    /// <c>SetInputAsHandled()</c> and then did nothing with them. Marking an
    /// event handled in <c>_Input</c> stops it before <c>_UnhandledInput</c>,
    /// which is where the vertical pan lived — so W and S produced a step on
    /// the first press and ↑/↓ produced none, catching up only when the
    /// hold-repeat's <c>Input.IsActionPressed</c> poll noticed the key was
    /// already down. Reserving and acting must stay together.
    /// </summary>
    [Fact]
    public void ArrowKeysAreActedOnBeforeTheyAreClaimed()
    {
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(),
            "game", "scripts", "Prototypes", "MacroStreetLiveView.cs"));

        int inputOverride = source.IndexOf(
            "public override void _Input(InputEvent @event)",
            System.StringComparison.Ordinal);
        Assert.True(inputOverride > 0, "MacroStreetLiveView no longer overrides _Input.");

        int unhandledOverride = source.IndexOf(
            "public override void _UnhandledInput(InputEvent @event)",
            System.StringComparison.Ordinal);
        Assert.True(unhandledOverride > inputOverride, "_UnhandledInput moved above _Input.");

        string inputBody = source[inputOverride..unhandledOverride];
        int acts = inputBody.IndexOf(
            "TryHandleCameraNavigationKey(@event)",
            System.StringComparison.Ordinal);
        int claims = inputBody.IndexOf(
            "GetViewport().SetInputAsHandled()",
            System.StringComparison.Ordinal);

        Assert.True(
            acts > 0,
            "_Input claims the arrow keys for the world, so it must also act on them: "
            + "anything it marks handled never reaches _UnhandledInput.");
        Assert.True(
            claims > acts,
            "_Input must act on the camera command before marking the event handled.");
    }

    /// <summary>
    /// The cause the semantic fix did not reach, measured in the running
    /// game: a fresh city owns three parcels in a single row, so deriving the
    /// navigable envelope from owned parcels alone collapsed the world to
    /// three streets — and the free camera opens on street 2. It was parked on
    /// the last row with every <c>PanUp</c> clamped to a no-op, able to move
    /// toward the viewer twice and then not at all.
    ///
    /// <para>
    /// Asserted as arithmetic over the real constants rather than through the
    /// engine: with the floor in place the opening street must have room in
    /// <em>both</em> directions.
    /// </para>
    /// </summary>
    [Fact]
    public void AFreshCityLeavesRoomToPanInBothDirections()
    {
        // What a fresh city actually owns: three parcels across one row.
        const int freshCityParcelRows = 1;
        int rows = System.Math.Max(
            MacroViewConstants.DefaultWorldParcelRows,
            freshCityParcelRows);
        int streetCount = rows * WorldofGoses.Domain.ParcelGrid.ConstructionRowsPerParcel;
        int openingStreet = System.Math.Clamp(2, 0, streetCount - 1);

        Assert.True(
            streetCount > 3,
            $"A fresh city must not collapse the navigable world to its owned "
            + $"parcel row; got {streetCount} streets.");
        Assert.NotEqual(
            openingStreet,
            MacroCameraController.ClampStreetStep(
                openingStreet, MacroCameraController.PanAwayFromViewer, streetCount));
        Assert.NotEqual(
            openingStreet,
            MacroCameraController.ClampStreetStep(
                openingStreet, MacroCameraController.PanTowardViewer, streetCount));
    }

    /// <summary>
    /// The envelope is recomputed on every snapshot and nothing else rechecks
    /// the camera against it, so the recompute has to pull the camera back in
    /// itself or a shallower world strands it on a street that no longer
    /// exists — where both directions clamp.
    /// </summary>
    [Fact]
    public void ShrinkingTheEnvelopeReclampsTheCamera()
    {
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(),
            "game", "scripts", "Prototypes", "MacroStreetLiveView.cs"));

        int envelope = source.IndexOf(
            "private void RefreshParcelEnvelope(",
            System.StringComparison.Ordinal);
        Assert.True(envelope > 0, "RefreshParcelEnvelope is gone.");

        Assert.Contains(
            "_worldParcelRows = Math.Max(DefaultWorldParcelRows, maximumRow + 1);",
            source,
            System.StringComparison.Ordinal);
        Assert.Contains(
            "_worldParcelColumns = Math.Max(DefaultWorldParcelColumns, maximumColumn + 1);",
            source,
            System.StringComparison.Ordinal);
        Assert.Contains(
            "KeepFreeCameraInsideTheWorld();",
            source[envelope..],
            System.StringComparison.Ordinal);
    }

    /// <summary>
    /// The defect the first three attempts missed. A4 moved the camera's depth
    /// state onto <see cref="MacroCameraController"/> but left the per-frame
    /// advance in the view, which read the three properties into locals,
    /// stepped those, and dropped them. The founder's equivalent stored its
    /// result back; the camera's did not, so the anchor the projection reads
    /// restarted from the same value every frame while the target was set
    /// correctly.
    ///
    /// <para>
    /// This exercises the controller's own state, so it fails if the advance
    /// ever goes back to operating on copies: the anchor simply would not
    /// move.
    /// </para>
    /// </summary>
    [Fact]
    public void AdvancingTheDepthTransitionPersistsOnTheController()
    {
        var camera = new MacroCameraController
        {
            CameraDepthAnchor = 2f,
            CameraDepthTarget = 4f,
        };

        // One cadence tick at the transition's own step size.
        camera.AdvanceDepthTransition(PixelMotion.CadenceSeconds, MacroViewConstants.DepthStepSize);

        Assert.True(
            camera.CameraDepthAnchor > 2f,
            "The anchor must advance on the controller, not on a copy the caller drops.");
        Assert.Equal(4f, camera.CameraDepthTarget);

        // And it must arrive, clearing the target rather than orbiting it.
        int safety = 1000;
        while (camera.CameraDepthTarget.HasValue && safety-- > 0)
        {
            camera.AdvanceDepthTransition(PixelMotion.CadenceSeconds, MacroViewConstants.DepthStepSize);
        }

        Assert.True(safety > 0, "The depth transition never reached its target.");
        Assert.Equal(4f, camera.CameraDepthAnchor);
        Assert.Null(camera.CameraDepthTarget);
    }

    /// <summary>
    /// The opposite direction persists identically — a camera walking back
    /// toward the viewer must not be a special case.
    /// </summary>
    [Fact]
    public void TheDepthTransitionPersistsInBothDirections()
    {
        var camera = new MacroCameraController
        {
            CameraDepthAnchor = 5f,
            CameraDepthTarget = 3f,
        };

        int safety = 1000;
        while (camera.CameraDepthTarget.HasValue && safety-- > 0)
        {
            camera.AdvanceDepthTransition(PixelMotion.CadenceSeconds, MacroViewConstants.DepthStepSize);
        }

        Assert.True(safety > 0);
        Assert.Equal(3f, camera.CameraDepthAnchor);
    }

    /// <summary>
    /// A completed transition is inert. Without the null-target guard the
    /// accumulator would keep growing and the next real transition would jump
    /// its whole backlog in one frame instead of walking it.
    /// </summary>
    [Fact]
    public void ACompletedTransitionDoesNotDriftOrBankTime()
    {
        var camera = new MacroCameraController { CameraDepthAnchor = 3f };

        camera.AdvanceDepthTransition(10.0, MacroViewConstants.DepthStepSize);

        Assert.Equal(3f, camera.CameraDepthAnchor);
        Assert.Null(camera.CameraDepthTarget);
        Assert.Equal(0f, camera.CameraTransitionAccumulator);
    }

    /// <summary>
    /// The view must not go back to stepping copies: the advance belongs to
    /// whoever owns the fields.
    /// </summary>
    [Fact]
    public void TheViewDelegatesTheCameraAdvanceToTheController()
    {
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(),
            "game", "scripts", "Prototypes", "MacroStreetLiveView.cs"));

        Assert.Contains(
            "_camera.AdvanceDepthTransition(",
            source,
            System.StringComparison.Ordinal);
        Assert.DoesNotMatch(
            new Regex(@"float\s+cameraDepthAnchor\s*=\s*_camera\.CameraDepthAnchor"),
            source);
    }

    /// <summary>
    /// The lateral axis is untouched by #14 and must stay that way: A/← move
    /// one way, D/→ the other, and neither is expressed through the vertical
    /// convention.
    /// </summary>
    [Fact]
    public void LateralPanKeepsItsOwnUnchangedConvention()
    {
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(),
            "game", "scripts", "Prototypes", "MacroStreetLiveView.cs"));

        Assert.Contains(
            "if (Input.IsActionPressed(CameraInputActions.PanLeft)) return -1f;",
            source,
            System.StringComparison.Ordinal);
        Assert.Contains(
            "if (Input.IsActionPressed(CameraInputActions.PanRight)) return 1f;",
            source,
            System.StringComparison.Ordinal);
    }
}
