#nullable enable

using System.Globalization;

namespace WorldofGoses.Ui;

/// <summary>
/// Compact presentation of large integers for the resource ticker, the city
/// summary rows, and any other HUD surface that may outgrow a few digits.
/// </summary>
/// <remarks>
/// <para>
/// Values render as their natural number up to 999, then collapse to a single
/// fixed-decimal suffix: <c>1,200 → "1.2K"</c>, <c>18,400 → "18.4K"</c>,
/// <c>1,100,000 → "1.1M"</c>. Tooltips keep the exact amount because the
/// player must still read a precise total without hovering every row.
/// </para>
/// <para>
/// Domain numeric types stay <c>int</c> — this helper only formats the
/// display string. <see cref="CultureInfo.InvariantCulture"/> keeps the
/// decimal separator a period regardless of locale; resource amounts are a
/// fixed-form count, not a localised number.
/// </para>
/// </remarks>
public static class CompactNumber
{
    private const int Thousand = 1_000;
    private const int Million = 1_000_000;

    /// <summary>
    /// Returns the value formatted with at most one decimal place and a
    /// <c>K</c>/<c>M</c> suffix once it crosses a thousand.
    /// </summary>
    public static string Format(int value)
    {
        if (value < 0) return "-" + Format(-value);
        if (value < Thousand) return value.ToString(CultureInfo.InvariantCulture);
        if (value < Million)
        {
            double thousands = value / (double)Thousand;
            // A value that rounds to 1000K (e.g. 999,999 → 999.999) reads
            // better as a million than as "1000K". Promote it.
            double rounded = System.Math.Round(thousands, 1);
            if (rounded >= Thousand)
            {
                return FormatSingleDecimal(value / (double)Million) + "M";
            }
            return FormatSingleDecimal(thousands) + "K";
        }
        double millions = value / (double)Million;
        return FormatSingleDecimal(millions) + "M";
    }

    /// <summary>
    /// Exact rendering of an integer with a thousands separator, used by
    /// tooltips that must stay truthful even when the chip shows the compact
    /// form. <c>1,200</c> not <c>1200</c>, <c>1,100,000</c> not
    /// <c>1100000</c>.
    /// </summary>
    public static string FormatExact(int value) =>
        value.ToString("N0", CultureInfo.InvariantCulture);

    private static string FormatSingleDecimal(double value)
    {
        double rounded = System.Math.Round(value, 1);
        if (rounded == System.Math.Floor(rounded))
        {
            return ((int)rounded).ToString(CultureInfo.InvariantCulture);
        }
        return rounded.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
