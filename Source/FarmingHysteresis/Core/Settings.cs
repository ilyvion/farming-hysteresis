namespace FarmingHysteresis;

/// <summary>
/// The settings for the Farming Hysteresis mod.
/// </summary>
public class Settings : ModSettings
{
    private string? _defaultHysteresisLowerBoundBuffer;
    private string? _defaultHysteresisUpperBoundBuffer;

    private int _defaultHysteresisLowerBound = Constants.DefaultHysteresisLowerBound;
    private int _defaultHysteresisUpperBound = Constants.DefaultHysteresisUpperBound;
    private bool _enabledByDefault = true;
    private bool _useGlobalValuesByDefault = true;
    private bool _countAllOnMap;
    private HysteresisMode _hysteresisMode = HysteresisMode.Sowing;
    private bool _showOldCommands;
    private bool _showHysteresisMainTab = true;
    private bool _showIlyvionLaboratoryWarning = true;

    /// <summary>
    /// Gets the default lower bound for newly created hysteresis controls.
    /// </summary>
    public int DefaultHysteresisLowerBound
    {
        get => _defaultHysteresisLowerBound;
        internal set => _defaultHysteresisLowerBound = value;
    }

    /// <summary>
    /// Gets the default upper bound for newly created hysteresis controls.
    /// </summary>
    public int DefaultHysteresisUpperBound
    {
        get => _defaultHysteresisUpperBound;
        internal set => _defaultHysteresisUpperBound = value;
    }

    /// <summary>
    /// Gets whether newly created hysteresis controls are enabled by default.
    /// </summary>
    public bool EnabledByDefault
    {
        get => _enabledByDefault;
        internal set => _enabledByDefault = value;
    }

    /// <summary>
    /// Gets whether newly created hysteresis controls use the map-global bounds by default.
    /// </summary>
    public bool UseGlobalValuesByDefault
    {
        get => _useGlobalValuesByDefault;
        internal set => _useGlobalValuesByDefault = value;
    }

    /// <summary>
    /// Gets whether the harvested-thing count should be summed across the whole map instead
    /// of just the individual plant grower.
    /// </summary>
    public bool CountAllOnMap
    {
        get => _countAllOnMap;
        internal set => _countAllOnMap = value;
    }

    /// <summary>
    /// Gets the <see cref="FarmingHysteresis.HysteresisMode"/> the mod currently controls.
    /// </summary>
    public HysteresisMode HysteresisMode
    {
        get => _hysteresisMode;
        internal set => _hysteresisMode = value;
    }

    /// <summary>
    /// Gets whether the old, deprecated increment/decrement commands should be shown.
    /// </summary>
    public bool ShowOldCommands
    {
        get => _showOldCommands;
        internal set => _showOldCommands = value;
    }

    /// <summary>
    /// Gets whether the Hysteresis main tab should be shown.
    /// </summary>
    public bool ShowHysteresisMainTab
    {
        get => _showHysteresisMainTab;
        internal set => _showHysteresisMainTab = value;
    }

    /// <summary>
    /// Gets whether sowing should be controlled based on the current <see cref="HysteresisMode"/>.
    /// </summary>
    public bool ControlSowing =>
        _hysteresisMode is HysteresisMode.Sowing or HysteresisMode.SowingAndHarvesting;

    /// <summary>
    /// Gets whether harvesting should be controlled based on the current <see cref="HysteresisMode"/>.
    /// </summary>
    public bool ControlHarvesting =>
        _hysteresisMode is HysteresisMode.Harvesting or HysteresisMode.SowingAndHarvesting;

    /// <summary>
    /// Gets or sets whether the ilyvion.Laboratory dependency warning should be shown.
    /// </summary>
    public bool ShowIlyvionLaboratoryWarning
    {
        get => _showIlyvionLaboratoryWarning;
        set => _showIlyvionLaboratoryWarning = value;
    }

