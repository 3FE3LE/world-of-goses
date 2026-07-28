#nullable enable
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Side panel of the building detail view. Lists currently-assigned
/// workers and available citizens, exposing assign/unassign actions.
/// </summary>
public partial class AssignmentPanel : PanelContainer
{
    private const int MaxVisibleRows = 5;
    private const float EmptyListHeight = 28f;
    private const float CitizenRowHeight = 40f;
    [Signal] public delegate void AssignRequestedEventHandler(int citizenId);
    [Signal] public delegate void UnassignRequestedEventHandler(int citizenId);

    private static readonly PackedScene AssignmentRowScene =
        GD.Load<PackedScene>("res://scenes/Components/AssignmentRow.tscn");

    private VBoxContainer _root = null!;
    private Label _summary = null!;
    private VBoxContainer _assignedList = null!;
    private VBoxContainer _availableList = null!;
    private ScrollContainer _assignedScroll = null!;
    private ScrollContainer _availableScroll = null!;
    private LineageThemeSignals? _themeSignals;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(220, 0);

        _root = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
        AddChild(_root);
        AddThemeStyleboxOverride("panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));
        _themeSignals = GetNodeOrNull<LineageThemeSignals>("/root/LineageThemeSignals");
        if (_themeSignals is not null)
        {
            _themeSignals.LineageChanged += OnLineageChanged;
        }

        var header = new Label
        {
            Text = UiText.Get("ui.assignment.workers_title"),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        header.ThemeTypeVariation = "PanelTitle";
        _root.AddChild(header);

        _summary = new Label { Text = "" };
        _summary.ThemeTypeVariation = "BodySmall";
        _root.AddChild(_summary);

        _root.AddChild(new HSeparator());

        var assignedHeader = new Label { Text = UiText.Get("Assigned") };
        assignedHeader.ThemeTypeVariation = "SectionTitle";
        _root.AddChild(assignedHeader);
        _assignedList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _assignedScroll = BuildListScroll(_assignedList);
        _root.AddChild(_assignedScroll);

        _root.AddChild(new HSeparator());

        var availableHeader = new Label { Text = UiText.Get("Available") };
        availableHeader.ThemeTypeVariation = "SectionTitle";
        _root.AddChild(availableHeader);
        _availableList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _availableScroll = BuildListScroll(_availableList);
        _root.AddChild(_availableScroll);
    }

    private static ScrollContainer BuildListScroll(Control content)
    {
        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, EmptyListHeight),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
        scroll.AddChild(content);
        return scroll;
    }

    public override void _ExitTree()
    {
        if (_themeSignals is not null) _themeSignals.LineageChanged -= OnLineageChanged;
    }

    private void OnLineageChanged(string lineage) => AddThemeStyleboxOverride(
        "panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));

    public void Refresh(BuildingDetailSnapshot snapshot)
    {
        _summary.Text =
            UiText.Format("ui.assignment.assigned_count", snapshot.AssignedCount, snapshot.WorkerCapacity) + "\n" +
            UiText.Format("ui.assignment.visible_inside", snapshot.VisibleWorkerCount, snapshot.HiddenWorkerCount);

        PopulateAssigned(snapshot);
        PopulateAvailable(snapshot);
        SetNaturalListHeight(_assignedScroll, snapshot.AssignedCitizens.Count);
        SetNaturalListHeight(_availableScroll, snapshot.AvailableCitizens.Count);
    }

    private static void SetNaturalListHeight(ScrollContainer scroll, int rowCount)
    {
        int visibleRows = Mathf.Min(rowCount, MaxVisibleRows);
        float height = visibleRows == 0
            ? EmptyListHeight
            : visibleRows * CitizenRowHeight;
        scroll.CustomMinimumSize = new Vector2(0, height);
    }

    private void PopulateAssigned(BuildingDetailSnapshot snapshot)
    {
        foreach (var child in _assignedList.GetChildren())
        {
            _assignedList.RemoveChild(child);
            child.QueueFree();
        }

        if (snapshot.AssignedCount == 0)
        {
            var empty = new Label { Text = UiText.Get("ui.assignment.no_workers") };
            empty.ThemeTypeVariation = "BodySmall";
            _assignedList.AddChild(empty);
            return;
        }

        foreach (var citizen in snapshot.AssignedCitizens)
        {
            var row = InstantiateRow(
                citizen.Id.Value,
                citizen.Name,
                UiText.Get("Remove"),
                UiText.Format("ui.assignment.remove_from", snapshot.DisplayName));
            row.ActionRequested += id => EmitSignal(SignalName.UnassignRequested, id);
            _assignedList.AddChild(row);
        }
    }

    private void PopulateAvailable(BuildingDetailSnapshot snapshot)
    {
        foreach (var child in _availableList.GetChildren())
        {
            _availableList.RemoveChild(child);
            child.QueueFree();
        }

        if (snapshot.AvailableCitizens.Count == 0)
        {
            var empty = new Label { Text = UiText.Get("ui.assignment.no_free_citizens") };
            empty.ThemeTypeVariation = "BodySmall";
            _availableList.AddChild(empty);
            return;
        }

        foreach (var citizen in snapshot.AvailableCitizens)
        {
            bool canAssign = snapshot.AssignedCount < snapshot.WorkerCapacity;
            var row = InstantiateRow(
                citizen.Id.Value,
                citizen.Name,
                UiText.Get("Assign"),
                UiText.Format("ui.assignment.assign_to", snapshot.DisplayName),
                disabled: !canAssign);
            row.ActionRequested += id => EmitSignal(SignalName.AssignRequested, id);
            _availableList.AddChild(row);
        }
    }

    private static AssignmentRow InstantiateRow(
        int id,
        string name,
        string actionLabel,
        string actionTooltip,
        bool disabled = false)
    {
        var row = AssignmentRowScene.Instantiate<AssignmentRow>();
        row.Configure(id, name, actionLabel, actionTooltip, disabled);
        return row;
    }
}
