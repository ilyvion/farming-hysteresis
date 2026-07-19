using ColonyManagerRedux;
using static ColonyManagerRedux.Constants;

namespace FarmingHysteresis.ColonyManagerRedux;

/// <summary>
/// Configures a single <see cref="CropRotationEntry"/>'s own <see cref="CropRotationEntry.TrackedThingFilter"/>
/// - modeled directly on CMR's own <see cref="WindowTriggerThresholdDetails"/> (see
/// <c>Docs/CMRIntegrationRework.md</c>, Step 4), minus its <c>Ops</c>/target-count editor. Tracked
/// items live per rotation entry (see Step 5 follow-up) rather than on the job's
/// <see cref="Trigger_Hysteresis"/> as a whole, since what determines "this crop is done" often
/// differs per crop; <see cref="CropRotationEntry.Lower"/>/<see cref="CropRotationEntry.Upper"/>
/// bounds stay where they are, in the crop rotation list itself.
/// </summary>
internal sealed class WindowTriggerHysteresisDetails(CropRotationEntry entry, Manager manager)
    : Window
{
    private readonly CropRotationEntry _entry = entry;
    private readonly Manager _manager = manager;
    private readonly ThingFilterUI.UIState _uiState = new();

    public override Vector2 InitialSize => new(300f, 500f);

    public override void DoWindowContents(Rect inRect)
    {
        var pos = inRect.ContractedBy(6f).position;
        var width = inRect.ContractedBy(6f).width;

        var followsTargetPlant = _entry.TrackedFilterFollowsTargetPlant;
        Utilities.DrawToggle(
            ref pos,
            width,
            "FarmingHysteresis.CMR.Trigger.FollowTargetPlant".Translate(),
            "FarmingHysteresis.CMR.Trigger.FollowTargetPlant.Tip".Translate(),
            ref followsTargetPlant
        );
        if (followsTargetPlant != _entry.TrackedFilterFollowsTargetPlant)
        {
            _entry.TrackedFilterFollowsTargetPlant = followsTargetPlant;
            if (followsTargetPlant)
            {
                _entry.SyncTrackedFilterToTargetPlant();
            }
        }

        if (followsTargetPlant)
        {
            if (_entry.HasResolvableSecondaryProduct)
            {
                DrawDualCropTrackingModeSelector(ref pos, width);
            }

            var message = "FarmingHysteresis.CMR.Trigger.FollowingTargetPlant".Translate();
            var messageHeight = Text.CalcHeight(message, width);
            Widgets.Label(new Rect(pos.x, pos.y, width, messageHeight), message);
            return;
        }

        var zoneRectRows = Math.Min(
            (int)
                Math.Ceiling(
                    (double)(_manager.map.zoneManager.AllZones.OfType<Zone_Stockpile>().Count() + 1)
                        / StockpileGUI.StockPilesPerRow
                ),
            3
        );
        var zoneRectHeight = zoneRectRows * ListEntryHeight;

        var filterRect = new Rect(
            pos.x,
            pos.y,
            width,
            inRect.yMax - pos.y - zoneRectHeight - Margin
        );
        var zoneRect = new Rect(filterRect.xMin, filterRect.yMax + Margin, width, zoneRectHeight);

        ThingFilterUI.DoThingFilterConfigWindow(
            filterRect,
            _uiState,
            _entry.TrackedThingFilter,
            Trigger_Hysteresis.ParentFilter
        );
        if (Event.current.type == EventType.Layout)
        {
            // Same fixup Window_TriggerThresholdDetails applies - RimWorld pads the bottom of
            // the filter list by 90px for reasons that don't apply to this layout.
            ThingFilterUI.viewHeight -= 90f;
        }

        var stockpile = _entry.Stockpile;
        _ = StockpileGUI.DoStockpileSelectors(
            zoneRect.position,
            zoneRect.width,
            ref stockpile,
            _manager
        );
        _entry.Stockpile = stockpile;

        var countAllOnMapRect = new Rect(
            zoneRect.xMin,
            zoneRect.yMax + Margin,
            width,
            ListEntryHeight
        );
        var countAllOnMap = _entry.CountAllOnMap;
        Utilities.DrawToggle(
            countAllOnMapRect,
            "ColonyManagerRedux.Threshold.CountAllOnMap".Translate(),
            "ColonyManagerRedux.Threshold.CountAllOnMap.Tip".Translate(),
            ref countAllOnMap,
            true
        );
        _entry.CountAllOnMap = countAllOnMap;
    }

    /// <summary>
    /// Only shown while <see cref="CropRotationEntry.HasResolvableSecondaryProduct"/> is true (see
    /// <c>Docs/CMRIntegrationRework.md</c>'s Step 6). Unlike
    /// <see cref="ManagerTab_FarmingHysteresis.DrawRotationModeSelector"/>/
    /// <see cref="ManagerTab_FarmingHysteresis.DrawSwitchModeSelector"/> (which share this same
    /// per-cell toggle-row shape but only ever have 2 values, drawn across the main tab's much
    /// wider column), <see cref="DualCropTrackingMode"/> has 3 values and this window is only
    /// 300px wide (see <see cref="InitialSize"/>) - three cells side by side left no room for
    /// "Primary only"/"Secondary only"/"Both" plus their checkmarks, so labels overlapped garbled
    /// into their neighboring cell. One full-width toggle per row avoids that entirely.
    /// </summary>
    private void DrawDualCropTrackingModeSelector(ref Vector2 pos, float width)
    {
        foreach (var mode in (DualCropTrackingMode[])Enum.GetValues(typeof(DualCropTrackingMode)))
        {
            var rowRect = new Rect(pos.x, pos.y, width, ListEntryHeight);
            Utilities.DrawToggle(
                rowRect,
                $"FarmingHysteresis.CMR.DualCropTrackingMode.{mode}".Translate(),
                $"FarmingHysteresis.CMR.DualCropTrackingMode.{mode}.Tip".Translate(),
                _entry.Mode == mode,
                () =>
                {
                    _entry.Mode = mode;
                    _entry.SyncTrackedFilterToTargetPlant();
                },
                () => { },
                wrap: false
            );
            pos.y += ListEntryHeight;
        }
    }
}
