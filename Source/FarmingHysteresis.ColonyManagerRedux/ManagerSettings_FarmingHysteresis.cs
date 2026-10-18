using ColonyManagerRedux;
using static ColonyManagerRedux.Constants;

namespace FarmingHysteresis.ColonyManagerRedux;

/// <summary>
/// Global (mod-config-scoped, not per-save) settings for the Farming Hysteresis manager job: the
/// "take over Farming Hysteresis control" toggle, and the defaults newly created jobs/rotation
/// entries are seeded with.
/// </summary>
internal sealed class ManagerSettings_FarmingHysteresis : ManagerSettings
{
    // Defaults to on: CMR-driven control is the intended way to use this mod. Saves that already
    // have old-style bounds configured are protected from silently losing them not by changing
    // this default, but by CmrMigrationGate forcing the *effective* controller off for such a
    // save until the player resolves the one-time migration prompt - see ApplyControllerState.
    public bool TakeOverHysteresisControl = true;

    /// <summary>
    /// The lower bound a newly added <see cref="CropRotationEntry"/> starts with - CMR's own copy
    /// of what <see cref="FarmingHysteresisMod.Settings"/>'s equally-named setting provides the
    /// legacy per-grower engine, kept independent since the two engines' defaults are no longer
    /// meant to be synced (see <c>FarmingHysteresis.OnlyAppliesWithoutCmr</c>).
    /// </summary>
    public int DefaultHysteresisLowerBound = Constants.DefaultHysteresisLowerBound;

    /// <summary>See <see cref="DefaultHysteresisLowerBound"/> - same shape, for the upper bound.</summary>
    public int DefaultHysteresisUpperBound = Constants.DefaultHysteresisUpperBound;

    /// <summary>
    /// The <see cref="HysteresisMode"/> a newly created
    /// <see cref="ManagerJob_FarmingHysteresis"/> starts with - see
    /// <see cref="DefaultHysteresisLowerBound"/> for why this is CMR's own independent copy
    /// rather than reading <see cref="FarmingHysteresisMod.Settings"/>.
    /// </summary>
    public HysteresisMode DefaultHysteresisMode = HysteresisMode.Sowing;

    /// <summary>
    /// The <see cref="RotationMode"/> a newly created <see cref="ManagerJob_FarmingHysteresis"/>
    /// starts with - see <see cref="DefaultHysteresisLowerBound"/> for why this is CMR's own
    /// independent copy rather than reading <see cref="FarmingHysteresisMod.Settings"/>.
    /// </summary>
    public RotationMode DefaultRotationMode = RotationMode.Priority;

    /// <summary>
    /// The <see cref="RotationSwitchMode"/> a newly created <see cref="ManagerJob_FarmingHysteresis"/>
    /// starts with - see <see cref="DefaultHysteresisLowerBound"/> for why this is CMR's own
    /// independent copy rather than reading <see cref="FarmingHysteresisMod.Settings"/>.
    /// </summary>
    public RotationSwitchMode DefaultSwitchMode = RotationSwitchMode.WaitForGrowthToFinish;

    /// <summary>Not scribed - <see cref="Widgets.IntEntry"/> needs a stable buffer across frames.</summary>
    private string? _defaultLowerBoundBuffer;

    /// <summary>Not scribed - see <see cref="_defaultLowerBoundBuffer"/>.</summary>
    private string? _defaultUpperBoundBuffer;

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
        Widgets_Section.Section(ref position, width, DrawHysteresisDefaults);
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

    /// <summary>
    /// The defaults every newly created <see cref="ManagerJob_FarmingHysteresis"/>/
    /// <see cref="CropRotationEntry"/> is seeded with - the CMR-side counterpart of
    /// <see cref="FarmingHysteresisMod.Settings"/>'s equally-named settings, which now only feed
    /// the legacy per-grower engine.
    /// </summary>
    public float DrawHysteresisDefaults(Vector2 pos, float width)
    {
        var start = pos;

        DrawHysteresisModeSelector(pos, width);
        pos.y += ListEntryHeight;
        DrawRotationModeSelector(pos, width);
        pos.y += ListEntryHeight;
        DrawSwitchModeSelector(pos, width);
        pos.y += ListEntryHeight;

        Widgets.Label(
            new Rect(pos.x, pos.y, width, ListEntryHeight),
            "FarmingHysteresis.DefaultLowerBound".Translate()
        );
        pos.y += ListEntryHeight;
        var lowerRect = new Rect(pos.x, pos.y, width, ListEntryHeight);
        Widgets.IntEntry(lowerRect, ref DefaultHysteresisLowerBound, ref _defaultLowerBoundBuffer);
        pos.y += ListEntryHeight;

        Widgets.Label(
            new Rect(pos.x, pos.y, width, ListEntryHeight),
            "FarmingHysteresis.DefaultUpperBound".Translate()
        );
        pos.y += ListEntryHeight;
        var upperRect = new Rect(pos.x, pos.y, width, ListEntryHeight);
        Widgets.IntEntry(upperRect, ref DefaultHysteresisUpperBound, ref _defaultUpperBoundBuffer);
        pos.y += ListEntryHeight;

        (DefaultHysteresisLowerBound, DefaultHysteresisUpperBound) = HysteresisBoundClamp.Clamp(
            DefaultHysteresisLowerBound,
            DefaultHysteresisUpperBound
        );

        return pos.y - start.y;
    }

