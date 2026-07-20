using WorldofGoses;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public class LineageThemeRegistryTests
{
    [Fact]
    public void AvailableLineages_ContainsExactlyEightIds()
    {
        Assert.Equal(8, LineageThemeRegistry.AvailableLineages.Count);
        Assert.Contains("ardhen", LineageThemeRegistry.AvailableLineages);
        Assert.Contains("theryn", LineageThemeRegistry.AvailableLineages);
    }

    [Fact]
    public void IdOf_NormalisesLineageValue()
    {
        Assert.Equal("ardhen", LineageThemeRegistry.IdOf(LineageId.Ardhen));
        Assert.Equal("caelith", LineageThemeRegistry.IdOf(LineageId.Caelith));
    }

    [Fact]
    public void GetStyleboxPath_ReturnsAssetForRegisteredLineage()
    {
        string path = LineageThemeRegistry.GetStyleboxPath("ardhen", "panel");
        Assert.Equal(
            "res://assets/ui/lineages/ardhen/panel/panel.stylebox.tres",
            path);
    }

    [Fact]
    public void GetStyleboxPath_FallsBackToDefaultForUnknownLineage()
    {
        string path = LineageThemeRegistry.GetStyleboxPath("atlantis", "panel");
        Assert.Equal(LineageThemeRegistry.DefaultPanelStyleboxPath, path);
    }

    [Fact]
    public void GetStyleboxPath_FallsBackToSameLineagePanelForMissingComponent()
    {
        string path = LineageThemeRegistry.GetStyleboxPath("eirune", "button_primary");
        Assert.Equal("res://assets/ui/lineages/eirune/panel/panel.stylebox.tres", path);
    }

    [Fact]
    public void SetActiveLineage_UsesProjectDefaultForUnknownValue()
    {
        LineageThemeRegistry.SetActiveLineage("ardhen");
        LineageThemeRegistry.SetActiveLineage("not-a-lineage");
        Assert.Equal(LineageThemeRegistry.SystemDefaultLineage, LineageThemeRegistry.ActiveLineage);
    }

    [Fact]
    public void SetActiveLineage_FiresEventOnChange()
    {
        LineageThemeRegistry.SetActiveLineage("ardhen");
        var received = (string?)null;
        void Handler(string lineage) => received = lineage;
        LineageThemeRegistry.ActiveLineageChanged += Handler;
        try
        {
            LineageThemeRegistry.SetActiveLineage("theryn");
            Assert.Equal("theryn", received);
        }
        finally
        {
            LineageThemeRegistry.ActiveLineageChanged -= Handler;
        }
    }
}
