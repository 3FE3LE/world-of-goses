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
    [Signal] public delegate void AssignRequestedEventHandler(int citizenId);
    [Signal] public delegate void UnassignRequestedEventHandler(int citizenId);

    private static readonly PackedScene AssignmentRowScene =
        GD.Load<PackedScene>("res://scenes/Components/AssignmentRow.tscn");

    private VBoxContainer _root = null!;
    private Label _summary = null!;
    private VBoxContainer _assignedList = null!;
    private VBoxContainer _availableList = null!;
    private LineageThemeSignals? _themeSignals;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(220, 0);

        _root = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
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
            Text = "Workers",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        header.ThemeTypeVariation = "PanelTitle";
        _root.AddChild(header);

        _summary = new Label { Text = "" };
        _summary.ThemeTypeVariation = "BodySmall";
        _root.AddChild(_summary);

        _root.AddChild(new HSeparator());

        var assignedHeader = new Label { Text = "Assigned" };
        assignedHeader.ThemeTypeVariation = "SectionTitle";
        _root.AddChild(assignedHeader);
        _assignedList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _root.AddChild(BuildListScroll(_assignedList, 88));

        _root.AddChild(new HSeparator());

        var availableHeader = new Label { Text = "Available" };
        availableHeader.ThemeTypeVariation = "SectionTitle";
        _root.AddChild(availableHeader);
        _availableList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _root.AddChild(BuildListScroll(_availableList, 132, expand: true));
    }

    private static ScrollContainer BuildListScroll(
        Control content,
        float minimumHeight,
        bool expand = false)
    {
        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, minimumHeight),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = expand ? SizeFlags.ExpandFill : SizeFlags.ShrinkBegin,
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
            $"Assigned: {snapshot.AssignedCount} / {snapshot.WorkerCapacity}\n" +
            $"Visible: {snapshot.VisibleWorkerCount} · Inside: {snapshot.HiddenWorkerCount}";

        PopulateAssigned(snapshot);
        PopulateAvailable(snapshot);
    }

    private void PopulateAssigned(BuildingDetailSnapshot snapshot)
    {
        foreach (var child in _assignedList.GetChildren())
        {
            child.QueueFree();
        }

        if (snapshot.AssignedCount == 0)
        {
            var empty = new Label { Text = "(no workers)" };
            empty.ThemeTypeVariation = "BodySmall";
            _assignedList.AddChild(empty);
            return;
        }

        foreach (var citizen in snapshot.AssignedCitizens)
        {
            var row = InstantiateRow(
                citizen.Id.Value,
                citizen.Name,
                "Remove",
                $"Remove from {snapshot.DisplayName}");
            row.ActionRequested += id => EmitSignal(SignalName.UnassignRequested, id);
            _assignedList.AddChild(row);
        }
    }

    private void PopulateAvailable(BuildingDetailSnapshot snapshot)
    {
        foreach (var child in _availableList.GetChildren())
        {
            child.QueueFree();
        }

        if (snapshot.AvailableCitizens.Count == 0)
        {
            var empty = new Label { Text = "(no free citizens)" };
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
                "Assign",
                $"Assign to {snapshot.DisplayName}",
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
