#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// A single stretch of vertical space shared by several bodies, of which
/// exactly one is ever visible.
/// </summary>
/// <remarks>
/// <para>
/// The problem this exists to remove: two bodies that each want the same
/// column, expressed as two sibling <see cref="Control.SizeFlags.ExpandFill"/>
/// children of one <see cref="VBoxContainer"/>. Godot then has to divide a
/// height between two claimants whose minimums move as their contents fold,
/// and the loser silently collapses to a zero-height rect while its children
/// stay alive and unrendered. The rail hit exactly that: the quick-action
/// button kept its pixels because it sat in a <c>ShrinkBegin</c> sibling with
/// a natural minimum, while the card list — the one negotiable node — was
/// squeezed to nothing. The old fix was to invalidate and re-sort three
/// ancestors after every toggle, which is a way of arguing with the layout
/// engine instead of giving it one answer.
/// </para>
/// <para>
/// The answer here: only one body is ever <see cref="CanvasItem.Visible"/>, and
/// a Godot <see cref="Container"/> excludes invisible children from its
/// minimum-size calculation. So there is never a division to make. No
/// <c>QueueSort</c>, no <c>ResetSize</c>, no <c>UpdateMinimumSize</c>, no
/// deferred relayout: swapping visibility is itself the whole mechanism, and
/// the engine re-measures on its next pass because that is what it already
/// does when a child's visibility changes.
/// </para>
/// <para>
/// The host deliberately knows nothing about headers. Its callers keep the
/// affordance — a <see cref="CollapsiblePanelHeader"/>, a tab strip, anything
/// — outside the host and call <see cref="ShowOnly"/> in response, so the
/// headers stay visible while the bodies swap beneath them, and this class
/// stays reusable by any surface with the same shape.
/// </para>
/// </remarks>
[GlobalClass]
public partial class AccordionHost : VBoxContainer
{
    private readonly List<Control> _bodies = new();

    /// <summary>
    /// Raised after <see cref="CurrentBody"/> changes. Consumers rebuild
    /// whatever depends on which body is on screen — a focus chain, most
    /// obviously, since the hidden body's controls must leave it.
    /// </summary>
    public event Action? CurrentBodyChanged;

    /// <summary>The one visible body, or null when every body is folded.</summary>
    public Control? CurrentBody { get; private set; }

    /// <summary>Registered bodies, in registration order.</summary>
    public IReadOnlyList<Control> Bodies => _bodies;

    public AccordionHost()
    {
        Name = "BodyHost";
        // The single ExpandFill of its parent column. This is the whole
        // point: the host takes the leftover height once, and whichever
        // body is visible inherits it.
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        MouseFilter = MouseFilterEnum.Pass;
    }

    /// <summary>
    /// Adopts <paramref name="body"/> as one of the swappable bodies. The
    /// body is reparented into the host if it currently lives elsewhere, is
    /// forced to <see cref="Control.SizeFlags.ExpandFill"/> so it inherits the
    /// host's full rect when shown, and starts hidden — <see cref="ShowOnly"/>
    /// is what puts one on screen.
    /// </summary>
    public void Register(Control body)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (_bodies.Contains(body)) return;

        // Reparent rather than refuse: the bodies are built and owned by the
        // panels that fill them, so they usually arrive with a parent already.
        if (body.GetParent() is Node currentParent && currentParent != this)
        {
            currentParent.RemoveChild(body);
        }
        if (body.GetParent() != this) AddChild(body);

        body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.Visible = false;
        _bodies.Add(body);
    }

    /// <summary>
    /// Makes exactly one registered body visible and every other one hidden.
    /// Passing null folds them all, which leaves the host empty and lets the
    /// parent column collapse to its headers.
    /// </summary>
    /// <remarks>
    /// Idempotent, and safe to call from a header's own change notification:
    /// it only touches <see cref="CanvasItem.Visible"/>, so it cannot re-enter
    /// through a header event the way a header-driven implementation would.
    /// </remarks>
    public void ShowOnly(Control? body)
    {
        if (body is not null && !_bodies.Contains(body))
        {
            throw new ArgumentException(
                "Body is not registered with this host.",
                nameof(body));
        }

        bool changed = !ReferenceEquals(CurrentBody, body);
        foreach (Control candidate in _bodies)
        {
            candidate.Visible = ReferenceEquals(candidate, body);
        }
        CurrentBody = body;
        if (changed) CurrentBodyChanged?.Invoke();
    }

    /// <summary>
    /// Whether <paramref name="body"/> is the one currently on screen. Callers
    /// use this to keep a header's chevron honest without tracking their own
    /// copy of the state.
    /// </summary>
    public bool IsShowing(Control body) => ReferenceEquals(CurrentBody, body);
}
