#nullable enable

using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Renders every compact-HUD primitive, and every state each one can take, on a
/// single screen at the two official review sizes.
/// </summary>
/// <remarks>
/// <para>
/// Separate from <c>ComponentShowcase</c> rather than folded into it. That scene
/// reviews the <em>screen</em> primitives — 18 px body text, 40 px actions, 6 px
/// panel frames — and this one reviews a deliberately different scale. Putting both
/// on one surface would leave neither enough width, and would invite the reader to
/// compare two systems that are not meant to agree.
/// </para>
/// <para>
/// <b>Three of the six states here are specimens, not live controls.</b> A static
/// capture can hold exactly one focused control, and hover needs a pointer the
/// harness is not holding at capture time. So <c>default</c>, <c>disabled</c>,
/// <c>selected</c> and one <c>focus</c> are real controls in their real state, and
/// <c>hover</c> and <c>pressed</c> are drawn by applying that state's stylebox as a
/// control's normal style. A specimen proves the stylebox renders; it does not prove
/// the control reaches that state. Anything depending on the transition still needs a
/// real pointer, which is what <c>VISUAL_REGRESSION.md</c>'s click-driven rows are for.
/// </para>
/// <para>
/// The first column's tier block is load-bearing. The reference builds its whole
/// hierarchy out of one border weight and three fills that sit within twelve
/// luminance steps of each other, so the thing most likely to break here is not a
/// component but the <em>separation between surfaces</em> — and it breaks invisibly,
/// by a fill drifting two steps until a card stops lifting off its panel. Both
/// readings, nested and unnested, are on screen for that reason.
/// </para>
/// <para>
/// This scene also settled the border question. It first drew three weights side by
/// side — 1 px from the composited <c>tile_0069</c> outline, 3 px from
/// <c>tile_0019</c>, 4 px from <c>tile_0018</c> — and at 1920x1080 the two heavier
/// frames lost plainly: <c>tile_0019</c>'s corner studs read as artefacts and
/// <c>tile_0018</c> doubled its own edge. Every HUD surface now shares the one-pixel
/// frame, and the losing tiles were never promoted.
/// </para>
/// <para>
/// A developer surface, like <c>TypographySpecimen</c>. Its strings are literals
/// rather than <c>UiText</c> keys because nothing here is player-facing, and adding
/// two dozen catalogue entries for a review scene would be noise in both `.po` files.
/// </para>
/// </remarks>
public partial class HudComponentShowcase : Control
{
    public override void _Ready()
    {
        var safeArea = new SafeAreaMarginContainer();
        safeArea.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(safeArea);

        var columns = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        columns.AddThemeConstantOverride("separation", Tokens.SpacingComfortable);
        safeArea.AddChild(columns);

        columns.AddChild(BuildSurfacesColumn());
        columns.AddChild(BuildControlsColumn());
        columns.AddChild(BuildDataColumn());
    }

    private static VBoxContainer NewColumn(string heading)
    {
        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        column.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        column.AddChild(new Label { Text = heading, ThemeTypeVariation = "HudBrand" });
        return column;
    }

    private static Label Caption(string text) => new()
    {
        Text = text,
        ThemeTypeVariation = "HudCaption",
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
    };

    private static PanelContainer Surface(string variation, Control content)
    {
        var surface = new PanelContainer { ThemeTypeVariation = variation };
        surface.AddChild(content);
        return surface;
    }

    private static VBoxContainer Stack(int separation = Tokens.SpacingTight)
    {
        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", separation);
        return stack;
    }

    /// <summary>A one-line body of text, ready to drop into a surface.</summary>
    private static VBoxContainer Line(string text, string variation = "HudBody")
    {
        var stack = Stack();
        stack.AddChild(new Label { Text = text, ThemeTypeVariation = variation });
        return stack;
    }

