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
    // Defaults to on: CMR-driven control is the intended way to use this mod going forward (see
    // Design decision 2 in Docs/CMRIntegrationRework.md). Saves that already have old-style
    // bounds configured are protected from silently losing them not by changing this default,
    // but by CmrMigrationGate forcing the *effective* controller off for such a save until the
    // player resolves the one-time migration prompt - see ApplyControllerState.
    public bool TakeOverHysteresisControl = true;

    /// <summary>
    /// The single instance CMR creates for this mod's <c>ManagerDef</c> (there's only ever one).
    /// Cached here in <see cref="PostMake"/> so <see cref="CmrMigrationGate"/> can reach the
    /// setting/apply the controller without needing a <c>ManagerDef</c> lookup of its own.
    /// </summary>
    internal static ManagerSettings_FarmingHysteresis? Instance { get; private set; }

    public override void PostMake()
    {
        Instance = this;
        ApplyControllerState();
    }

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

        Scribe_Values.Look(ref TakeOverHysteresisControl, "takeOverHysteresisControl", true);

        if (Scribe.mode is LoadSaveMode.LoadingVars or LoadSaveMode.PostLoadInit)
        {
            ApplyControllerState();
        }
    }

    /// <summary>
    /// Installs the controller that matches the current effective takeover state - the global
    /// <see cref="TakeOverHysteresisControl"/> setting, unless <see cref="CmrMigrationGameComponent"/>
    /// is still suppressing it for the currently loaded save (no game loaded counts as "not
    /// suppressed", matching pre-migration-gate behavior at the main menu).
    /// </summary>
    internal void ApplyControllerState() =>
        FarmingHysteresisMod.HysteresisController =
            TakeOverHysteresisControl && !CmrMigrationGameComponent.IsCurrentSaveSuppressingTakeover()
                ? CmrHysteresisController.Instance
                : DefaultHysteresisController.Instance;
}
