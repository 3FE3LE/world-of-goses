#nullable enable
using Godot;

namespace WorldofGoses.Prototypes;

/// <summary>
/// The macro view's one stepped depth advancer: it walks an anchor toward a
/// target in fixed increments at the pixel-motion cadence, never as a
/// continuous tween (design bible §08, pixel-motion grammar).
/// </summary>
/// <remarks>
/// <para>
/// Three things move through depth on their own clock — the founder's
/// row-crossing, each recruited citizen's, and the free camera's — and all
/// three want the same discrete walk. Keeping the algorithm here rather than
/// as a private static on whichever type happened to need it first is what
/// lets <see cref="MacroCameraController"/> own its own advance without
/// either duplicating the loop or reaching back into the view for it.
/// </para>
/// <para>
/// The <c>ref</c> shape is deliberate and is also where GitHub #14 went
/// wrong. Advancing into locals and forgetting to store them back is
/// invisible: the call compiles, the transition appears to run, and the
/// caller's own state simply never moves — which is exactly what left the
/// camera's <c>CameraDepthAnchor</c> restarting from the old value every
/// frame while its target was set correctly. Callers that own the fields
/// should expose an advance of their own, as the camera controller now does,
/// rather than passing copies around.
/// </para>
/// </remarks>
internal static class SteppedDepthTransition
{
    /// <summary>
    /// Moves <paramref name="anchor"/> one <paramref name="stepSize"/> toward
    /// <paramref name="target"/> per elapsed cadence tick, clearing the target
    /// once it is reached. A null target is a completed transition and does
    /// nothing.
    /// </summary>
    public static void Advance(
        ref float anchor,
        ref float? target,
        ref float accumulator,
        double delta,
        float stepSize)
    {
        if (!target.HasValue) return;
        accumulator += (float)delta;
        while (accumulator >= PixelMotion.CadenceSeconds && target.HasValue)
        {
            accumulator -= PixelMotion.CadenceSeconds;
            float value = target.Value;
            if (Mathf.Abs(value - anchor) <= stepSize)
            {
                anchor = value;
                target = null;
            }
            else
            {
                anchor += Mathf.Sign(value - anchor) * stepSize;
            }
        }
    }
}
