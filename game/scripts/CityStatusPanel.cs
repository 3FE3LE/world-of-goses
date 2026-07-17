using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Top-of-screen status strip in the macro view. Shows stock and
/// staffing for every building plus the citizens who remain free.
///
/// Initialization-order note: the label is constructed via a field
/// initializer (not in <c>_Ready()</c>) because the parent macro
/// view's <c>_Ready()</c> calls <see cref="Refresh"/> before this
/// panel's own <c>_Ready</c> has fired.
/// </summary>
public partial class CityStatusPanel : PanelContainer
{
    private readonly Label _label = new()
    {
        Text = "",
        HorizontalAlignment = HorizontalAlignment.Center,
    };

    public override void _Ready()
    {
        AddChild(_label);
    }

    public void Refresh(CityWorldController controller)
    {
        var free = new List<string>();
        foreach (var citizen in controller.Citizens().Values)
        {
            if (!citizen.CurrentAssignment.HasValue) free.Add(citizen.Name);
        }

        if (controller.World.Buildings.Count == 0)
        {
            _label.Text = "City (no buildings — loaded save was empty)";
            return;
        }

        var buildingSummaries = new List<string>(controller.World.Buildings.Count);
        foreach (var building in controller.World.Buildings.Values)
        {
            buildingSummaries.Add(
                $"{building.DisplayName}: {building.Stock}/{building.StorageCapacity} " +
                $"{building.ResourceUnit} · {building.AssignedCount}/{building.WorkerCapacity} workers");
        }

        string freeCitizens = free.Count == 0 ? "none" : string.Join(", ", free);
        _label.Text = $"{string.Join("  |  ", buildingSummaries)}  |  Free: {freeCitizens}";
    }
}
