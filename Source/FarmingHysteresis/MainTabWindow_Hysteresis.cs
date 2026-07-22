namespace FarmingHysteresis;

/// <summary>
/// The main button worker for the Hysteresis main tab, hidden unless enabled in settings.
/// </summary>
public class MainButtonWorker_Hysteresis : MainButtonWorker_ToggleTab
{
    /// <inheritdoc/>
    public override bool Visible =>
        ComputeVisible(
            FarmingHysteresisMod.Settings.ShowHysteresisMainTab,
            FarmingHysteresisMod.HysteresisController.ShowMainTab
        );

    /// <summary>
    /// Pure AND behind <see cref="Visible"/>, split out so it's unit-testable without the live
    /// <see cref="FarmingHysteresisMod.Settings"/>/<see cref="FarmingHysteresisMod.HysteresisController"/>
    /// singletons.
    /// </summary>
    internal static bool ComputeVisible(bool showHysteresisMainTab, bool controllerShowMainTab) =>
        showHysteresisMainTab && controllerShowMainTab;
}

/// <summary>
/// The main tab window showing and allowing editing of the Map- and Game-tier hysteresis bounds.
/// </summary>
public class MainTabWindow_Hysteresis : MainTabWindow
{
    private enum HysteresisTab : byte
    {
        HysteresisValues,
        SomethingElse,
    }

    private readonly List<TabRecord> tabs = [];

    private static HysteresisTab currentTab = HysteresisTab.HysteresisValues;

    // Self is intentionally not selectable here: it's per-grower storage, edited only via
    // ITab_Hysteresis.
    private static BoundsSource selectedSource = BoundsSource.Map;

    private readonly Dictionary<ThingDef, IBoundedValueAccessor> boundAccessors = [];
    private readonly Dictionary<ThingDef, BoundValues> bounds = [];
    private readonly Dictionary<ThingDef, string?> boundLowerBuffers = [];
    private readonly Dictionary<ThingDef, string?> boundUpperBuffers = [];

    // The map RebuildBoundsList() last resolved Find.CurrentMap against, so DoWindowContents can
    // detect a map switch while the tab stays open and
    // rebuild against the new map instead of silently editing the old one's bounds.
    private Map? boundMap;

    /// <summary>
    /// Whether a bound-map switch happened while the
    /// tab is open and Map-tier bounds are being shown, requiring a rebuild against the new map
    /// instead of continuing to show/edit the previous map's bounds.
    /// </summary>
    internal static bool ShouldRebuildForMapSwitch(
        BoundsSource selectedSource,
        object? boundMap,
        object? currentMap
    ) => selectedSource == BoundsSource.Map && !ReferenceEquals(boundMap, currentMap);

    private static IBoundedValueAccessor GetAccessorFor(ThingDef thingDef) =>
        selectedSource switch
        {
            BoundsSource.Map => FarmingHysteresisMapComponent
                .For(Find.CurrentMap)
                .GetMapBoundedValueAccessorFor(thingDef),
            BoundsSource.Game => FarmingHysteresisGameComponent
                .For(Current.Game)
                .GetGameBoundedValueAccessorFor(thingDef),
            BoundsSource.Self => throw new InvalidOperationException(
                $"MainTabWindow_Hysteresis has no list view for {selectedSource}."
            ),
            _ => throw new InvalidOperationException($"Uncovered BoundsSource: {selectedSource}."),
        };

    private void RebuildBoundsList()
    {
        boundMap = Find.CurrentMap;
        boundAccessors.Clear();
        boundLowerBuffers.Clear();
        boundUpperBuffers.Clear();
        bounds.Clear();
        foreach (
            var plantDef in DefDatabase<ThingDef>.AllDefs.Where(def =>
                def.category == ThingCategory.Plant
            )
        )
        {
            var harvestedThingDef = plantDef.plant.harvestedThingDef;
            if (harvestedThingDef == null || boundAccessors.ContainsKey(harvestedThingDef))
            {
                continue;
            }
            boundAccessors.Add(harvestedThingDef, GetAccessorFor(harvestedThingDef));
            bounds.Add(harvestedThingDef, boundAccessors[harvestedThingDef].PeekBoundValue());
            boundLowerBuffers.Add(harvestedThingDef, null);
            boundUpperBuffers.Add(harvestedThingDef, null);
        }
        _filteredHarvestedThingDefs = null;
    }