    /// <summary>
    /// Default-<see cref="HysteresisMode"/> icon row - same <c>Utilities.DrawToggle</c> cell
    /// layout as <see cref="ManagerTab_FarmingHysteresis"/>'s per-job selector, so the settings
    /// tab visually matches the job's own crop rotation UI.
    /// </summary>
    private void DrawHysteresisModeSelector(Vector2 pos, float width)
    {
        var modes = Enum.GetValues<HysteresisMode>();
        var cellWidth = width / modes.Length;
        var cellRect = new Rect(pos.x, pos.y, cellWidth, ListEntryHeight);

        foreach (var mode in modes)
        {
            Utilities.DrawToggle(
                cellRect,
                mode.AsString().CapitalizeFirst(),
                $"FarmingHysteresis.CMR.HysteresisMode.{mode}.Tip".Translate(),
                DefaultHysteresisMode == mode,
                () => DefaultHysteresisMode = mode,
                () => { },
                wrap: false
            );
            cellRect.x += cellWidth;
        }
    }

    /// <summary>Default-<see cref="RotationMode"/> icon row - see <see cref="DrawHysteresisModeSelector"/>.</summary>
    private void DrawRotationModeSelector(Vector2 pos, float width)
    {
        var modes = Enum.GetValues<RotationMode>();
        var cellWidth = width / modes.Length;
        var cellRect = new Rect(pos.x, pos.y, cellWidth, ListEntryHeight);

        foreach (var mode in modes)
        {
            Utilities.DrawToggle(
                cellRect,
                $"FarmingHysteresis.CMR.RotationMode.{mode}".Translate(),
                $"FarmingHysteresis.CMR.RotationMode.{mode}.Tip".Translate(),
                DefaultRotationMode == mode,
                () => DefaultRotationMode = mode,
                () => { },
                wrap: false
            );
            cellRect.x += cellWidth;
        }
    }

    /// <summary>Default-<see cref="RotationSwitchMode"/> icon row - see <see cref="DrawHysteresisModeSelector"/>.</summary>
    private void DrawSwitchModeSelector(Vector2 pos, float width)
    {
        var modes = Enum.GetValues<RotationSwitchMode>();
        var cellWidth = width / modes.Length;
        var cellRect = new Rect(pos.x, pos.y, cellWidth, ListEntryHeight);

        foreach (var mode in modes)
        {
            Utilities.DrawToggle(
                cellRect,
                $"FarmingHysteresis.CMR.RotationSwitchMode.{mode}".Translate(),
                $"FarmingHysteresis.CMR.RotationSwitchMode.{mode}.Tip".Translate(),
                DefaultSwitchMode == mode,
                () => DefaultSwitchMode = mode,
                () => { },
                wrap: false
            );
            cellRect.x += cellWidth;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref TakeOverHysteresisControl, "takeOverHysteresisControl", true);
        Scribe_Values.Look(
            ref DefaultHysteresisLowerBound,
            "defaultHysteresisLowerBound",
            Constants.DefaultHysteresisLowerBound
        );
        Scribe_Values.Look(
            ref DefaultHysteresisUpperBound,
            "defaultHysteresisUpperBound",
            Constants.DefaultHysteresisUpperBound
        );
        Scribe_Values.Look(
            ref DefaultHysteresisMode,
            "defaultHysteresisMode",
            HysteresisMode.Sowing
        );
        Scribe_Values.Look(ref DefaultRotationMode, "defaultRotationMode", RotationMode.Priority);
        Scribe_Values.Look(
            ref DefaultSwitchMode,
            "defaultSwitchMode",
            RotationSwitchMode.WaitForGrowthToFinish
        );

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
        FarmingHysteresisMod.HysteresisController = ComputeShouldUseCmrController(
            TakeOverHysteresisControl,
            CmrMigrationGameComponent.IsCurrentSaveSuppressingTakeover()
        )
            ? CmrHysteresisController.Instance
            : DefaultHysteresisController.Instance;

    /// <summary>
    /// Pure dispatch behind <see cref="ApplyControllerState"/> - whether the CMR controller should
    /// be the effective one, given the two flags that decide it. Split out as a static method so
    /// it's unit-testable without touching the live controller singletons.
    /// </summary>
    internal static bool ComputeShouldUseCmrController(
        bool takeOverHysteresisControl,
        bool isCurrentSaveSuppressingTakeover
    ) => takeOverHysteresisControl && !isCurrentSaveSuppressingTakeover;
}