    /// <summary>
    /// The three nested surface tiers, and the border-weight comparison the hybrid
    /// chrome decision rests on.
    /// </summary>
    private static Control BuildSurfacesColumn()
    {
        var column = NewColumn("Surfaces");

        // Tiers, actually nested: the reference separates them by only ~4 and ~9
        // luminance steps, so they have to be seen inside one another to be judged.
        var body = Stack(Tokens.SpacingBase);
        body.AddChild(new HudSectionHeader("ESTADO", "3"));
        body.AddChild(Surface("HudInset", Line("HudInset — recessed")));
        body.AddChild(Surface("HudCard", Line("HudCard — raised")));
        body.AddChild(Surface("HudDock", Line("HudDock")));
        column.AddChild(Surface("HudSurface", body));

        // The tiers again, this time side by side rather than nested. Separated by
        // only 4 and 8 luminance steps, they have to survive both readings: a card
        // must lift off the panel it sits in *and* be tellable from a panel it does
        // not touch.
        column.AddChild(new HudSectionHeader("TIERS, UNNESTED"));
        foreach ((string variation, string label) in new[]
                 {
                     ("HudInset", "HudInset — fill lum 8"),
                     ("HudSurface", "HudSurface — fill lum 12"),
                     ("HudCard", "HudCard — fill lum 20"),
                 })
        {
            column.AddChild(Surface(variation, Line(label, "HudCaption")));
        }

        column.AddChild(Caption(
            "Every frame here is one pixel, from the same Kenney tile_0069 outline. "
            + "The tiers differ by fill alone, which is what the reference does."));

        return column;
    }

    /// <summary>Every interactive primitive, in every state it can take.</summary>
    private static Control BuildControlsColumn()
    {
        var column = NewColumn("Controls");

        column.AddChild(new HudSectionHeader("HUDBUTTON", "6 states"));

        var normal = new Button { Text = "Default", ThemeTypeVariation = "HudButton" };
        normal.CustomMinimumSize = new Vector2(0, Tokens.HudControlHeight);
        var focused = new Button { Text = "Focus", ThemeTypeVariation = "HudButton" };
        focused.CustomMinimumSize = new Vector2(0, Tokens.HudControlHeight);
        focused.FocusMode = FocusModeEnum.All;
        var disabled = new Button
        {
            Text = "Disabled",
            ThemeTypeVariation = "HudButton",
            Disabled = true,
            CustomMinimumSize = new Vector2(0, Tokens.HudControlHeight),
        };

        column.AddChild(normal);
        column.AddChild(focused);
        column.AddChild(Specimen("Hover (specimen)", "HudButton", "hud_button_hover"));
        column.AddChild(Specimen("Pressed (specimen)", "HudButton", "hud_button_pressed"));
        column.AddChild(disabled);

        var selected = new Button
        {
            Text = "Selected",
            ThemeTypeVariation = "HudButtonSelected",
            CustomMinimumSize = new Vector2(0, Tokens.HudControlHeight),
        };
        column.AddChild(selected);

        var danger = new Button
        {
            Text = "[!] Cancel — warning",
            ThemeTypeVariation = "HudButtonDanger",
            CustomMinimumSize = new Vector2(0, Tokens.HudControlHeight),
        };
        column.AddChild(danger);

        column.AddChild(new HudSectionHeader("COLLAPSIBLE"));
        column.AddChild(new CollapsiblePanelHeader("CIUDAD", expanded: true));
        column.AddChild(new CollapsiblePanelHeader("REGISTRO", expanded: false));
        column.AddChild(Caption(
            "Expanded and collapsed differ by chevron, not by tint alone."));

        column.AddChild(new HudSectionHeader("BADGE"));
        var badgeRow = new HBoxContainer();
        badgeRow.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        badgeRow.AddChild(new HudBadge("12"));
        badgeRow.AddChild(new HudBadge("3"));
        badgeRow.AddChild(new Label
        {
            Text = "the only amber surface",
            ThemeTypeVariation = "HudCaption",
            VerticalAlignment = VerticalAlignment.Center,
        });
        column.AddChild(badgeRow);

        // State badge: every phase of an expedition, glyph + label.
        // Each row uses the same authoring path the compact card uses,
        // so a future expedition screen can compose the same chip without
        // a second design system.
        column.AddChild(new HudSectionHeader("STATE BADGE", "6"));
        var stateBadgeStack = Stack(Tokens.SpacingTight);
        foreach (ExpeditionPhase phase in new[]
                 {
                     ExpeditionPhase.Outbound,
                     ExpeditionPhase.Encounter,
                     ExpeditionPhase.Objective,
                     ExpeditionPhase.Returning,
                     ExpeditionPhase.Retreating,
                     ExpeditionPhase.Resolved,
                 })
        {
            stateBadgeStack.AddChild(new HudStateBadge(
                HudStateBadge.IconFor(phase),
                PhaseLabel(phase)));
        }
        column.AddChild(stateBadgeStack);
        column.AddChild(Caption(
            "Phase carried by a glyph plus a localized word — colour is "
            + "never the only signal."));

        // The sixth state. Unlike the other five it is not a stylebox a control
        // switches into — every primitive expresses it differently, and each one has
        // to prove it does so without relying on colour.
        column.AddChild(new HudSectionHeader("WARNING", "[!]"));
        var warned = Stack();
        warned.AddChild(new HudMetricRow("Almacen", "lleno", IconPaths.Warning));
        warned.AddChild(new HudResourceRow(IconPaths.Leaf, "Alimentos", "12", "-9"));
        warned.AddChild(new HudProgressBar(0.94, showPercent: true));
        column.AddChild(Surface("HudSurface", warned));
        column.AddChild(Caption(
            "Warning is a glyph, a sign and a number — never a tint. Desaturate this "
            + "block and every one of the three still reads."));

        // Focus is a real state and the only one a screenshot cannot provoke by
        // hovering, so grab it deliberately.
        focused.CallDeferred(Control.MethodName.GrabFocus);

        return column;
    }