    /// <inheritdoc/>
    public override void PreOpen()
    {
        base.PreOpen();
        tabs.Clear();
        tabs.Add(
            new TabRecord(
                "FarmingHysteresis.HysteresisBounds".Translate(),
                delegate
                {
                    currentTab = HysteresisTab.HysteresisValues;
                },
                () => currentTab == HysteresisTab.HysteresisValues
            )
        );
        // tabs.Add(new TabRecord("SomethingElse".Translate(), delegate
        // {
        //     currentTab = HysteresisTab.SomethingElse;
        // }, () => currentTab == HysteresisTab.SomethingElse));

        RebuildBoundsList();
    }

    /// <inheritdoc/>
    public override void DoWindowContents(Rect inRect)
    {
        if (ShouldRebuildForMapSwitch(selectedSource, boundMap, Find.CurrentMap))
        {
            RebuildBoundsList();
        }

        var rect2 = inRect;
        rect2.yMin += 45f;

        Rect sourceSelectorRect = new(rect2.xMax - 200f, rect2.y - 32f, 200f, 30f);
        if (Widgets.ButtonText(sourceSelectorRect, BoundsSourceUi.Label(selectedSource)))
        {
            List<FloatMenuOption> options =
            [
                new(
                    BoundsSourceUi.Label(BoundsSource.Map),
                    () =>
                    {
                        selectedSource = BoundsSource.Map;
                        RebuildBoundsList();
                    }
                ),
                new(
                    BoundsSourceUi.Label(BoundsSource.Game),
                    () =>
                    {
                        selectedSource = BoundsSource.Game;
                        RebuildBoundsList();
                    }
                ),
            ];
            Find.WindowStack.Add(new FloatMenu(options));
        }

        _ = TabDrawer.DrawTabs(rect2, tabs);
        switch (currentTab)
        {
            case HysteresisTab.HysteresisValues:
                DoHysteresisValuesPage(rect2);
                break;
            case HysteresisTab.SomethingElse:
                //DoSomethingElsePage(rect2);
                break;
            default:
                break;
        }
    }

    private Vector2 messagesScrollPos;
    private float scrollViewHeight;

    private readonly QuickSearchWidget _quickSearch = new();
    private List<ThingDef>? _filteredHarvestedThingDefs;

    private void UpdateFilter()
    {
        _filteredHarvestedThingDefs =
        [
            .. boundAccessors.Keys.Where(h =>
                MatchesQuickSearch(h.label, _quickSearch.filter.Text)
            ),
        ];
        _quickSearch.noResultsMatched = _filteredHarvestedThingDefs.Count == 0;
    }

    /// <summary>
    /// Split out so the case-insensitivity of the quick search filter is unit-testable across
    /// all supported RimWorld versions without a live <see cref="QuickSearchWidget"/>.
    /// </summary>
#pragma warning disable IDE0057, CA2249 // string.Contains(string, StringComparison) isn't available pre-1.6
    internal static bool MatchesQuickSearch(string label, string filterText) =>
        label.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0;
#pragma warning restore IDE0057, CA2249

    private void DoHysteresisValuesPage(Rect tabRect)
    {
        if (_filteredHarvestedThingDefs == null)
        {
            UpdateFilter();
        }

        Rect quickSearchRect = new(tabRect.x + 3f, tabRect.y + 5f, tabRect.width - 16f - 6f, 24f);
        _quickSearch.OnGUI(quickSearchRect, UpdateFilter);

        Rect listRect = new(
            tabRect.x,
            quickSearchRect.yMax + 5f,
            tabRect.width,
            tabRect.height - quickSearchRect.height - 5f
        );

        Rect viewRect = new(0f, 0f, tabRect.width - 16f, scrollViewHeight);
        Widgets.BeginScrollView(listRect, ref messagesScrollPos, viewRect);

        var num = 0f;
        foreach (var harvestedThingDef in _filteredHarvestedThingDefs!)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            Widgets.DrawLineHorizontal(0f, num, viewRect.width);
            GUI.color = Color.white;

            num += DrawPlantRow(harvestedThingDef, num, viewRect);
        }

