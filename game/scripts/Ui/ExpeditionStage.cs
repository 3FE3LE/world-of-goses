#nullable enable
using System;
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Static lateral expedition stage. It draws structural placeholders only:
/// no movement, hitboxes, damage, clocks or combat resolution.
/// </summary>
public partial class ExpeditionStage : Control
{
    private int _partyCount;
    private int _enemyCount;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
        QueueRedraw();
    }

    public void Configure(int partyCount, int enemyCount)
    {
        _partyCount = Math.Clamp(partyCount, 0, 4);
        _enemyCount = Math.Clamp(enemyCount, 0, 4);
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2I logicalSize = new(
            Mathf.RoundToInt(Size.X),
            Mathf.RoundToInt(Size.Y));
        if (logicalSize.X <= 0 || logicalSize.Y <= 0) return;

        Color sky = GetThemeColor("fill_empty");
        Color distance = GetThemeColor("fill_cooldown");
        Color ground = GetThemeColor("border_disabled");
        Color outline = GetThemeColor("border_locked");
        Color accent = LineageThemeRegistry.IconAccent;
        Color danger = GetThemeColor("font_color", "HudButtonDanger");

        DrawRect(new Rect2I(0, 0, logicalSize.X, logicalSize.Y), sky);
        int horizon = Mathf.RoundToInt(logicalSize.Y * 0.56f);
        DrawRect(new Rect2I(0, horizon - 48, logicalSize.X, 48), distance);
        DrawRect(new Rect2I(0, horizon, logicalSize.X, logicalSize.Y - horizon), ground);

        DrawLandscapeSilhouette(logicalSize, horizon, outline);
        for (int i = 0; i < _partyCount; i++)
        {
            DrawCombatant(new Vector2I(104 + i * 44, horizon - 34), accent, facesRight: true);
        }
        for (int i = 0; i < _enemyCount; i++)
        {
            DrawCombatant(
                new Vector2I(logicalSize.X - 120 - i * 44, horizon - 34),
                danger,
                facesRight: false);
        }
    }

    private void DrawLandscapeSilhouette(Vector2I logicalSize, int horizon, Color color)
    {
        for (int x = 20; x < logicalSize.X; x += 112)
        {
            int height = 10 + (x / 112 % 3) * 6;
            DrawRect(new Rect2I(x, horizon - height, 32, height), color.Darkened(0.25f));
            DrawRect(new Rect2I(x + 8, horizon - height - 6, 16, 6), color.Darkened(0.25f));
        }
        DrawLine(
            new Vector2I(0, horizon),
            new Vector2I(logicalSize.X, horizon),
            color,
            width: 2,
            antialiased: false);
    }

    private void DrawCombatant(Vector2I origin, Color color, bool facesRight)
    {
        DrawRect(new Rect2I(origin.X + 8, origin.Y, 12, 12), color);
        DrawRect(new Rect2I(origin.X + 6, origin.Y + 14, 16, 18), color);
        DrawRect(new Rect2I(origin.X + 2, origin.Y + 32, 8, 4), color);
        DrawRect(new Rect2I(origin.X + 18, origin.Y + 32, 8, 4), color);
        int facingOffset = facesRight ? 22 : 0;
        DrawRect(new Rect2I(origin.X + facingOffset, origin.Y + 16, 6, 4), color);
    }
}
