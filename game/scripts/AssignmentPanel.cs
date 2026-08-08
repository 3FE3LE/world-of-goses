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
    private VBoxContainer _unavailableList = null!;
    private ScrollContainer _assignedScroll = null!;
    private ScrollContainer _availableScroll = null!;
    private ScrollContainer _unavailableScroll = null!;
    private Label _unavailableHeader = null!;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(220, 0);

        _root = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
        AddChild(_root);
        // The panel surface arrives through the theme: this node's PanelCard
        // variation is repainted per lineage by LineageThemePainter, so there is
        // nothing to override here and nothing to re-apply on a lineage change.

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

        _root.AddChild(new HSeparator());

        _unavailableHeader = new Label { Text = UiText.Get("ui.assignment.unavailable_title") };
        _unavailableHeader.ThemeTypeVariation = "SectionTitle";
        _root.AddChild(_unavailableHeader);
        _unavailableList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _unavailableScroll = BuildListScroll(_unavailableList);
        _root.AddChild(_unavailableScroll);
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


    public void Refresh(BuildingDetailSnapshot snapshot)
    {
        _summary.Text =
            UiText.Format("ui.assignment.assigned_count", snapshot.AssignedCount, snapshot.WorkerCapacity) + "\n" +
            UiText.Format("ui.assignment.visible_inside", snapshot.VisibleWorkerCount, snapshot.HiddenWorkerCount);

        PopulateAssigned(snapshot);
        PopulateAvailable(snapshot);
        PopulateUnavailable(snapshot);
        SetNaturalListHeight(_assignedScroll, snapshot.AssignedCitizens.Count);
        SetNaturalListHeight(_availableScroll, snapshot.AvailableCitizens.Count);
        SetNaturalListHeight(_unavailableScroll, snapshot.UnavailableCitizens.Count);
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

    private void PopulateUnavailable(BuildingDetailSnapshot snapshot)
    {
        foreach (var child in _unavailableList.GetChildren())
        {
            _unavailableList.RemoveChild(child);
            child.QueueFree();
        }

        bool hasUnavailable = snapshot.UnavailableCitizens.Count > 0;
        _unavailableHeader.Visible = hasUnavailable;
        _unavailableScroll.Visible = hasUnavailable;
        if (!hasUnavailable) return;

        foreach (var citizen in snapshot.UnavailableCitizens)
        {
            string reason = DescribeUnavailabilityReason(citizen);
            var row = InstantiateRow(
                citizen.Id.Value,
                UiText.Format("ui.assignment.unavailable_row", citizen.Name, reason),
                UiText.Get("Assign"),
                reason,
                disabled: true);
            _unavailableList.AddChild(row);
        }
    }

    private static string DescribeUnavailabilityReason(BuildingDetailSnapshot.UnavailableCitizenItem citizen) =>
        citizen.Reason switch
        {
            CitizenAvailabilityReason.AssignedToBuilding =>
                UiText.Format("ui.assignment.reason_building", citizen.LocationName ?? UiText.Get("Unknown")),
            CitizenAvailabilityReason.AssignedToConstruction =>
                UiText.Format("ui.assignment.reason_construction", citizen.LocationName ?? UiText.Get("Unknown")),
            CitizenAvailabilityReason.OnExpedition => UiText.Get("ui.assignment.reason_expedition"),
            CitizenAvailabilityReason.Recovering => UiText.Get("ui.assignment.reason_recovering"),
            _ => UiText.Get("Available"),
        };

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
