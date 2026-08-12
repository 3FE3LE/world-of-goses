namespace WorldofGoses;

/// <summary>
/// Centralised catalog of icon paths used throughout the UI. Every
/// path resolves to a PNG/SVG under <c>res://assets/ui/icons/24/</c>
/// that has been imported into the Godot project. UI code never
/// hand-writes paths; this is the single point where a renamed or
/// re-imported icon needs to be updated.
///
/// Pixelarticons ships under the MIT license; the SVGs were copied
/// verbatim into the project. Project-owned icons (resources,
/// buildings, linajes) live under <see cref="BuildingArt"/>.
/// </summary>
public static class IconPaths
{
    private const string Root = "res://assets/ui/icons/24/";

    // Navigation and universal actions.
    public const string ArrowLeft = Root + "arrow-left.svg";
    public const string ChevronDown = Root + "chevron-down.svg";
    public const string ChevronUp = Root + "chevron-up.svg";
    public const string Close = Root + "close.svg";
    public const string Menu = Root + "menu.svg";
    public const string Backpack = Root + "backpack.svg";
    public const string ClipboardNote = Root + "clipboard-note.svg";
    public const string Reload = Root + "reload.svg";
    public const string Trash = Root + "trash.svg";

    // Decisions and constructive actions.
    public const string Check = Root + "check.svg";
    public const string Play = Root + "play.svg";
    public const string Pause = Root + "pause.svg";
    public const string Plus = Root + "plus.svg";
    public const string Minus = Root + "minus.svg";
    public const string Expand = Root + "expand.svg";

    /// <summary>
    /// The three-state speedometer for the simulation-speed control, promoted
    /// from <c>art/Pixelarticons/svg/speed-{slow,medium,fast}.svg</c> on
    /// 2026-08-12 to close GitHub #16.
    ///
    /// <para>They exist because the previous control stacked one, two or four
    /// copies of <see cref="Play"/> in 8 px cells. These icons are 24 px and
    /// <c>TextureRect.StretchMode.Keep</c> draws a source at its own size
    /// regardless of the rect, so the cells never shrank the glyphs — they
    /// overflowed a 36 px button. One glyph per state fits its cell natively,
    /// which is what <see cref="Tokens.IconInline"/> has always documented.</para>
    ///
    /// <para>The names are the upstream ones and describe the three steps
    /// relative to each other, not an absolute pace: the control cycles
    /// 1× → 2× → 4×, and there is no slower-than-normal speed to confuse them
    /// with. <c>DEC</c>-nothing: the world always runs.</para>
    /// </summary>
    public const string SpeedSlow = Root + "speed-slow.svg";
    public const string SpeedMedium = Root + "speed-medium.svg";
    public const string SpeedFast = Root + "speed-fast.svg";

    // State and context.
    public const string Info = Root + "info.svg";
    public const string Warning = Root + "warning.svg";
    public const string Shield = Root + "shield.svg";
    public const string Heart = Root + "heart.svg";
    public const string Leaf = Root + "leaf.svg";
    public const string Clock = Root + "clock.svg";
    public const string Calendar = Root + "calendar.svg";
    public const string Sun = Root + "sun.svg";
    public const string Moon = Root + "moon.svg";
    /// <summary>Fire spirit / campfire icon. Promoted from
    /// <c>art/Pixelarticons/svg/fire.svg</c> on 2026-08-06 to close the
    /// M-22 trigger for the first-night spirit placeholder
    /// (see <c>docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md</c>).</summary>
    public const string Fire = Root + "fire.svg";
    public const string Cog = Root + "cog.svg";
    public const string Coin = Root + "coin.svg";

    // Characters and places.
    public const string User = Root + "user.svg";

    /// <summary>
    /// The citizen roster, as opposed to the single hero <see cref="User"/> stands
    /// for. Promoted 2026-08-07: the navigation rail showed the hero, the roster
    /// and the camera toggle with the same <c>user.svg</c>, so three unrelated
    /// actions were indistinguishable once the rail collapsed to icons.
    /// </summary>
    public const string Users = Root + "users.svg";

    /// <summary>The camera follow/free toggle.</summary>
    public const string Camera = Root + "camera.svg";

    public const string House = Root + "house.svg";
    public const string Building = Root + "building.svg";
    /// <summary>Alias for <see cref="Leaf"/> while the forest-slice
    /// reuses the leaf sprite. Will become tree.svg when art lands.</summary>
    public const string Tree = Leaf;
}
