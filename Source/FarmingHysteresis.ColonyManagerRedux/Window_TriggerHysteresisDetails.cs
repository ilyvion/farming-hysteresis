using ColonyManagerRedux;
using static ColonyManagerRedux.Constants;

namespace FarmingHysteresis.ColonyManagerRedux;

/// <summary>
/// Configures <see cref="Trigger_Hysteresis.TrackedThingFilter"/> - modeled directly on CMR's own
/// <see cref="WindowTriggerThresholdDetails"/> (see <c>Docs/CMRIntegrationRework.md</c>, Step 4),
/// minus its <c>Ops</c>/target-count editor - <see cref="Trigger_Hysteresis"/>'s Lower/Upper bounds
/// stay exactly where they are today, in the main tab.
/// </summary>
internal sealed class WindowTriggerHysteresisDetails(Trigger_Hysteresis trigger) : Window
{
    private readonly Trigger_Hysteresis _trigger = trigger;
    private readonly ThingFilterUI.UIState _uiState = new();

    public override Vector2 InitialSize => new(300f, 500f);

    public override void DoWindowContents(Rect inRect)
    {
        var pos = inRect.ContractedBy(6f).position;
        var width = inRect.ContractedBy(6f).width;

        var followsTargetPlant = _trigger.TrackedFilterFollowsTargetPlant;
        Utilities.DrawToggle(
            ref pos,
            width,
            "FarmingHysteresis.CMR.Trigger.FollowTargetPlant".Translate(),
            "FarmingHysteresis.CMR.Trigger.FollowTargetPlant.Tip".Translate(),
            ref followsTargetPlant
        );
        if (followsTargetPlant != _trigger.TrackedFilterFollowsTargetPlant)
        {
            _trigger.TrackedFilterFollowsTargetPlant = followsTargetPlant;
            if (followsTargetPlant)
            {
                _trigger.SyncTrackedFilterToTargetPlant();
            }
        }

        if (followsTargetPlant)
        {
            var message = "FarmingHysteresis.CMR.Trigger.FollowingTargetPlant".Translate();
            var messageHeight = Text.CalcHeight(message, width);
            Widgets.Label(new Rect(pos.x, pos.y, width, messageHeight), message);
            return;
        }

        var zoneRectRows = Math.Min(
            (int)
                Math.Ceiling(
                    (double)(
                        _trigger.Job.Manager.map.zoneManager.AllZones.OfType<Zone_Stockpile>().Count()
                        + 1
                    ) / StockpileGUI.StockPilesPerRow
                ),
            3
        );
        var zoneRectHeight = zoneRectRows * ListEntryHeight;

        var filterRect = new Rect(pos.x, pos.y, width, inRect.yMax - pos.y - zoneRectHeight - Margin);
        var zoneRect = new Rect(filterRect.xMin, filterRect.yMax + Margin, width, zoneRectHeight);

        ThingFilterUI.DoThingFilterConfigWindow(
            filterRect,
            _uiState,
            _trigger.TrackedThingFilter,
            _trigger.ParentFilter
        );
        if (Event.current.type == EventType.Layout)
        {
            // Same fixup Window_TriggerThresholdDetails applies - RimWorld pads the bottom of
            // the filter list by 90px for reasons that don't apply to this layout.
            ThingFilterUI.viewHeight -= 90f;
        }

        _ = StockpileGUI.DoStockpileSelectors(
            zoneRect.position,
            zoneRect.width,
            ref _trigger.StockpileRef,
            _trigger.Job.Manager
        );

        var countAllOnMapRect = new Rect(zoneRect.xMin, zoneRect.yMax + Margin, width, ListEntryHeight);
        var countAllOnMap = _trigger.CountAllOnMap;
        Utilities.DrawToggle(
            countAllOnMapRect,
            "ColonyManagerRedux.Threshold.CountAllOnMap".Translate(),
            "ColonyManagerRedux.Threshold.CountAllOnMap.Tip".Translate(),
            ref countAllOnMap,
            true
        );
        _trigger.CountAllOnMap = countAllOnMap;
    }
}
