using FarmingHysteresis.Defs;

namespace FarmingHysteresis.ColonyManagerRedux;

/// <summary>
/// Which of a crop's harvested products (see <see cref="SecondaryProductResolvers"/>, e.g.
/// Vanilla Expanded Framework's dual-crop mechanic - <c>Docs/CMRIntegrationRework.md</c>'s Step 6)
/// <see cref="CropRotationEntry.SyncTrackedFilterToTargetPlant"/> auto-tracks. Only meaningful
/// while <see cref="CropRotationEntry.TrackedFilterFollowsTargetPlant"/> is on and the crop
/// actually has a resolvable secondary product (see
/// <see cref="CropRotationEntry.HasResolvableSecondaryProduct"/>) - an ordinary crop with no
/// secondary product is always tracked by primary product alone regardless of this value.
/// </summary>
internal enum DualCropTrackingMode
{
    PrimaryOnly,
    SecondaryOnly,
    Both,
}

/// <summary>
/// A single step in <see cref="ManagerJob_FarmingHysteresis.RotationEntries"/> — a crop to grow,
/// the stock bounds that decide when to move on to the next entry in the rotation, and (since
/// what determines "this crop is done" will often differ per crop) its own independent tracked
/// item filter/stockpile/count-all-on-map settings (see <c>Docs/CMRIntegrationRework.md</c>, Step
/// 5 — resolves #6, and same-session follow-ups). Its own <see cref="Lower"/>/<see cref="Upper"/>/
/// <see cref="TrackedThingFilter"/> etc. only take effect while it's the job's active entry
/// (<see cref="Trigger_Hysteresis"/>'s equivalent members delegate to whichever entry is active).
/// </summary>
internal sealed class CropRotationEntry : IExposable
{
    /// <summary>
    /// A stable identity for this entry, unique within its owning job's
    /// <see cref="ManagerJob_FarmingHysteresis.RotationEntries"/> (assigned via
    /// <see cref="ManagerJob_FarmingHysteresis.AllocateNextEntryId"/>) - this, not this entry's
    /// current position in the list, is what
    /// <see cref="ManagerJob_FarmingHysteresis.ActiveEntryId"/> tracks, so the "active" marker
    /// correctly follows this specific crop across reordering/removal of other entries (see
    /// <c>Docs/CMRIntegrationRework.md</c>'s Step 5 follow-up). 0 is reserved as "never assigned" -
    /// only possible for an entry loaded from a save predating this field, fixed up in
    /// <see cref="ManagerJob_FarmingHysteresis.ExposeData"/>'s <c>PostLoadInit</c> pass.
    /// </summary>
    public int Id;

    private ThingDef? _plantDef;

    /// <summary>
    /// The crop this entry grows. Setting this re-syncs <see cref="TrackedThingFilter"/> to it
    /// (via <see cref="SyncTrackedFilterToTargetPlant"/>) whenever
    /// <see cref="TrackedFilterFollowsTargetPlant"/> is on, so e.g. the picker's FloatMenu callback
    /// (<c>entry.PlantDef = candidate</c>) doesn't need to remember to sync separately.
    /// </summary>
    public ThingDef? PlantDef
    {
        get => _plantDef;
        set
        {
            _plantDef = value;
            SyncTrackedFilterToTargetPlant();
        }
    }

    public int Lower = FarmingHysteresisMod.Settings.DefaultHysteresisLowerBound;
    public int Upper = FarmingHysteresisMod.Settings.DefaultHysteresisUpperBound;

    /// <summary>
    /// This entry's own hysteresis latch state, recomputed every manager job cycle regardless of
    /// whether it's currently the job's active entry (see
    /// <see cref="Trigger_Hysteresis.ComputeCycleUpdate"/>) - each crop's hysteresis
    /// is its own, independent memory, so a crop that isn't being actively grown right now still
    /// remembers whether it's latched enabled/disabled the next time it becomes active, rather than
    /// starting over from <see cref="LatchMode.Unknown"/> (see <c>Docs/CMRIntegrationRework.md</c>'s
    /// per-job rotation mode follow-up).
    /// </summary>
    public LatchMode LatchModeValue = LatchMode.Unknown;

    /// <summary>This entry's own tracked-thing count as of the last manager job cycle - not scribed, recomputed every cycle.</summary>
    public int TrackedThingCount;

    /// <summary>Not scribed — <see cref="Widgets.IntEntry"/> needs a stable buffer across frames, per-entry.</summary>
    internal string? LowerBuffer;

    /// <summary>Not scribed — see <see cref="LowerBuffer"/>.</summary>
    internal string? UpperBuffer;

    private ThingFilter trackedThingFilter;

    public CropRotationEntry()
    {
        trackedThingFilter = new ThingFilter(NoOpSettingsChangedCallback);
        trackedThingFilter.SetDisallowAll();
    }

    /// <summary>The filter this entry counts against while active — see <see cref="Trigger_Hysteresis.TrackedThingFilter"/>.</summary>
    public ThingFilter TrackedThingFilter => trackedThingFilter;

    /// <summary>
    /// <see cref="ThingFilter"/>'s settings-changed callback is bound at construction time (it's a
    /// private field with no public setter - confirmed via decompiler), so a callback is required
    /// even though nothing needs to react to it: this trigger's latch state is only ever
    /// recomputed once per actual manager job cycle (see
    /// <see cref="Trigger_Hysteresis.ApplyCycleUpdate"/>), never eagerly on edit.
    /// </summary>
    private static void NoOpSettingsChangedCallback() { }