    /// <summary>
    /// Draws one state's stylebox as a control's normal style. Honest about what it
    /// is: the label says "specimen" so a reader never mistakes it for a live state.
    /// </summary>
    private static Button Specimen(string label, string variation, string styleboxName)
    {
        var button = new Button
        {
            Text = label,
            ThemeTypeVariation = variation,
            CustomMinimumSize = new Vector2(0, Tokens.HudControlHeight),
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        var stylebox = ResourceLoader.Load<StyleBox>(
            $"res://assets/ui/composites/{styleboxName}.tres");
        if (stylebox is not null) button.AddThemeStyleboxOverride("normal", stylebox);
        return button;
    }

    /// <summary>
    /// Localized label for a phase, mirroring <see cref="ExpeditionCompactCard.PhaseText"/>.
    /// The showcase intentionally uses literals to keep the developer
    /// surface free of <see cref="UiText"/> keys.
    /// </summary>
    private static string PhaseLabel(ExpeditionPhase phase) => phase switch
    {
        ExpeditionPhase.Outbound => "Outbound",
        ExpeditionPhase.Encounter => "Encounter",
        ExpeditionPhase.Objective => "At objective",
        ExpeditionPhase.Returning => "Returning",
        ExpeditionPhase.Retreating => "Retreating",
        _ => "Resolved",
    };

    /// <summary>
    /// Every <see cref="ResourceType"/> in canonical priority order, with the
    /// silhouette next to a small quantity and a large quantity. The two
    /// amount columns exercise both the natural and the compact
    /// (<see cref="CompactNumber"/>) presentations so a reviewer can confirm
    /// the formatter triggers at the documented thresholds (1.0K, 1.0M).
    /// </summary>
    private static Control BuildResourceCatalog()
    {
        var stack = Stack(Tokens.SpacingTight);

        // Header row so the columns are unambiguous in the captured frame.
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        header.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(BuildCatalogHeaderCell("ICON", Tokens.IconInline + Tokens.SpacingBase));
        header.AddChild(BuildCatalogHeaderCell("NAME", 0, expand: true));
        header.AddChild(BuildCatalogHeaderCell("SMALL", 56));
        header.AddChild(BuildCatalogHeaderCell("LARGE", 64));
        stack.AddChild(header);

        foreach (ResourceType resource in ResourcePriority.Sequence)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", Tokens.SpacingBase);
            row.MouseFilter = MouseFilterEnum.Ignore;
            // Reserve the 24 px icon cell so every glyph sits in the same
            // 24 px column — the silhouettes are comparable at a glance.
            var iconCell = new MarginContainer
            {
                CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.HudRowHeight),
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            iconCell.AddThemeConstantOverride("margin_top", 1);
            var icon = new ResourceIcon(resource)
            {
                CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.IconInline),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            iconCell.AddChild(icon);
            row.AddChild(iconCell);

            row.AddChild(new Label
            {
                Text = resource.ToString(),
                ThemeTypeVariation = "HudBody",
                VerticalAlignment = VerticalAlignment.Center,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            });
            row.AddChild(new Label
            {
                Text = CompactNumber.Format(CatalogSmallAmount(resource)),
                ThemeTypeVariation = "HudNumeric",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                CustomMinimumSize = new Vector2(56, 0),
                MouseFilter = MouseFilterEnum.Ignore,
            });
            row.AddChild(new Label
            {
                Text = CompactNumber.Format(CatalogLargeAmount(resource)),
                ThemeTypeVariation = "HudNumeric",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                CustomMinimumSize = new Vector2(64, 0),
                MouseFilter = MouseFilterEnum.Ignore,
            });
            stack.AddChild(row);
        }
        return stack;
    }

