#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// Derives a citizen's physical expression from their Kovari Cube.
/// </summary>
/// <remarks>
/// <para>
/// The expression is the highest face of the final <see cref="FounderCubeProfile"/>.
/// It is a pure function of that profile: the same cube always yields the same
/// expression, whatever the citizen's elemental affinity, lineage or history.
/// That purity is the point — the cube is persisted, so nothing else has to be,
/// and no stored copy can drift away from it.
/// </para>
/// <para>
/// The face-to-expression table is canon
/// (<c>docs/systems/statistics-and-combat.md</c>).
/// That chapter publishes one table with three columns — face, elemental
/// affinity, physical expression — and an earlier implementation read it as a
/// chain, deriving the expression from the affinity. The columns are three
/// independent correspondences; only face → expression belongs here.
/// </para>
/// </remarks>
public static class CubeExpression
{
    /// <summary>
    /// Face order used to break a tie, highest priority first. Explicit and
    /// public so a test can assert it: a deterministic fallback that lives in
    /// enum declaration order, dictionary iteration or collection insertion
    /// order is a fallback nobody can see changing.
    /// </summary>
    public static IReadOnlyList<CubeFace> CanonicalTieOrder { get; } = new[]
    {
        CubeFace.Body,
        CubeFace.Bond,
        CubeFace.Stability,
        CubeFace.Impulse,
        CubeFace.Domain,
        CubeFace.Reach,
    };

    public static PhysicalExpression ForFace(CubeFace face) => face switch
    {
        CubeFace.Body => PhysicalExpression.Fracture,
        CubeFace.Bond => PhysicalExpression.Poisoning,
        CubeFace.Stability => PhysicalExpression.Paralysis,
        CubeFace.Impulse => PhysicalExpression.Stunning,
        CubeFace.Domain => PhysicalExpression.Bleeding,
        CubeFace.Reach => PhysicalExpression.Knockdown,
        _ => throw new ArgumentOutOfRangeException(nameof(face), face, "Unknown cube face."),
    };

    /// <summary>Highest face of the profile, ties resolved by <see cref="CanonicalTieOrder"/>.</summary>
    public static CubeFace HighestFace(FounderCubeProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        // Strict `>` while walking the canonical order, so the first face of an
        // equal group wins and the result never depends on iteration accident.
        CubeFace best = CanonicalTieOrder[0];
        int bestValue = profile.For(best);
        for (int i = 1; i < CanonicalTieOrder.Count; i++)
        {
            CubeFace face = CanonicalTieOrder[i];
            int value = profile.For(face);
            if (value > bestValue)
            {
                best = face;
                bestValue = value;
            }
        }

        return best;
    }

    public static PhysicalExpression Derive(FounderCubeProfile profile) =>
        ForFace(HighestFace(profile));

    /// <summary>
    /// The three expressions a lineage can actually produce, read from its
    /// vertex rather than from a per-lineage table.
    /// </summary>
    /// <remarks>
    /// Under the canonical 60/40 vertex with the ±8 onboarding cap, a favoured
    /// face stays within 52–68 and its opposite within 32–48. The highest face
    /// is therefore always one of the three favoured ones, so a lineage's
    /// complementary expressions are unreachable by construction — no blacklist
    /// is needed, and none exists.
    /// </remarks>
    public static IReadOnlyList<PhysicalExpression> NaturallyAvailableTo(LineageId lineage)
    {
        FounderCubeProfile vertex = CubeScoring.ComputeCubeVertex(lineage);
        var expressions = new List<PhysicalExpression>(3);
        foreach (CubeFace face in CanonicalTieOrder)
        {
            if (vertex.For(face) == CubeScoring.VertexHigh)
            {
                expressions.Add(ForFace(face));
            }
        }

        return expressions;
    }
}
