using System;
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class CubeScoringTests
{
    public static TheoryData<LineageId, int, int, int> Vertices => new()
    {
        { LineageId.Ardhen, 60, 60, 60 },
        { LineageId.Eirune, 60, 60, 40 },
        { LineageId.Kovari, 60, 40, 60 },
        { LineageId.Vaelun, 60, 40, 40 },
        { LineageId.Orveth, 40, 60, 60 },
        { LineageId.Myrven, 40, 60, 40 },
        { LineageId.Theryn, 40, 40, 60 },
        { LineageId.Caelith, 40, 40, 40 },
    };

    [Theory]
    [MemberData(nameof(Vertices))]
    public void ComputeCubeVertex_UsesCanonicalSixtyFortyPairs(
        LineageId lineage,
        int body,
        int stability,
        int domain)
    {
        FounderCubeProfile profile = CubeScoring.ComputeCubeVertex(lineage);

        Assert.Equal(body, profile.Body);
        Assert.Equal(stability, profile.Stability);
        Assert.Equal(domain, profile.Domain);
        AssertPairs(profile);
        Assert.Equal(lineage, CubeScoring.ComputeNearestVertex(profile));
    }

    [Fact]
    public void FounderCubeProfile_RejectsPairsThatDoNotSumToOneHundred()
    {
        Assert.Throws<ArgumentException>(() => new FounderCubeProfile(60, 41, 60, 40, 60, 40));
    }

    [Fact]
    public void ApplyContribution_MovesComplementAndSaturatesAtBounds()
    {
        var edge = new FounderCubeProfile(100, 0, 0, 100, 50, 50);
        FounderCubeProfile result = CubeScoring.ApplyContribution(
            edge,
            new ScoreContribution(FounderScoreAxis.Cube, CubeScoring.BodyValueId, 8));
        result = CubeScoring.ApplyContribution(
            result,
            new ScoreContribution(FounderScoreAxis.Cube, CubeScoring.StabilityValueId, -8));

        Assert.Equal((100, 0), (result.Body, result.Bond));
        Assert.Equal((0, 100), (result.Stability, result.Impulse));
        AssertPairs(result);
    }

    [Fact]
    public void Recalculate_ClampsAggregateOnboardingMovementToEightPerAxis()
    {
        var contributions = new List<ScoreContribution>();
        for (int index = 0; index < 20; index++)
        {
            contributions.Add(new ScoreContribution(
                FounderScoreAxis.Cube,
                CubeScoring.BodyValueId,
                1));
            contributions.Add(new ScoreContribution(
                FounderScoreAxis.Cube,
                CubeScoring.ImpulseValueId,
                1));
        }

        FounderCubeProfile result = CubeScoring.Recalculate(LineageId.Ardhen, contributions);

        Assert.Equal((68, 32), (result.Body, result.Bond));
        Assert.Equal((52, 48), (result.Stability, result.Impulse));
        AssertPairs(result);
    }

    [Fact]
    public void Recalculate_AlwaysStartsFromLineageVertex()
    {
        var first = new[]
        {
            new ScoreContribution(FounderScoreAxis.Cube, CubeScoring.BodyValueId, 8),
        };
        var replacement = new[]
        {
            new ScoreContribution(FounderScoreAxis.Cube, CubeScoring.BondValueId, 3),
        };

        FounderCubeProfile changed = CubeScoring.Recalculate(LineageId.Ardhen, first);
        FounderCubeProfile recalculated = CubeScoring.Recalculate(LineageId.Ardhen, replacement);

        Assert.Equal(68, changed.Body);
        Assert.Equal(57, recalculated.Body);
        Assert.NotEqual(65, recalculated.Body);
    }

    [Fact]
    public void LineageSignatures_AreEightSmallDistinctVisibleLabels()
    {
        string[] signatures = new[]
            {
                LineageId.Ardhen,
                LineageId.Eirune,
                LineageId.Kovari,
                LineageId.Vaelun,
                LineageId.Orveth,
                LineageId.Myrven,
                LineageId.Theryn,
                LineageId.Caelith,
            }
            .Select(CubeScoring.Signature)
            .ToArray();

        Assert.Equal(8, signatures.Distinct(StringComparer.Ordinal).Count());
        Assert.All(signatures, signature => Assert.InRange(signature.Length, 5, 18));
    }

    private static void AssertPairs(FounderCubeProfile profile)
    {
        Assert.Equal(100, profile.Body + profile.Bond);
        Assert.Equal(100, profile.Stability + profile.Impulse);
        Assert.Equal(100, profile.Domain + profile.Reach);
    }
}