    /// <inheritdoc/>
    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(
            ref _defaultHysteresisLowerBound,
            "defaultHysteresisLowerBound",
            Constants.DefaultHysteresisLowerBound
        );
        Scribe_Values.Look(
            ref _defaultHysteresisUpperBound,
            "defaultHysteresisUpperBound",
            Constants.DefaultHysteresisUpperBound
        );
        Scribe_Values.Look(ref _enabledByDefault, "enabledByDefault", true);
        Scribe_Values.Look(ref _useGlobalValuesByDefault, "useGlobalValuesByDefault", true);
        Scribe_Values.Look(ref _countAllOnMap, "countAllOnMap", false);
        Scribe_Values.Look(ref _hysteresisMode, "hysteresisMode", HysteresisMode.Sowing);
        Scribe_Values.Look(ref _showOldCommands, "showOldCommands", false);
        Scribe_Values.Look(ref _showHysteresisMainTab, "showHysteresisMainTab", true);
        Scribe_Values.Look(ref _showIlyvionLaboratoryWarning, "showIlyvionLaboratoryWarning", true);
    }

    /// <summary>
    /// Draws the mod's settings window contents.
    /// </summary>
    /// <param name="inRect">The rectangle to draw the settings window contents in.</param>
    public void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listingStandard = new();
        listingStandard.Begin(inRect);

        listingStandard.CheckboxLabeled(
            "FarmingHysteresis.EnabledByDefault".Translate(),
            ref _enabledByDefault
        );
        listingStandard.CheckboxLabeled(
            "FarmingHysteresis.UseGlobalValuesByDefault".Translate(),
            ref _useGlobalValuesByDefault
        );

        // Calculate where the CountAllOnMap checkbox will go
        var textHeight = Text.CalcHeight(
            "FarmingHysteresis.CountAllOnMap".Translate(),
            listingStandard.ColumnWidth
        );
        Rect textRect = new(
            Traverse.Create(listingStandard).Field<float>("curX").Value,
            Traverse.Create(listingStandard).Field<float>("curY").Value,
            listingStandard.ColumnWidth,
            textHeight
        );

        listingStandard.CheckboxLabeled(
            "FarmingHysteresis.CountAllOnMap".Translate(),
            ref _countAllOnMap,
            "FarmingHysteresis.CountAllOnMapTooltip".Translate()
        );

        // Render expensive icon inline in CountAllOnMap checkbox row
        var iconRect = new Rect(inRect.xMax - 16f - 32f, 0f, 16f, 16f).CenteredOnYIn(textRect);
        TooltipHandler.TipRegion(iconRect, "FarmingHysteresis.Expensive.Tooltip".Translate());
        GUI.color = _countAllOnMap ? Resources.Orange : Color.grey;
        GUI.DrawTexture(iconRect, Resources.Stopwatch);
        GUI.color = Color.white;

        listingStandard.CheckboxLabeled(
            "FarmingHysteresis.ShowOldCommands".Translate(),
            ref _showOldCommands,
            "FarmingHysteresis.ShowOldCommandsTooltip".Translate()
        );
        listingStandard.CheckboxLabeled(
            "FarmingHysteresis.ShowHysteresisMainTab".Translate(),
            ref _showHysteresisMainTab,
            "FarmingHysteresis.ShowHysteresisMainTabTooltip".Translate()
        );

#if v1_3
        if (
            listingStandard.ButtonTextLabeled(
                "FarmingHysteresis.HysteresisMode".Translate(),
                "FarmingHysteresis.Control".Translate(_hysteresisMode.AsString())
            )
        )
        {
#else
        if (
            listingStandard.ButtonTextLabeledPct(
                "FarmingHysteresis.HysteresisMode".Translate(),
                "FarmingHysteresis.Control".Translate(_hysteresisMode.AsString()),
                0.6f,
                TextAnchor.MiddleLeft
            )
        )
        {
#endif
            List<FloatMenuOption> list =
            [
                new FloatMenuOption(
                    "FarmingHysteresis.Control".Translate("FarmingHysteresis.Sowing".Translate()),
                    () => _hysteresisMode = HysteresisMode.Sowing
                ),
                new FloatMenuOption(
                    "FarmingHysteresis.Control".Translate(
                        "FarmingHysteresis.Harvesting".Translate()
                    ),
                    () => _hysteresisMode = HysteresisMode.Harvesting
                ),
                new FloatMenuOption(
                    "FarmingHysteresis.Control".Translate(
                        "FarmingHysteresis.SowingAndHarvesting".Translate()
                    ),
                    () => _hysteresisMode = HysteresisMode.SowingAndHarvesting
                ),
            ];
            Find.WindowStack.Add(new FloatMenu(list));
        }

        listingStandard.Label("FarmingHysteresis.DefaultLowerBound".Translate());
        listingStandard.IntEntry(
            ref _defaultHysteresisLowerBound,
            ref _defaultHysteresisLowerBoundBuffer
        );

        listingStandard.Label("FarmingHysteresis.DefaultUpperBound".Translate());
        listingStandard.IntEntry(
            ref _defaultHysteresisUpperBound,
            ref _defaultHysteresisUpperBoundBuffer
        );

        listingStandard.End();
    }
}

/// <summary>
/// Determines which plant grower activities the mod's hysteresis controls apply to.
/// </summary>
public enum HysteresisMode
{
    /// <summary>Only control sowing.</summary>
    Sowing,

    /// <summary>Only control harvesting.</summary>
    Harvesting,

    /// <summary>Control both sowing and harvesting.</summary>
    SowingAndHarvesting,
}

/// <summary>
/// Extension methods for <see cref="HysteresisMode"/>.
/// </summary>
public static class HysteresisModeExtensions
{
    /// <summary>
    /// Gets the translated, human-readable name of the given <paramref name="mode"/>.
    /// </summary>
    /// <param name="mode">The mode to get the name of.</param>
    /// <returns>The translated name of <paramref name="mode"/>.</returns>
    public static string AsString(this HysteresisMode mode) =>
        mode switch
        {
            HysteresisMode.Sowing => "FarmingHysteresis.Sowing".Translate(),
            HysteresisMode.Harvesting => "FarmingHysteresis.Harvesting".Translate(),
            HysteresisMode.SowingAndHarvesting =>
                "FarmingHysteresis.SowingAndHarvesting".Translate(),
            _ => throw new InvalidOperationException($"Uncovered HysteresisMode: {mode}"),
        };
}
