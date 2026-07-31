using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Ambient day/night light for the city map. Returns, for any tick
/// fraction in [0, 1) (one full in-game day), the colour the world is
/// <b>multiplied</b> by — not a veil painted on top of it.
///
/// That distinction is the whole point. An alpha-blended overlay computes
/// <c>world·(1−α) + tint·α</c>: it scales contrast down by <c>1−α</c> and
/// raises the black point by <c>tint·α</c>, so a night strong enough to
/// read as night also flattened the map into fog. Multiplying computes
/// <c>world · light</c>: black stays black, the full dynamic range
/// survives, and the strength of the effect is no longer paid for in
/// contrast. Noon is pure white — an exact no-op.
///
/// The day is not a single ramp from night to noon and back: that read as
/// a permanent orange smear, with visible warm tint at 03:00. Nor is it a
/// set of flat plateaus joined by short ramps: a colour that holds
/// perfectly still for five hours and then lurches reads just as
/// artificial. It is a two-speed curve — every stretch moves, but the
/// dawn and dusk bands move an order of magnitude faster:
///
/// <list type="bullet">
/// <item>00:00 → 05:00 — deep night light thinning slowly toward dawn.</item>
/// <item><b>05:00 → 06:00</b> — dawn band, night climbs to sunrise peak.</item>
/// <item><b>06:00 → 07:12</b> — sunrise fades out to a cool morning light.</item>
/// <item>07:12 → 12:00 — the cool cast clears slowly into pure white.</item>
/// <item>12:00 → 16:48 — a faint gold builds just as slowly.</item>
/// <item><b>16:48 → 18:00</b> — dusk warms up to sunset peak.</item>
/// <item><b>18:00 → 19:00</b> — dusk band, sunset drops to night.</item>
/// <item>19:00 → 24:00 — night deepens slowly back to midnight.</item>
/// </list>
///
/// The bold entries are the inflection points the curve is built around;
/// the long stretches exist to connect them without ever standing still.
/// Within every segment the blend is smoothstepped rather than linear, so
/// a transition eases in, moves quickly through its middle, and settles
/// instead of arriving at the anchor at constant speed. Smoothstep also
/// leaves zero slope at both ends of every segment, so a fast band and
/// the slow stretch beside it meet without a visible kink.
///
/// Three properties are load-bearing for the player-facing feel: noon must
/// be exactly white, the small hours must stay clearly blue (no warm bleed
/// at 03:00), and no stretch may be perfectly constant.
/// </summary>
public static class TimeOfDayColor
{
    // Night light. Red is cut hardest and blue least, which is both how
    // moonlight actually reads and what keeps the night unmistakably cold
    // without needing to crush the green channel — green carries most of
    // the luminance detail, so it is what preserves legibility.
    //
    // Night is not one colour: it arrives and leaves lighter at the dusk
    // and dawn anchors and thickens toward midnight. That slow swing is
    // what keeps the long night from looking like a static filter.
    private static readonly Color NightEdge = new(0.48f, 0.58f, 0.80f, 1f);
    private static readonly Color NightDeep = new(0.34f, 0.44f, 0.68f, 1f);

    // Low sun: warm light is warm because it has lost its blue end, so
    // these dim blue and green rather than pushing red past white.
    private static readonly Color Sunrise = new(1.00f, 0.78f, 0.60f, 1f);
    private static readonly Color Sunset = new(1.00f, 0.72f, 0.52f, 1f);

    // Daylight has its own slow arc: a faintly cool cast left over from
    // the morning and a faint gold gathering through the afternoon. Kept
    // deliberately weak — this stretch must read as "daytime", not as a
    // second sunset.
    private static readonly Color MorningCool = new(0.92f, 0.96f, 1.00f, 1f);
    private static readonly Color AfternoonWarm = new(1.00f, 0.95f, 0.86f, 1f);

    // Noon alone is pure white: multiplying by it is the identity, so the
    // macro view renders in its canonical midday palette, untouched.
    private static readonly Color NoonClear = new(1f, 1f, 1f, 1f);

    // Nine anchors. The four short ones (05:00, 06:00, 18:00, 19:00) are
    // the player-visible inflection points: dawn and dusk each get a
    // single in-game hour, which is what makes them feel like events.
    // The long segments between them carry distinct colours at each end
    // rather than a repeated one, so they drift instead of holding still.
    //
    // Anchoring 00:00 and 24:00 on the same deep-night value closes the
    // loop seamlessly, and it happens to be the true middle of the night:
    // five hours after the 19:00 dusk anchor and five hours before the
    // 05:00 dawn anchor.
    private static readonly (double Tick, Color Color)[] Anchors = new[]
    {
        (0.000, NightDeep),     // 00:00 deepest night, closes the loop
        (0.208, NightEdge),     // 05:00 night thinned out, dawn about to start
        (0.250, Sunrise),       // 06:00 sunrise peak
        (0.300, MorningCool),   // 07:12 warm gone, cool morning left
        (0.500, NoonClear),     // 12:00 pure white, no-op
        (0.700, AfternoonWarm), // 16:48 faint gold, dusk about to start
        (0.750, Sunset),        // 18:00 sunset peak
        (0.792, NightEdge),     // 19:00 night again
        (1.000, NightDeep),     // 24:00 deepest night
    };

    /// <summary>
    /// Returns the light colour at the given fraction of an in-game day,
    /// to be multiplied into the world. Alpha is always 1: strength lives
    /// in how far the channels fall below white, never in transparency.
    /// Fractions outside [0, 1) wrap modulo 1 so a negative tick or a tick
    /// past one day resolves to the same colour as the equivalent in-range
    /// tick.
    /// </summary>
    public static Color ForFraction(double fraction)
    {
        double wrapped = fraction - System.Math.Floor(fraction);
        for (int index = 0; index < Anchors.Length - 1; index++)
        {
            (double Tick, Color Color) lower = Anchors[index];
            (double Tick, Color Color) upper = Anchors[index + 1];
            if (wrapped < lower.Tick || wrapped > upper.Tick) continue;
            double span = upper.Tick - lower.Tick;
            double t = span > 0 ? (wrapped - lower.Tick) / span : 0.0;
            return lower.Color.Lerp(upper.Color, (float)SmoothStep(t));
        }
        // Anchors[0] handles the wrap at 0.0; defensive fallback if the
        // array ever loses its first/last entries.
        return Anchors[0].Color;
    }

    /// <summary>
    /// Hermite ease (3t² − 2t³) over [0, 1]. Endpoints are preserved
    /// exactly, so the anchors still hold their declared colours; only
    /// the pacing between them changes.
    /// </summary>
    private static double SmoothStep(double t)
    {
        if (t <= 0.0) return 0.0;
        if (t >= 1.0) return 1.0;
        return t * t * (3.0 - (2.0 * t));
    }
}