    private static Control BuildCatalogHeaderCell(
        string text,
        float minWidth,
        bool expand = false)
    {
        var label = new Label
        {
            Text = text,
            ThemeTypeVariation = "HudCaption",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = expand
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Right,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        if (expand) label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        if (minWidth > 0) label.CustomMinimumSize = new Vector2(minWidth, 0);
        return label;
    }

    /// <summary>
    /// A small but distinctive per-resource value used by the catalog
    /// sample. Hard-coded so each row reads differently even though the
    /// formatter also sees a real <see cref="int"/>.
    /// </summary>
    private static int CatalogSmallAmount(ResourceType resource) => resource switch
    {
        ResourceType.Food => 28,
        ResourceType.WildFood => 12,
        ResourceType.Wood => 47,
        ResourceType.Stone => 19,
        ResourceType.Branches => 6,
        ResourceType.PlantFiber => 9,
        ResourceType.SmallStone => 14,
        ResourceType.Iron => 3,
        ResourceType.Potions => 2,
        _ => 1,
    };

    /// <summary>
    /// A large value chosen so the compact formatter triggers on at least
    /// one of the K / M thresholds, letting the reviewer confirm the
    /// formatter in a single pass.
    /// </summary>
    private static int CatalogLargeAmount(ResourceType resource) => resource switch
    {
        ResourceType.Food => 1_240,       // → 1.2K
        ResourceType.Wood => 18_400,      // → 18.4K
        ResourceType.Stone => 999,        // → 999 (natural)
        ResourceType.Branches => 4_560,   // → 4.6K
        ResourceType.PlantFiber => 870,   // → 870
        ResourceType.SmallStone => 3_200, // → 3.2K
        ResourceType.Iron => 1_120_000,   // → 1.1M
        ResourceType.Potions => 42,       // → 42
        ResourceType.WildFood => 96,      // → 96
        _ => 1,
    };

    /// <summary>The rows that carry values, plus the compact type scale itself.</summary>
    private static Control BuildDataColumn()
    {
        var column = NewColumn("Data");

        var city = Stack();
        city.AddChild(new HudSectionHeader("ESTADO"));
        city.AddChild(new HudMetricRow("Felicidad", "78%", IconPaths.Heart));
        city.AddChild(new HudMetricRow("Orden", "92%", IconPaths.Shield));
        city.AddChild(new HudMetricRow("Almacen", "lleno", IconPaths.Warning));
        city.AddChild(new HSeparator { ThemeTypeVariation = "HudSeparator" });
        city.AddChild(new HudMetricRow("Progreso", "42%"));
        city.AddChild(new HudProgressBar(0.42, showPercent: false));
        column.AddChild(Surface("HudSurface", city));

        // The full resource catalog: every ResourceType that can appear in
        // the icon-only HUD ticker, in canonical priority order, with the
        // exact silhouette alongside its compact and large-quantity
        // presentations. Silhouette collisions are visible at a glance:
        // Stone≠SmallStone, Food≠WildFood, Wood≠Branches, Iron≠Potions, and
        // so on. A reviewer should be able to confirm the unique
        // silhouettes in a single pass.
        column.AddChild(new HudSectionHeader("RESOURCE CATALOG", "9"));
        column.AddChild(Surface("HudSurface", BuildResourceCatalog()));
        column.AddChild(Caption(
            "Every ResourceType, in priority order. The first column is the "
            + "real silhouette, the second is a small-tick amount, the third "
            + "is a large amount that crosses into the compact K/M formatter."));

        var construction = Stack();
        construction.AddChild(new HudSectionHeader("CONSTRUCTION", "1"));
        construction.AddChild(new ConstructionQueueItem(new CityStatusSnapshot.ProjectItem(
            "Basic Shelter",
            Progress: 240,
            RequiredWork: 720,
            AssignedCount: 1,
            WorkerCapacity: 2,
            Enabled: true,
            StopCause: ConstructionStopCause.WorkersExhausted)));
        column.AddChild(Surface("HudSurface", construction));

        column.AddChild(new HudSectionHeader("EXPEDITION COMPACT CARD STATES"));
        column.AddChild(new ExpeditionCompactCard(new ExpeditionRailSnapshot.Item(
            new ExpeditionId(1),
            "Bosque Silente",
            ExpeditionPhase.Outbound,
            new[] { "Aster", "Lira" },
            ResourceType.Food,
            2,
            StartTick: 0,
            EndTick: GameClock.TicksPerInGameDay,
            CanCancel: true),
            currentTick: 0));
        column.AddChild(new ExpeditionCompactCard(new ExpeditionRailSnapshot.Item(
            new ExpeditionId(2),
            "Sendero del espíritu",
            ExpeditionPhase.Returning,
            new[] { "Aster" },
            ResourceType.Wood,
            1,
            StartTick: 0,
            EndTick: GameClock.TicksPerInGameDay,
            CanCancel: false),
            currentTick: GameClock.TicksPerInGameDay * 3 / 4));
        column.AddChild(new ExpeditionCompactCard(new ExpeditionRailSnapshot.Item(
            new ExpeditionId(3),
            "Paso de la niebla",
            ExpeditionPhase.Retreating,
            new[] { "Aster" },
            ResourceType.Wood,
            1,
            StartTick: 0,
            EndTick: GameClock.TicksPerInGameDay,
            CanCancel: false),
            currentTick: GameClock.TicksPerInGameDay / 2));
        column.AddChild(new ExpeditionCompactCard(new ExpeditionRailSnapshot.Item(
            new ExpeditionId(4),
            "Cantera distante",
            ExpeditionPhase.Resolved,
            new[] { "Aster" },
            ResourceType.Food,
            2,
            StartTick: 0,
            EndTick: GameClock.TicksPerInGameDay,
            CanCancel: false),
            currentTick: GameClock.TicksPerInGameDay));

        // The reuse proofs: five fixture-only compositions built from the
        // existing primitives. Each one is what a future dedicated
        // expedition screen might render without inventing a second
        // design system. Strings are literals — this is a developer
        // surface, not a player-facing catalog.
        column.AddChild(new HudSectionHeader("EXPEDITION REUSE PATTERNS"));
        column.AddChild(BuildExpeditionMemberCard());
        column.AddChild(BuildRouteNodeRow());
        column.AddChild(BuildDecisionOptionRow());
        column.AddChild(BuildBestiarySummaryCard());
        column.AddChild(BuildRewardItemCard());

        column.AddChild(new HudSectionHeader("TYPE SCALE"));
        var type = Stack();
        foreach ((string variation, string sample) in new[]
                 {
                     ("HudBrand", "HudBrand 20 — Geist Pixel"),
                     ("HudHeader", "HudHeader 18 — Jersey 10"),
                     ("HudLabel", "HudLabel 16 — Jersey 10"),
                     ("HudBody", "HudBody 16 — Pixelify Sans"),
                     ("HudNumeric", "HudNumeric 16 — 1320 · 42% · 8/10"),
                     ("HudCaption", "HudCaption 14 — Dia 17 · Aserradero completado"),
                 })
        {
            type.AddChild(new Label { Text = sample, ThemeTypeVariation = variation });
        }
        column.AddChild(Surface("HudInset", type));
        column.AddChild(Caption("14 px is unapproved until it is read here at 1280x720."));

        return column;
    }

    // ── Expedition reuse patterns ──────────────────────────────────────────
    //
    // Five fixture-only compositions built from the existing primitives.
    // Each one is what a future dedicated expedition screen might
    // render without inventing a second design system. Strings are
    // literals — this is a developer surface, not a player-facing
    // catalog.

    /// <summary>
    /// The hero / member card. A HudCard with a leading resource icon,
    /// the member name, and one HudMetricRow for role and one for state.
    /// Composed from HudCard + HudMetricRow + ResourceIcon.
    /// </summary>
    private static Control BuildExpeditionMemberCard()
    {
        var body = Stack(Tokens.SpacingTight);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        row.AddChild(new ResourceIcon(ResourceType.Food)
        {
            CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.IconInline),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        });
        row.AddChild(new Label
        {
            Text = "Aster",
            ThemeTypeVariation = "HudHeader",
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });
        row.AddChild(new HudStateBadge(
            IconPaths.Shield, "Leader"));
        body.AddChild(row);
        body.AddChild(new HudMetricRow("Rol", "exploradora"));
        body.AddChild(new HudMetricRow("Herida", "ninguna"));
        return Surface("HudCard", body);
    }