    /// <summary>
    /// Whether <see cref="TrackedThingFilter"/> is kept in sync with this entry's own
    /// <see cref="PlantDef"/> (see <see cref="SyncTrackedFilterToTargetPlant"/>) rather than left
    /// to the player's own choice. Defaults to <see langword="true"/> so a freshly added entry
    /// behaves exactly like this integration did before per-crop tracked items existed — tracking
    /// whatever plant it's set to grow — until a player explicitly detaches it.
    /// </summary>
    public bool TrackedFilterFollowsTargetPlant = true;

    public Zone_Stockpile? Stockpile;

    private string? _stockpileScribe;

    public bool CountAllOnMap;

    /// <summary>
    /// Which of this entry's harvested products (see <see cref="DualCropTrackingMode"/>) to
    /// auto-track. Defaults to <see cref="DualCropTrackingMode.PrimaryOnly"/> so every existing
    /// save keeps behaving exactly as before this field existed with zero behavior change.
    /// </summary>
    public DualCropTrackingMode Mode = DualCropTrackingMode.PrimaryOnly;

    /// <summary>
    /// Whether this entry's <see cref="PlantDef"/> has any resolvable secondary product (see
    /// <see cref="SecondaryProductResolvers"/>) — used by the UI to decide whether the
    /// <see cref="Mode"/> selector is worth showing at all. An ordinary crop with no such product
    /// never shows it, and <see cref="Mode"/> is inert for such an entry regardless of its scribed
    /// value.
    /// </summary>
    internal bool HasResolvableSecondaryProduct => SecondaryProductResolvers.ResolveFor(PlantDef).Any();

    /// <summary>
    /// Pure dispatch behind <see cref="SyncTrackedFilterToTargetPlant"/> - which defs
    /// <paramref name="mode"/> resolves to, given this entry's own primary/secondary products.
    /// Split out as a static method so it's unit-testable without a live <see cref="PlantDef"/>/
    /// mod-extension lookup.
    /// </summary>
    internal static IEnumerable<ThingDef> ComputeTrackedDefs(
        DualCropTrackingMode mode,
        ThingDef? primary,
        IEnumerable<ThingDef> secondary
    ) =>
        mode switch
        {
            DualCropTrackingMode.PrimaryOnly => primary != null ? [primary] : [],
            DualCropTrackingMode.SecondaryOnly => secondary,
            DualCropTrackingMode.Both => primary != null ? secondary.Append(primary) : secondary,
            _ => throw new InvalidOperationException($"Uncovered {nameof(DualCropTrackingMode)}: {mode}."),
        };

    /// <summary>
    /// Re-seeds <see cref="TrackedThingFilter"/> to allow only this entry's own
    /// <see cref="PlantDef"/>'s harvested product(s), per <see cref="Mode"/> — no-ops unless
    /// <see cref="TrackedFilterFollowsTargetPlant"/> is on.
    /// </summary>
    internal void SyncTrackedFilterToTargetPlant()
    {
        if (!TrackedFilterFollowsTargetPlant)
        {
            return;
        }

        var primary = PlantDef?.plant.harvestedThingDef;
        var secondary = SecondaryProductResolvers.ResolveFor(PlantDef);
        Trigger_Hysteresis.SyncFilterToDefs(trackedThingFilter, ComputeTrackedDefs(Mode, primary, secondary));
    }

    /// <summary>
    /// Resolves <see cref="Stockpile"/> from its scribed label — called from
    /// <see cref="ManagerJob_FarmingHysteresis.ExposeData"/>'s <c>PostLoadInit</c> pass, which has
    /// access to the job's map; a stockpile reference isn't scribable directly, same reasoning as
    /// <see cref="Trigger_Hysteresis.ExposeData"/> used before this moved per-entry.
    /// </summary>
    internal void ResolveStockpileReference(Map map) =>
        Stockpile =
            map.zoneManager.AllZones.FirstOrDefault(z => z is Zone_Stockpile && z.label == _stockpileScribe)
            as Zone_Stockpile;

    public void ExposeData()
    {
        Scribe_Values.Look(ref Id, "id");
        Scribe_Defs.Look(ref _plantDef, "plantDef");
        Scribe_Values.Look(ref Lower, "lower", FarmingHysteresisMod.Settings.DefaultHysteresisLowerBound);
        Scribe_Values.Look(ref Upper, "upper", FarmingHysteresisMod.Settings.DefaultHysteresisUpperBound);
        Scribe_Values.Look(ref LatchModeValue, "latchMode", LatchMode.Unknown);
        Scribe_Values.Look(ref TrackedFilterFollowsTargetPlant, "trackedFilterFollowsTargetPlant", true);
        Scribe_Deep.Look(ref trackedThingFilter, "trackedThingFilter", (object)NoOpSettingsChangedCallback);
        Scribe_Values.Look(ref CountAllOnMap, "countAllOnMap");
        Scribe_Values.Look(ref Mode, "dualCropTrackingMode", DualCropTrackingMode.PrimaryOnly);

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            _stockpileScribe = Stockpile?.ToString() ?? "null";
        }
        Scribe_Values.Look(ref _stockpileScribe, "stockpile", "null");
    }
}