        if (Event.current.type == EventType.Layout)
        {
            scrollViewHeight = num;
        }
        Widgets.EndScrollView();
    }

    private const float PLANT_ROW_HEIGHT = 52f;
    private const float PLANT_ROW_GAP_WIDTH = 32f;

    private float DrawPlantRow(ThingDef harvest, float rowY, Rect fillRect)
    {
        Rect rowRect = new(0f, rowY, fillRect.width, PLANT_ROW_HEIGHT);
        Rect labelRect = new(90f, rowY, 250f, PLANT_ROW_HEIGHT);
        Rect plantIconRect = new(24f, rowY + 3f, 42f, 42f);

        GUI.color = new Color(1f, 1f, 1f, 0.5f);
        Widgets.DrawHighlightIfMouseover(rowRect);
        GUI.color = Color.white;

        DrawHarvestIconWithLabelAndTooltip(plantIconRect, labelRect, harvest);

        var lowerBoundRect = DrawLowerBoundWidget(labelRect, rowY, harvest);
        _ = DrawUpperBoundWidget(lowerBoundRect, rowY, harvest);

        return PLANT_ROW_HEIGHT;
    }

    private static void DrawHarvestIconWithLabelAndTooltip(
        Rect harvestIconRect,
        Rect harvestLabelRect,
        ThingDef harvestDef
    )
    {
        GUI.DrawTexture(harvestIconRect, harvestDef.uiIcon);

        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(harvestLabelRect, harvestDef.LabelCap);
        Text.Anchor = TextAnchor.UpperLeft;

        if (Mouse.IsOver(harvestIconRect))
        {
            TipSignal tip = new(
                harvestDef.LabelCap.Colorize(ColoredText.TipSectionTitleColor)
                    + "\n\n"
                    + harvestDef.description
            );
            TooltipHandler.TipRegion(harvestIconRect, tip);
        }
        if (Mouse.IsOver(harvestLabelRect))
        {
            TipSignal tip = new(
                harvestDef.LabelCap.Colorize(ColoredText.TipSectionTitleColor)
                    + "\n\n"
                    + harvestDef.description
            );
            TooltipHandler.TipRegion(harvestLabelRect, tip);
        }

        if (
            Widgets.ButtonInvisible(harvestIconRect, doMouseoverSound: false)
            || Widgets.ButtonInvisible(harvestLabelRect, doMouseoverSound: false)
        )
        {
            Find.WindowStack.Add(new Dialog_InfoCard(harvestDef));
        }
    }

    /// <summary>
    /// Draws the lower-bound entry widget for the given <paramref name="harvestDef"/>.
    /// </summary>
    /// <param name="prevRect">The rect of the widget drawn immediately before this one.</param>
    /// <param name="rowY">The vertical position of the row being drawn.</param>
    /// <param name="harvestDef">The harvested thing def the widget is for.</param>
    /// <returns>The rect the widget was drawn in.</returns>
    public Rect DrawLowerBoundWidget(Rect prevRect, float rowY, ThingDef harvestDef)
    {
        var lowerBoundRect = new Rect(prevRect.xMax, rowY, 250f, PLANT_ROW_HEIGHT);
        var listingStandard = new Listing_Standard();
        listingStandard.Begin(lowerBoundRect);
        var boundValues = bounds[harvestDef];
        var oldValue = boundValues.Lower;
        ref var value = ref boundValues.Lower;
        var buffer = boundLowerBuffers[harvestDef];
        _ = listingStandard.Label("FarmingHysteresis.LowerBoundLabel".Translate());
        listingStandard.IntEntry(ref value, ref buffer);
        value = HysteresisBoundClamp.ClampLower(value, boundValues.Upper);
        boundLowerBuffers[harvestDef] = buffer;
        listingStandard.End();

        if (value != oldValue)
        {
            boundAccessors[harvestDef].CommitBoundValue(boundValues);
        }

        return lowerBoundRect;
    }

    /// <summary>
    /// Draws the upper-bound entry widget for the given <paramref name="harvestDef"/>.
    /// </summary>
    /// <param name="prevRect">The rect of the widget drawn immediately before this one.</param>
    /// <param name="rowY">The vertical position of the row being drawn.</param>
    /// <param name="harvestDef">The harvested thing def the widget is for.</param>
    /// <returns>The rect the widget was drawn in.</returns>
    public Rect DrawUpperBoundWidget(Rect prevRect, float rowY, ThingDef harvestDef)
    {
        var upperBoundRect = new Rect(
            prevRect.xMax + PLANT_ROW_GAP_WIDTH,
            rowY,
            250f,
            PLANT_ROW_HEIGHT
        );
        var listingStandard = new Listing_Standard();
        listingStandard.Begin(upperBoundRect);
        var boundValues = bounds[harvestDef];
        var oldValue = boundValues.Upper;
        ref var value = ref boundValues.Upper;
        var buffer = boundUpperBuffers[harvestDef];
        _ = listingStandard.Label("FarmingHysteresis.UpperBoundLabel".Translate());
        listingStandard.IntEntry(ref value, ref buffer);
        value = HysteresisBoundClamp.ClampUpper(value, boundValues.Lower);
        boundUpperBuffers[harvestDef] = buffer;
        listingStandard.End();

        if (value != oldValue)
        {
            boundAccessors[harvestDef].CommitBoundValue(boundValues);
        }

        return upperBoundRect;
    }

    // private void DoSomethingElsePage(Rect rect2)
    // {
    // }
}
