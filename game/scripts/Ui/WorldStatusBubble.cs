#nullable enable
using System.Collections.Generic;
using System.Text;
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// One reusable, pointer-driven world tooltip. It is mounted above the macro
/// world and rebuilt only when the hovered subject changes, so contextual
/// citizen information does not become permanent map clutter.
/// </summary>
public partial class WorldStatusBubble : PanelContainer
{
    private const float ViewportMargin = 8f;
    private const float AnchorGap = 8f;
    private const float HudPanelGap = 16f;
    private readonly VBoxContainer _content = new();
    private string _contentSignature = string.Empty;

    public readonly record struct Item(string IconPath, string Text);

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        // The bubble is mounted below MacroStreetLiveView, whose negative
        // depth range keeps world geometry under the HUD. Opt out of relative
        // z so the semantic world-dialogue layer is explicit and cannot rise
        // over CitySummaryPanel or ExpeditionRail when parent depth changes.
        ZAsRelative = false;
        ZIndex = OverlayLayers.WorldDialogue;
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.05f, 0.04f, 0.94f),
            BorderColor = new Color(0.96f, 0.93f, 0.86f, 1f),
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
        };
        AddThemeStyleboxOverride("panel", panelStyle);
        // The VBoxContainer below is the bubble's body. Without Ignore the
        // PanelContainer's default MouseFilter = Stop on the child would let
        // the bubble's own panel swallow the cursor and trip IsPointerOwnedByUi,
        // which immediately calls ClearWorldStatusHover from the next _Process
        // tick — the bubble would only stay visible on the exact frame the
        // cursor entered the citizen, then disappear. The bubble is a tooltip
        // overlay, never a hit target.
        _content.MouseFilter = MouseFilterEnum.Ignore;
        _content.AddThemeConstantOverride("separation", 3);
        AddChild(_content);
        Hide();
    }

    public void ShowAt(Vector2 globalAnchor, string title, IReadOnlyList<Item> items)
    {
        string signature = BuildContentSignature(title, items);
        if (_contentSignature != signature)
        {
            _contentSignature = signature;
            foreach (Node child in _content.GetChildren())
            {
                _content.RemoveChild(child);
                child.QueueFree();
            }

            _content.AddChild(new Label
            {
                Text = title,
                ThemeTypeVariation = "SectionTitle",
                MouseFilter = MouseFilterEnum.Ignore,
            });

            foreach (Item item in items)
            {
                var row = new HBoxContainer
                {
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                row.AddThemeConstantOverride("separation", 6);
                row.AddChild(new TextureRect
                {
                    Texture = ResourceLoader.Load<Texture2D>(item.IconPath),
                    CustomMinimumSize = new Vector2(24, 24),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    MouseFilter = MouseFilterEnum.Ignore,
                });
                row.AddChild(new Label
                {
                    Text = item.Text,
                    ThemeTypeVariation = "TooltipText",
                    VerticalAlignment = VerticalAlignment.Center,
                    MouseFilter = MouseFilterEnum.Ignore,
                });
                _content.AddChild(row);
            }
        }

        ResetSize();
        Vector2 minimum = GetCombinedMinimumSize().Ceil();
        Size = minimum;
        Vector2 viewportSize = GetViewportRect().Size;
        Vector2 desired = new(
            globalAnchor.X - minimum.X * 0.5f,
            globalAnchor.Y - minimum.Y - AnchorGap);
        float worldLeft = CitySummaryPanel.PanelWidth + HudPanelGap;
        float worldRight = viewportSize.X - ExpeditionRail.PanelWidth - HudPanelGap;
        desired.X = Mathf.Clamp(
            desired.X,
            worldLeft,
            Mathf.Max(worldLeft, worldRight - minimum.X));
        desired.Y = Mathf.Clamp(
            desired.Y,
            ViewportMargin,
            Mathf.Max(ViewportMargin, viewportSize.Y - minimum.Y - ViewportMargin));
        GlobalPosition = desired.Round();
        Show();
    }

    private static string BuildContentSignature(string title, IReadOnlyList<Item> items)
    {
        var signature = new StringBuilder(title);
        foreach (Item item in items)
        {
            signature.Append('\u001f').Append(item.IconPath).Append('\u001f').Append(item.Text);
        }
        return signature.ToString();
    }
}
