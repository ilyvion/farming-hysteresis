using ColonyManagerRedux;
using static ColonyManagerRedux.Constants;

namespace FarmingHysteresis.ColonyManagerRedux;

/// <summary>
/// Global (mod-config-scoped, not per-save) settings for the Farming Hysteresis manager job.
/// Currently holds only the "take over Farming Hysteresis control" toggle; see
/// <c>Docs/CMRIntegrationRework.md</c> for the ownership model this drives.
/// </summary>
internal sealed class ManagerSettings_FarmingHysteresis : ManagerSettings
{
    // Defaults to off: until a Step 2 job actually controls something, flipping this on would
    // just leave every grower uncontrolled (see Design decision 2 in the roadmap doc).
    public bool TakeOverHysteresisControl;

    public override void PostMake() => ApplyControllerState();

    public override void DoTabContents(Rect rect)
    {
        var panelRect = new Rect(rect.xMin, rect.yMin, rect.width, rect.height - Margin);

        Widgets_Section.BeginSectionColumn(
            panelRect,
            "FarmingHysteresis.Settings",
            out var position,
            out var width
        );
        Widgets_Section.Section(ref position, width, DrawTakeOverToggle);
        Widgets_Section.EndSectionColumn("FarmingHysteresis.Settings", position);
    }

    public float DrawTakeOverToggle(Vector2 pos, float width)
    {
        var rowRect = new Rect(pos.x, pos.y, width, ListEntryHeight);

        var before = TakeOverHysteresisControl;
        Utilities.DrawToggle(
            rowRect,
            "FarmingHysteresis.CMR.TakeOverControl".Translate(),
            "FarmingHysteresis.CMR.TakeOverControlTip".Translate(),
            ref TakeOverHysteresisControl
        );
        if (TakeOverHysteresisControl != before)
        {
            ApplyControllerState();
        }

        return ListEntryHeight;
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref TakeOverHysteresisControl, "takeOverHysteresisControl", false);

        if (Scribe.mode is LoadSaveMode.LoadingVars or LoadSaveMode.PostLoadInit)
        {
            ApplyControllerState();
        }
    }

    private void ApplyControllerState() =>
        FarmingHysteresisMod.HysteresisController = TakeOverHysteresisControl
            ? CmrHysteresisController.Instance
            : DefaultHysteresisController.Instance;
}
