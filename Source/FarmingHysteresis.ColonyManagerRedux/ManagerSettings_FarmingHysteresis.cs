using ColonyManagerRedux;
using static ColonyManagerRedux.Constants;

namespace FarmingHysteresis.ColonyManagerRedux;

/// <summary>
/// Global (mod-config-scoped, not per-save) settings for the Farming Hysteresis manager job.
/// Currently holds only the "take over Farming Hysteresis control" toggle.
/// </summary>
internal sealed class ManagerSettings_FarmingHysteresis : ManagerSettings
{
    // Defaults to on: CMR-driven control is the intended way to use this mod. Saves that already
    // have old-style bounds configured are protected from silently losing them not by changing
    // this default, but by CmrMigrationGate forcing the *effective* controller off for such a
    // save until the player resolves the one-time migration prompt - see ApplyControllerState.
    public bool TakeOverHysteresisControl = true;

    /// <summary>
    /// Resolves the single, authoritative instance CMR holds for this mod's <c>ManagerDef</c>
    /// (there's only ever one) - looked up fresh every time rather than cached in a static field
    /// set from <see cref="PostMake"/>, because <c>ColonyManagerRedux.Settings</c>'s constructor
    /// eagerly creates one throwaway instance per <c>ManagerDef</c> (calling <see cref="PostMake"/>
    /// on it, with this class's field defaults) purely to have *something* in the list before its
    /// own <c>ExposeData</c> runs; deep-scribe deserialization then replaces that list entry with
    /// a brand new, separately-constructed object carrying the actual persisted settings, but
    /// never calls <see cref="PostMake"/> on it. A static field only ever set from
    /// <see cref="PostMake"/> would therefore keep pointing at the discarded, default-valued
    /// throwaway object forever, so any code reading it (e.g.
    /// <see cref="CmrMigrationGate.HandleGameLoaded"/>) would see a stale
    /// <see cref="TakeOverHysteresisControl"/> value that never reflects what the player actually
    /// set in the mod options tab.
    /// </summary>
    internal static ManagerSettings_FarmingHysteresis? Instance =>
        ColonyManagerReduxMod.Settings.ManagerSettingsFor<ManagerSettings_FarmingHysteresis>(
            ManagerDefOf.CM_FarmingHysteresisManager
        );

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
            TakeOverHysteresisControl
            && !CmrMigrationGameComponent.IsCurrentSaveSuppressingTakeover()
                ? CmrHysteresisController.Instance
                : DefaultHysteresisController.Instance;
}