    /// <summary>
    /// A route / waypoint row. Target glyph + name + state badge + a
    /// progress bar. Composed from HBoxContainer + ResourceIcon +
    /// HudStateBadge + HudProgressBar.
    /// </summary>
    private static Control BuildRouteNodeRow()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        row.AddChild(new ResourceIcon(ResourceType.Wood)
        {
            CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.IconInline),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        });
        row.AddChild(new Label
        {
            Text = "Sendero del espíritu",
            ThemeTypeVariation = "HudBody",
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });
        row.AddChild(new HudStateBadge(
            HudStateBadge.IconFor(ExpeditionPhase.Objective),
            PhaseLabel(ExpeditionPhase.Objective)));
        row.AddChild(new HudProgressBar(0.5, showPercent: true));
        return row;
    }

    /// <summary>
    /// A decision option. An IconButton with a selectable state, the
    /// same contract the planning panel's posture buttons already use.
    /// Composed from IconButton with HudButtonSelected. Proves that the
    /// existing button primitive is sufficient for 2–4 way choices — no
    /// bespoke DecisionTray widget is needed for the showcase.
    /// </summary>
    private static Control BuildDecisionOptionRow()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        var confirm = new IconButton
        {
            IconPath = IconPaths.Check,
            ButtonText = "Confirmar",
            ShowLabel = true,
            ThemeTypeVariation = "HudButtonSelected",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        var alt = new IconButton
        {
            IconPath = IconPaths.Warning,
            ButtonText = "Retirada",
            ShowLabel = true,
            ThemeTypeVariation = "HudButton",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row.AddChild(confirm);
        row.AddChild(alt);
        return row;
    }

    /// <summary>
    /// A bestiary summary card. HudCard with an icon, a name, and two
    /// HudMetricRows (threat and stats). Composed from HudCard +
    /// ResourceIcon + HudMetricRow.
    /// </summary>
    private static Control BuildBestiarySummaryCard()
    {
        var body = Stack(Tokens.SpacingTight);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        row.AddChild(new ResourceIcon(ResourceType.Iron)
        {
            CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.IconInline),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        });
        row.AddChild(new Label
        {
            Text = "Jabalí del bosque",
            ThemeTypeVariation = "HudHeader",
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });
        body.AddChild(row);
        body.AddChild(new HudMetricRow("Amenaza", "media", IconPaths.Shield));
        body.AddChild(new HudMetricRow("Botín", "+12 alimento", IconPaths.Coin));
        return Surface("HudCard", body);
    }

    /// <summary>
    /// A reward item card. HudCard with a ResourceIcon, the amount, and
    /// a label. Composed from HudCard + ResourceIcon + HudMetricRow.
    /// </summary>
    private static Control BuildRewardItemCard()
    {
        var body = Stack(Tokens.SpacingTight);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        row.AddChild(new ResourceIcon(ResourceType.Stone)
        {
            CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.IconInline),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        });
        row.AddChild(new Label
        {
            Text = "Piedra",
            ThemeTypeVariation = "HudBody",
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });
        body.AddChild(row);
        body.AddChild(new HudMetricRow("Cantidad", "+18"));
        return Surface("HudCard", body);
    }
}
