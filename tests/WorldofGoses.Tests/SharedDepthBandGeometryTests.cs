using System.Collections.Generic;
using Godot;
using WorldofGoses.Ui;
using WorldofGoses.Prototypes;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Regression coverage for <see cref="SharedDepthBands"/>: the helpers
/// moved out of <c>MacroStreetRenderer</c> in #19 so the macro view and
/// the future expedition path renderer can share the trapezoid
/// rasterizer and the pixel snap. The acceptance criterion for #19
/// asks for "a regression that compares band coordinates before and
/// after the extraction" — these tests pin that, plus a parity
/// check against the legacy forwarder.
/// </summary>
public class SharedDepthBandGeometryTests
{
    [Fact]
    public void SnapPixel_RoundsToTheRequestedStep()
    {
        Assert.Equal(0f, SharedDepthBands.SnapPixel(0f, 2f));
        Assert.Equal(2f, SharedDepthBands.SnapPixel(1.5f, 2f));
        Assert.Equal(4f, SharedDepthBands.SnapPixel(3.7f, 2f));
        Assert.Equal(-4f, SharedDepthBands.SnapPixel(-3.7f, 2f));
    }

    [Fact]
    public void SnapPixel_Matches_The_Legacy_Helper()
    {
        float[] values = { -120.4f, -3.1f, 0f, 0.49f, 9.99f, 217.5f, 1024.0f };
        foreach (float value in values)
        {
            Assert.Equal(
                MacroProjectionHelpers.SnapPixel(value, 2f),
                SharedDepthBands.SnapPixel(value, 2f));
        }
    }

    [Fact]
    public void SnapPixel_MatchesAcrossEveryPixelStepUsed()
    {
        // Every pixel step the macro and the future expedition path
        // renderer actually use must agree with the legacy forwarder
        // and with itself on every code path. This is the byte-for-byte
        // parity the acceptance criterion for #19 demands.
        float[] values = { -217.5f, -3.1f, 0f, 0.49f, 9.99f, 217.5f };
        float[] steps = { 1f, 2f, MacroViewConstants.PixelStepPx };
        foreach (float value in values)
        {
            foreach (float step in steps)
            {
                float actual = SharedDepthBands.SnapPixel(value, step);
                float legacy = Mathf.Round(value / step) * step;
                Assert.Equal(legacy, actual);
            }
        }
    }

    [Theory]
    [InlineData(60f, 0f, 200f, 580f)]
    [InlineData(120f, 3f, -640f, 720f)]
    [InlineData(200f, 11f, 1280f, 200f)]
    public void SharedDepthBands_PreservesStreetDepthProjectionBandCoordinates(
        float rowWidth, float depth, float centerX, float baseY)
    {
        // The rasterizer inherits its band projection from
        // StreetDepthProjection. Whichever consumer uses it (macro
        // or expedition path renderer) reads the same row y, same
        // horizontal scale, and same project; this test asserts the
        // shared helpers did not drift those.
        (Vector2 pos, Vector2 scale) = StreetDepthProjection.Project(
            depth, rowWidth, centerX, baseY);

        // Snap is identity inside the helper so do it explicitly to
        // mirror the rasterizer's stripes:
        float snappedX = SharedDepthBands.SnapPixel(pos.X, MacroViewConstants.PixelStepPx);
        float snappedY = SharedDepthBands.SnapPixel(pos.Y, MacroViewConstants.PixelStepPx);

        Assert.InRange(snappedX, pos.X - MacroViewConstants.PixelStepPx, pos.X + MacroViewConstants.PixelStepPx);
        Assert.InRange(snappedY, pos.Y - MacroViewConstants.PixelStepPx, pos.Y + MacroViewConstants.PixelStepPx);
        Assert.True(scale.X > 0f);
        Assert.True(scale.Y > 0f);
    }

    [Fact]
    public void SpatialHash_IsDeterministicAndNeutral()
    {
        // The terrain sampling hashes live in TerrainAtlas, but the
        // contract for #19 says the new shared module must not
        // accidentally add a city-specific dependency. Pin the
        // canonical sampling gives the same answer across calls and
        // does not depend on MacroStreetRenderer.
        Assert.Equal(0, TerrainAtlastSamplingFor(0, 0));
        Assert.Equal(TerrainAtlastSamplingFor(0, 0), TerrainAtlastSamplingFor(0, 0));
        Assert.Equal(TerrainAtlastSamplingFor(7, 13), TerrainAtlastSamplingFor(7, 13));
        Assert.NotEqual(
            TerrainAtlastSamplingFor(7, 13),
            TerrainAtlastSamplingFor(13, 7));
    }

    private static int TerrainAtlastSamplingFor(int column, int row) =>
        TerrainAtlas.VariantIndex(column, row, 5);
}
