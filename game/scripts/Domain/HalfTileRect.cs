using System;

namespace WorldofGoses.Domain;

/// <summary>
/// Integer rectangle measured in half-tiles. Keeping half-tile precision in
/// domain integers avoids floating-point placement and remains independent
/// from the rendered tile size.
/// </summary>
public readonly record struct HalfTileRect(int X, int Y, int Width, int Height)
{
    public int Right => checked(X + Width);
    public int Bottom => checked(Y + Height);

    public HalfTileRect ValidatePositive()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(X);
        ArgumentOutOfRangeException.ThrowIfNegative(Y);
        if (Width <= 0) throw new ArgumentOutOfRangeException(nameof(Width));
        if (Height <= 0) throw new ArgumentOutOfRangeException(nameof(Height));
        return this;
    }

    public bool Contains(HalfTileRect other) =>
        other.X >= X
        && other.Y >= Y
        && other.Right <= Right
        && other.Bottom <= Bottom;

    public bool Intersects(HalfTileRect other) =>
        X < other.Right
        && Right > other.X
        && Y < other.Bottom
        && Bottom > other.Y;
}
