using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Locks the contract of <see cref="TimeOfDayColor"/>. The helper returns
/// the light colour the world is <b>multiplied</b> by, so strength lives
/// in how far the channels fall below white and alpha is always 1 — Godot's
/// Mul blend ignores source alpha. The curve is two-speed: one-hour dawn
/// and dusk bands that move fast, joined by long stretches that keep
/// drifting slowly rather than holding a constant colour, smoothstepped
/// everywhere. Any future "let's just shift the sunrise to 05:00" change
/// must land here, not in the runtime values of the filter node itself.
/// </summary>
public sealed class TimeOfDayColorTests
{
    private const double Tolerance = 0.0001f;

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.05)]
    [InlineData(0.125)]  // 03:00 — used to bleed warm; must now stay pure night
    [InlineData(0.208)]  // 05:00 — last tick of pure night
    [InlineData(0.95)]
    [InlineData(0.99)]
    public void Fraction_BeforeSunrise_IsNightBlue(double fraction)
    {
        Color color = TimeOfDayColor.ForFraction(fraction);
        Assert.True(color.B > color.R, $"Night should be blue-dominant at fraction {fraction}");
        // Night has to be visible on a busy macro view: a light colour
        // that barely dips below white would multiply to no visible
        // change at all.
        Assert.True(Luminance(color) <= 0.75f,
            $"Night must visibly darken the world, got luminance {Luminance(color)}");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.125)]  // 03:00
    [InlineData(0.229)]  // 05:30, mid-dawn
    [InlineData(0.5)]    // noon
    [InlineData(0.75)]   // 18:00, sunset peak
    [InlineData(0.95)]
    public void Fraction_AlwaysReturnsAnOpaqueMultiplier(double fraction)
    {
        // Godot's Mul blend takes the source colour and ignores its
        // alpha. Any anchor that encodes strength as transparency would
        // silently render at full strength instead.
        Color color = TimeOfDayColor.ForFraction(fraction);
        Assert.Equal(1f, color.A, Tolerance);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.125)]
    [InlineData(0.229)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(0.95)]
    public void Fraction_NeverBrightensTheWorld(double fraction)
    {
        // A multiplier above 1 would blow out the macro view's palette
        // instead of lighting it. The filter may only ever subtract.
        Color color = TimeOfDayColor.ForFraction(fraction);
        Assert.True(color.R <= 1f + Tolerance, $"R={color.R} exceeds white at {fraction}");
        Assert.True(color.G <= 1f + Tolerance, $"G={color.G} exceeds white at {fraction}");
        Assert.True(color.B <= 1f + Tolerance, $"B={color.B} exceeds white at {fraction}");
    }

    [Theory]
    [InlineData(0.229)]  // 05:30 — mid-transition, already 50% warm
    [InlineData(0.245)]  // 05:54 — almost peak sunrise
    [InlineData(0.275)]  // 06:36 — past peak, fading toward neutral
    public void Fraction_AroundSunrise_IsWarm(double fraction)
    {
        Color color = TimeOfDayColor.ForFraction(fraction);
        Assert.True(color.R >= color.B,
            $"Sunrise window should be warm (R>=B) at fraction {fraction}, got R={color.R} B={color.B}");
    }

    [Fact]
    public void Fraction_AtNoon_IsTheIdentityMultiplier()
    {
        // Pure white: multiplying by it leaves the macro view in its
        // canonical midday palette, bit for bit.
        Color color = TimeOfDayColor.ForFraction(0.5);
        Assert.Equal(1f, color.R, Tolerance);
        Assert.Equal(1f, color.G, Tolerance);
        Assert.Equal(1f, color.B, Tolerance);
        Assert.Equal(1f, color.A, Tolerance);
    }

    [Theory]
    [InlineData(0.55)]
    [InlineData(0.70)]
    public void Fraction_AroundSunset_IsWarm(double fraction)
    {
        Color color = TimeOfDayColor.ForFraction(fraction);
        Assert.True(color.R >= color.B,
            $"Sunset window should be warm (R>=B) at fraction {fraction}");
    }

    [Fact]
    public void Fraction_AfterSunset_IsNightBlue()
    {
        // The dusk band closes at 0.792 (19:00), so everything from
        // there to midnight is flat night blue.
        Color color = TimeOfDayColor.ForFraction(0.95);
        Assert.True(color.B > color.R, $"Night should be blue-dominant after sunset, got R={color.R} B={color.B}");
    }

    // The short inflection bands, by their anchor bounds. Kept as a
    // plain array so the rate comparison below can iterate it directly.
    private static readonly (double Start, double End)[] ShortBands = new[]
    {
        (0.208, 0.250),  // 05:00 → 06:00 dawn
        (0.250, 0.300),  // 06:00 → 07:12 sunrise fading out
        (0.700, 0.750),  // 16:48 → 18:00 dusk warming up
        (0.750, 0.792),  // 18:00 → 19:00 nightfall
    };

    [Theory]
    [InlineData(0.000, 0.208)]  // 00:00 → 05:00 night thinning toward dawn
    [InlineData(0.300, 0.500)]  // 07:12 → 12:00 morning haze clearing
    [InlineData(0.500, 0.700)]  // 12:00 → 16:48 afternoon gold gathering
    [InlineData(0.792, 1.000)]  // 19:00 → 24:00 night deepening
    public void Fraction_OnALongStretch_KeepsDrifting(double start, double end)
    {
        // A stretch that holds perfectly still for five hours and then
        // lurches reads as artificial as a single all-day ramp. Every
        // long stretch must actually move — the point of the curve is
        // that it moves *slowly* there, not that it stops.
        float travelled = Distance(
            TimeOfDayColor.ForFraction(start),
            TimeOfDayColor.ForFraction(end));
        Assert.True(travelled > 0.05f,
            $"Stretch {start}-{end} is effectively constant (travelled {travelled}).");
    }

    [Theory]
    [InlineData(0.000, 0.208)]
    [InlineData(0.300, 0.500)]
    [InlineData(0.500, 0.700)]
    [InlineData(0.792, 1.000)]
    public void Fraction_OnALongStretch_MovesFarSlowerThanEveryBand(double start, double end)
    {
        // The whole design rests on the contrast in *rate*, not on the
        // stretches being frozen. Dawn and dusk have to feel like events
        // against a background that is always quietly changing, so every
        // band must outpace every stretch by a wide margin.
        float stretchRate = RateOfChange(start, end);
        foreach ((double bandStart, double bandEnd) in ShortBands)
        {
            float bandRate = RateOfChange(bandStart, bandEnd);
            Assert.True(bandRate > stretchRate * 4f,
                $"Band {bandStart}-{bandEnd} ({bandRate}/day) must clearly outpace "
                + $"stretch {start}-{end} ({stretchRate}/day).");
        }
    }

    [Fact]
    public void Fraction_NightIsDeepestAtMidnight()
    {
        // The slow night drift has a direction: darkest in the small
        // hours, lighter as it approaches the dawn and dusk anchors.
        float midnight = Luminance(TimeOfDayColor.ForFraction(0.0));
        float afterDusk = Luminance(TimeOfDayColor.ForFraction(0.792));
        float beforeDawn = Luminance(TimeOfDayColor.ForFraction(0.208));
        Assert.True(midnight < afterDusk,
            $"Midnight ({midnight}) should be darker than dusk's edge ({afterDusk}).");
        Assert.True(midnight < beforeDawn,
            $"Midnight ({midnight}) should be darker than dawn's edge ({beforeDawn}).");
    }

    [Theory]
    [InlineData(0.0)]    // midnight, the strongest the filter ever gets
    [InlineData(0.125)]  // 03:00
    [InlineData(0.75)]   // 18:00, sunset peak
    public void Fraction_NeverCrushesTheWorldsContrast(double fraction)
    {
        // The complaint this model exists to answer: the map has to stay
        // legible at night. Under multiplication the surviving contrast
        // of a channel *is* its multiplier, and green carries most of the
        // luminance detail — so green is the channel that decides whether
        // buildings and terrain still read apart from each other.
        Color color = TimeOfDayColor.ForFraction(fraction);
        Assert.True(color.G >= 0.40f,
            $"Green multiplier {color.G} at {fraction} leaves too little contrast to read the map.");
    }

    private static float Luminance(Color color)
        => (0.2126f * color.R) + (0.7152f * color.G) + (0.0722f * color.B);

    [Theory]
    [InlineData(0.02)]
    [InlineData(0.06)]
    [InlineData(0.125)]  // 03:00 — the original complaint
    [InlineData(0.18)]
    [InlineData(0.85)]
    [InlineData(0.93)]
    public void Fraction_DuringTheNightDrift_NeverTurnsWarm(double fraction)
    {
        // The night stretch drifts, but only between blues. If a future
        // anchor edit lets warmth leak into the small hours, this is the
        // test that catches it.
        Color color = TimeOfDayColor.ForFraction(fraction);
        Assert.True(color.B > color.R + 0.15f,
            $"Night at {fraction} must stay clearly blue, got R={color.R} B={color.B}");
    }

    private static float RateOfChange(double start, double end)
        => Distance(
               TimeOfDayColor.ForFraction(start),
               TimeOfDayColor.ForFraction(end))
           / (float)(end - start);

    [Fact]
    public void Fraction_AcrossDawnBand_MovesFastestInTheMiddle()
    {
        // Smoothstep, not a straight line: the middle third of the
        // 05:00-06:00 band must cover more colour distance than the
        // opening third, otherwise the transition reads as mechanical.
        double start = 0.208;
        double end = 0.250;
        double third = (end - start) / 3.0;

        float opening = Distance(
            TimeOfDayColor.ForFraction(start),
            TimeOfDayColor.ForFraction(start + third));
        float middle = Distance(
            TimeOfDayColor.ForFraction(start + third),
            TimeOfDayColor.ForFraction(start + (2 * third)));

        Assert.True(middle > opening,
            $"Dawn should accelerate through its middle: opening={opening} middle={middle}");
    }

    [Fact]
    public void Fraction_AcrossDuskBand_MovesFastestInTheMiddle()
    {
        double start = 0.750;
        double end = 0.792;
        double third = (end - start) / 3.0;

        float opening = Distance(
            TimeOfDayColor.ForFraction(start),
            TimeOfDayColor.ForFraction(start + third));
        float middle = Distance(
            TimeOfDayColor.ForFraction(start + third),
            TimeOfDayColor.ForFraction(start + (2 * third)));

        Assert.True(middle > opening,
            $"Dusk should accelerate through its middle: opening={opening} middle={middle}");
    }

    private static float Distance(Color from, Color to)
        => System.Math.Abs(to.R - from.R)
         + System.Math.Abs(to.G - from.G)
         + System.Math.Abs(to.B - from.B)
         + System.Math.Abs(to.A - from.A);

    [Fact]
    public void Fraction_WrapsAroundAtBoundary()
    {
        // 0.999 and 0.001 should resolve to near-identical night
        // colours (the anchors close the loop at 0.0 / 1.0).
        Color atEnd = TimeOfDayColor.ForFraction(0.999);
        Color atStart = TimeOfDayColor.ForFraction(0.001);
        Assert.Equal(atEnd.R, atStart.R, 2);
        Assert.Equal(atEnd.G, atStart.G, 2);
        Assert.Equal(atEnd.B, atStart.B, 2);
    }

    [Fact]
    public void Fraction_NegativeWrapsToEquivalentPositive()
    {
        Color negative = TimeOfDayColor.ForFraction(-0.25);
        Color positive = TimeOfDayColor.ForFraction(0.75);
        Assert.Equal(positive.R, negative.R, 2);
        Assert.Equal(positive.G, negative.G, 2);
        Assert.Equal(positive.B, negative.B, 2);
    }

    [Fact]
    public void Fraction_HoursMatchExpectedAnchors()
    {
        // The day fraction helpers in GameClock map hour -> tick / 3600.
        // Sanity-check the anchors line up with the documented times.
        Assert.Equal(0.25, GameClock.DayFraction(900),  3);  // 06:00
        Assert.Equal(0.50, GameClock.DayFraction(1800), 3);  // 12:00
        Assert.Equal(0.75, GameClock.DayFraction(2700), 3);  // 18:00
    }
}
