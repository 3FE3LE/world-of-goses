#nullable enable
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Side panel of the building detail view. Lists currently-assigned
/// workers and available citizens, exposing assign/unassign actions.
/// </summary>
public partial class AssignmentPanel : PanelContainer
{
    [Signal] public delegate void AssignRequestedEventHandler(int citizenId);
    [Signal] public delegate void UnassignRequestedEventHandler(int citizenId);

    private const int RowHeight = 22;

    private VBoxContainer _root = null!;
    private Label _summary = null!;
    private VBoxContainer _assignedList = null!;
    private VBoxContainer _availableList = null!;
    private LineageThemeSignals? _themeSignals;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(220, 0);

        _root = new VBoxContainer();
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
        _assignedList = new VBoxContainer();
        _root.AddChild(_assignedList);

        _root.AddChild(new HSeparator());

        var availableHeader = new Label { Text = "Available" };
        availableHeader.ThemeTypeVariation = "SectionTitle";
        _root.AddChild(availableHeader);
        _availableList = new VBoxContainer();
        _root.AddChild(_availableList);
    }

    public override void _ExitTree()
    {
        if (_themeSignals is not null) _themeSignals.LineageChanged -= OnLineageChanged;
    }

    private void OnLineageChanged(string lineage) => AddThemeStyleboxOverride(
        "panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));

    public void Refresh(Building building, CityWorldController controller)
    {
        _summary.Text =
            $"Assigned: {building.AssignedCount} / {building.WorkerCapacity}\n" +
            $"Visible: {building.VisibleWorkerCount} · Inside: {building.HiddenWorkerCount}";

        PopulateAssigned(building, controller);
        PopulateAvailable(building, controller);
    }

    private void PopulateAssigned(Building building, CityWorldController controller)
    {
        foreach (var child in _assignedList.GetChildren())
        {
            child.QueueFree();
        }

        if (building.AssignedCount == 0)
        {
            var empty = new Label { Text = "(no workers)" };
            empty.ThemeTypeVariation = "BodySmall";
            _assignedList.AddChild(empty);
            return;
        }

        foreach (var citizenId in building.AssignedCitizenIds)
        {
            string name = controller.Citizens().TryGetValue(citizenId, out var citizen)
                ? citizen.Name
                : $"Citizen {citizenId.Value}";
            var row = BuildRow(citizenId, name, "Remove", $"Remove from {building.DisplayName}");
            row.GetNode<Button>("Button").Pressed += () =>
                EmitSignal(SignalName.UnassignRequested, citizenId.Value);
            _assignedList.AddChild(row);
        }
    }

    private void PopulateAvailable(Building building, CityWorldController controller)
    {
        foreach (var child in _availableList.GetChildren())
        {
            child.QueueFree();
        }

        var available = controller.AvailableCitizens();
        if (available.Count == 0)
        {
            var empty = new Label { Text = "(no free citizens)" };
            empty.ThemeTypeVariation = "BodySmall";
            _availableList.AddChild(empty);
            return;
        }

        foreach (var citizen in available)
        {
            bool canAssign = building.AssignedCount < building.WorkerCapacity;
            var row = BuildRow(citizen.Id, citizen.Name, "Assign", $"Assign to {building.DisplayName}");
            var button = row.GetNode<Button>("Button");
            button.Disabled = !canAssign;
            var capturedId = citizen.Id;
            button.Pressed += () => EmitSignal(SignalName.AssignRequested, capturedId.Value);
            _availableList.AddChild(row);
        }
    }

    private static HBoxContainer BuildRow(CitizenId id, string name, string actionLabel, string actionTooltip)
    {
        var row = new HBoxContainer { CustomMinimumSize = new Vector2(0, RowHeight) };
        var label = new Label { Text = name, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        label.ThemeTypeVariation = "BodyText";
        row.AddChild(label);
        var button = new Button
        {
            Text = actionLabel,
            Name = "Button",
            TooltipText = actionTooltip,
            ThemeTypeVariation = "ButtonText",
        };
        row.AddChild(button);
        return row;
    }
}
