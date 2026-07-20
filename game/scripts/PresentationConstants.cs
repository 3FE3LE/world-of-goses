namespace WorldofGoses;

/// <summary>
/// Logical base unit for the pixel art grid. All placeholder visuals
/// are sized in multiples of this value so the final art slots in
/// cleanly later.
/// </summary>
internal static class PresentationConstants
{
    /// <summary>Logical base unit for the pixel art grid.</summary>
    public const int BaseUnit = 64;

    /// <summary>Detailed LPC citizen cell used by building-detail slots.</summary>
    public const int DetailedCitizenWidth = 128;
    public const int DetailedCitizenHeight = 128;

    /// <summary>Macro citizen placeholder size, in pixels. 6 px sits in the 4–8 pixel range required by the prototype.</summary>
    public const int MacroCitizenSize = 6;

    /// <summary>Number of macro citizen activity dots the macro view shows. The requirement says this number does not need to match the total population.</summary>
    public const int MacroActivityDotCount = 6;

    /// <summary>Layout for the macro and detail views.</summary>
    public const int MacroPlotSize = BaseUnit * 3;            // 192 px
    public const int VisibleWorkerSlotWidth = DetailedCitizenWidth;
    public const int VisibleWorkerSlotHeight = DetailedCitizenHeight;
    public const int CanvasWidth = BaseUnit * 10;            // 640 px
    public const int CanvasHeight = BaseUnit * 6;            // 384 px

    public const string GroupMacroCitizenDot = "macro_citizen_dot";
    public const string GroupVisibleWorkerSlot = "visible_worker_slot";

    /// <summary>Group name attached to every dynamic <c>BuildingPlot</c> created by <c>BuildingPlotStage</c>.</summary>
    public const string GroupBuildingPlot = "building_plot";

    /// <summary>Horizontal spacing in pixels between adjacent plots in the macro view plot stage.</summary>
    public const int MacroPlotSpacing = 16;
}
