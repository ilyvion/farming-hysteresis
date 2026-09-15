using ColonyManagerRedux;
using FarmingHysteresis.Defs;
using FarmingHysteresis.Extensions;

namespace FarmingHysteresis.ColonyManagerRedux;

/// <summary>
/// How <see cref="ManagerJob_FarmingHysteresis.AssignmentMode"/> narrows down
/// <see cref="ManagerJob_FarmingHysteresis.ScopeEligiblePlantGrowers"/> — same shape as
/// <c>ManagerJob_Production.WorkbenchAssignmentMode</c>/<c>ManagerJob_Mining.MiningArea</c>.
/// </summary>
internal enum GrowerAssignmentMode
{
    All,
    Area,
    Specific,
}

/// <summary>
/// Whether switching to the next crop in <see cref="ManagerJob_FarmingHysteresis.RotationEntries"/>
/// force-clears the outgoing crop's not-yet-ripe leftover plants (losing their yield, for an
/// instant cutover) or leaves them to mature and harvest normally. Either way, already-ripe
/// leftovers are always harvested rather than stranded — see
/// <see cref="ManagerJob_FarmingHysteresis.GrowerHasLeftoverPlants"/>.
/// </summary>
internal enum RotationSwitchMode
{
    WaitForGrowthToFinish,
    SwitchImmediately,
}

/// <summary>
/// How <see cref="ManagerJob_FarmingHysteresis.ActiveEntryId"/> is picked each manager job cycle
/// (see <see cref="Trigger_Hysteresis.ComputeCycleUpdate"/>) - a per-job choice between the two
/// rotation semantics the player can pick.
/// </summary>
internal enum RotationMode
{
    /// <summary>
    /// Every manager job cycle, the active entry is whichever entry is first (by list position)
    /// among those whose own latch isn't <see cref="LatchMode.AboveUpperBound"/> (i.e. not yet
    /// satisfied) - list order is a priority order, so reordering can hand priority to an earlier
    /// crop immediately, and a crop that's already full is skipped over entirely rather than
    /// waited on.
    /// </summary>
    Priority,

    /// <summary>
    /// The current entry stays active until its own hysteresis latch transitions into
    /// <see cref="LatchMode.AboveUpperBound"/> (i.e. it's satisfied), at which point the rotation
    /// moves on to the next entry by list position, cycling. Reordering the list only changes
    /// where the cycle resumes, never which entry is active right now.
    /// </summary>
    RoundRobin,
}

internal sealed class ManagerJob_FarmingHysteresis
    : ManagerJob<ManagerSettings_FarmingHysteresis, ManagerJob_FarmingHysteresis.WorkData>
{
    /// <summary>
    /// The gather phase's verdict: the growers to apply it to, and this cycle's latch/rotation
    /// update, computed but <b>not yet applied</b> - see <see cref="Trigger_Hysteresis.CycleUpdate"/>'s
    /// own doc comment for why gather must not write it, only <see cref="ExecuteJobDataCoroutine"/>
    /// may (via <see cref="Trigger_Hysteresis.ApplyCycleUpdate"/>).
    /// </summary>
    internal sealed class WorkData(
        IReadOnlyList<IPlantToGrowSettable> growers,
        Trigger_Hysteresis.CycleUpdate cycleUpdate,
        RotationSwitchMode switchMode
    )
    {
        public IReadOnlyList<IPlantToGrowSettable> Growers { get; } = growers;
        public Trigger_Hysteresis.CycleUpdate CycleUpdate { get; } = cycleUpdate;
        public RotationSwitchMode SwitchMode { get; } = switchMode;
    }

    /// <summary>
    /// Charts <see cref="Trigger_Hysteresis.TrackedThingCount"/> alongside its two bounds as
    /// three independent flat/varying lines - "stock", "lower bound", and "upper bound" - each
    /// chapter's "count" is simply that value each tick. None of them use CMR's target-line
    /// mechanism.
    /// </summary>
    public sealed class History : HistoryWorker<ManagerJob_FarmingHysteresis>
    {
        /// <summary>
        /// Pure per-chapter count selection behind
        /// <see cref="GetCountForHistoryChapterCoroutine"/>, split out so it's unit-testable
        /// without a live <see cref="ManagerJob_FarmingHysteresis"/>/coroutine.
        /// </summary>
        internal static int SelectCount(
            ManagerJobHistoryChapterDef chapterDef,
            int trackedThingCount,
            int lower,
            int upper
        ) =>
            chapterDef == ManagerJobHistoryChapterDefOf.FH_HistoryStock ? trackedThingCount
            : chapterDef == ManagerJobHistoryChapterDefOf.FH_HistoryLower ? lower
            : upper;

        /// <inheritdoc/>
        public override Coroutine GetCountForHistoryChapterCoroutine(
            ManagerJob_FarmingHysteresis managerJob,
            int tick,
            ManagerJobHistoryChapterDef chapterDef,
            Boxed<int> count
        )
        {
            // Charts whatever TrackedThingCount/Lower/Upper held as of the job's last actual
            // work cycle (see Trigger_Hysteresis.ApplyCycleUpdate's own doc comment) -
            // deliberately not forced fresh here, same reasoning as State itself.
            count.Value = SelectCount(
                chapterDef,
                managerJob.HysteresisTrigger.TrackedThingCount,
                managerJob.HysteresisTrigger.Lower,
                managerJob.HysteresisTrigger.Upper
            );
            yield break;
        }

        /// <inheritdoc/>
        public override Coroutine GetTargetForHistoryChapterCoroutine(
            ManagerJob_FarmingHysteresis managerJob,
            int tick,
            ManagerJobHistoryChapterDef chapterDef,
            Boxed<int> target
        )
        {
            // None of this job's chapters use a target line - all three (stock/lower/upper)
            // are already plain value series, so there's nothing left for a target to add.
            target.Value = 0;
            yield break;
        }
    }

    public ManagerJob_FarmingHysteresis(Manager manager)
        : base(manager)
    {
        Trigger = new Trigger_Hysteresis(this);
    }

    public Trigger_Hysteresis HysteresisTrigger => (Trigger_Hysteresis)Trigger!;

    public override bool IsValid => base.IsValid && Trigger != null;

    /// <summary>
    /// Pure decision logic behind <see cref="IsManaged"/>, split out so the "both must hold"
    /// rule is unit-testable without a live <c>Manager</c>/job instance.
    /// </summary>
    internal static bool ComputeIsManaged(bool baseIsManaged, bool ownsJobExecution) =>
        baseIsManaged && ownsJobExecution;

    /// <summary>
    /// Gates <see cref="JobTracker"/> from ever selecting this job as <c>NextJob</c> (and thus
    /// from ever invoking <see cref="GatherJobDataCoroutine"/>/<see cref="ExecuteJobDataCoroutine"/>)
    /// while CMR isn't actually the active controller - i.e. while "take over Farming Hysteresis
    /// control" is off, or a per-save <see cref="CmrMigrationGate"/> is still suppressing it. Without
    /// this, an existing job would keep pushing plant/sow/harvest state onto its growers even while
    /// the old always-on engine (<see cref="DefaultHysteresisController"/>) is simultaneously
    /// managing the same growers - two controllers must never act on the same grower at once. The
    /// job's own config (scope, target plant, bounds) is untouched either way; it simply goes
    /// dormant.
    /// </summary>
    public override bool IsManaged =>
        ComputeIsManaged(
            base.IsManaged,
            FarmingHysteresisMod.HysteresisController is CmrHysteresisController
        );

    /// <summary>
    /// The raw "has this job been committed to <see cref="JobTracker"/>" flag, ignoring whether
    /// CMR is currently the active controller - unlike <see cref="IsManaged"/>, which folds both
    /// together for job-execution gating. <see cref="ManagerTab_FarmingHysteresis"/>'s
    /// Manage!/Delete toggle needs this instead of <see cref="IsManaged"/>: if it used
    /// <see cref="IsManaged"/>, a job that's already committed but currently dormant (takeover
    /// off, or a <see cref="CmrMigrationGate"/> still suppressing it) would read as "not managed"
    /// and show "Manage!" again, and clicking it would re-add an already-tracked job to
    /// <see cref="JobTracker"/> as a duplicate.
    /// </summary>
    internal bool IsCommittedToTracker => base.IsManaged;

    public GrowerAssignmentMode AssignmentMode = GrowerAssignmentMode.All;
    public Area? GrowerArea;
    public bool InvertGrowerArea;

    /// <summary>
    /// Deliberately typed as <see cref="Zone"/> rather than <see cref="Zone_Growing"/> - soft-mod
    /// integrations (e.g. Vanilla Plants Expanded: More Plants' <c>Zone_GrowingAquatic</c>/
    /// <c>Zone_GrowingSandy</c>) register their own <c>FarmingHysteresisControlDef</c> for zone
    /// types that implement <see cref="IPlantToGrowSettable"/> directly without deriving from
    /// <see cref="Zone_Growing"/>. Narrowing this to <see cref="Zone_Growing"/> silently dropped
    /// those grower types from ever being selectable.
    /// </summary>
    public HashSet<Zone> SpecificGrowingZones = [];
    public HashSet<Building_PlantGrower> SpecificPlantGrowerBuildings = [];

    /// <summary>
    /// The ordered crops this job cycles through: the growers it manages are pushed onto
    /// <see cref="ActiveEntry"/>'s plant until that entry's own stock threshold is satisfied (see
    /// <see cref="Trigger_Hysteresis.ShouldAdvanceRotation"/>), at which point
    /// <see cref="ComputeNewActiveEntryId"/> moves on to the next entry, cycling. A single-entry
    /// list behaves like a plain one-crop-per-job setup - nothing to ever switch to.
    /// </summary>
    public List<CropRotationEntry> RotationEntries = [];

    /// <summary>
    /// The <see cref="CropRotationEntry.Id"/> of the crop currently being pushed onto managed
    /// growers, or <see langword="null"/> if <see cref="RotationEntries"/> is empty. Tracked by
    /// stable identity rather than list position specifically so it survives
    /// <see cref="MoveRotationEntry"/>/<see cref="RemoveRotationEntry"/> reordering other entries
    /// out from under it - a plain index would silently point at the wrong crop whenever an
    /// earlier entry was removed. Only ever changed by
    /// <see cref="Trigger_Hysteresis.ApplyCycleUpdate"/>, called exclusively from
    /// <see cref="ExecuteJobDataCoroutine"/> (an actual manager job cycle) - reordering/removing
    /// entries between cycles never moves this on its own.
    /// </summary>
    public int? ActiveEntryId;

    /// <summary>Per-job counter behind <see cref="AllocateNextEntryId"/>.</summary>
    private int _nextEntryId = 1;

    /// <summary>Mints a fresh, job-unique <see cref="CropRotationEntry.Id"/>.</summary>
    internal int AllocateNextEntryId() => _nextEntryId++;

    /// <summary>
    /// The rotation entry currently being pushed onto managed growers, resolved from
    /// <see cref="ActiveEntryId"/>, or <see langword="null"/> if <see cref="RotationEntries"/> is
    /// empty.
    /// </summary>
    public CropRotationEntry? ActiveEntry =>
        RotationEntries.Count == 0
            ? null
            : RotationEntries.FirstOrDefault(e => e.Id == ActiveEntryId);

    /// <summary>
    /// Whether switching to the next rotation entry force-clears the outgoing crop's not-yet-ripe
    /// leftovers or leaves them to mature and harvest normally - see <see cref="RotationSwitchMode"/>,
    /// defaulting to <see cref="ManagerSettings_FarmingHysteresis.DefaultSwitchMode"/> for a freshly
    /// created job.
    /// </summary>
    public RotationSwitchMode SwitchMode =
        ManagerSettings_FarmingHysteresis.Instance?.DefaultSwitchMode
        ?? RotationSwitchMode.WaitForGrowthToFinish;

    /// <summary>
    /// Which rotation semantics (see <see cref="RotationMode"/>) this job uses to pick
    /// <see cref="ActiveEntryId"/> each manager job cycle - defaulting to
    /// <see cref="ManagerSettings_FarmingHysteresis.DefaultRotationMode"/> for a freshly created
    /// job.
    /// </summary>
    public RotationMode Mode =
        ManagerSettings_FarmingHysteresis.Instance?.DefaultRotationMode ?? RotationMode.Priority;

    /// <summary>
    /// Which plant grower activities (see <see cref="FarmingHysteresis.HysteresisMode"/>) this
    /// job's hysteresis latch controls - a per-job choice, defaulting to
    /// <see cref="ManagerSettings_FarmingHysteresis.DefaultHysteresisMode"/> for a freshly created
    /// job. See <see cref="HasMigratedHysteresisMode"/> for how an existing job saved before this
    /// field existed picks up its starting value instead.
    /// </summary>
    public HysteresisMode HysteresisMode =
        ManagerSettings_FarmingHysteresis.Instance?.DefaultHysteresisMode ?? HysteresisMode.Sowing;

    /// <summary>
    /// Whether this job caps how many of its <see cref="ManagedGrowers"/>' cells are actually
    /// sown at once, instead of sowing every eligible cell the moment the active entry's latch
    /// enables sowing - see <see cref="ComputeRequiredCellCount"/>/<see cref="ComputeCascadingSowPlan"/>
    /// for the cap itself. Off by default: the cap is only ever an estimate (it can't account for
    /// blights, fires, cold snaps, or anything else the game throws at a growing crop between now
    /// and harvest), so a player who wants the mod's exact "stop when full" guarantee instead of
    /// an approximate "sow roughly this much" estimate keeps today's behavior unless they opt in.
    /// </summary>
    public bool LimitSowingToComputedNeed;

    /// <summary>
    /// Multiplies <see cref="ComputeRequiredCellCount"/>'s raw cell estimate, while
    /// <see cref="LimitSowingToComputedNeed"/> is on, to hedge against the estimate falling short
    /// (partial fertility, plant death, imprecise yield rounding, etc.) - 1.2 sows roughly 20%
    /// more cells than the bare math calls for.
    /// </summary>
    public float SowingSafetyMultiplier = 1.2f;

    /// <summary>
    /// The per-cell sow plan <see cref="LimitSowingToComputedNeed"/> currently enforces, or
    /// <see langword="null"/> while it's off (unrestricted, see <see cref="IsCellWithinSowBudget"/>)
    /// or before <see cref="RecomputeSowBudget"/> has ever run for this job (disallowed everywhere
    /// in that case, not unrestricted). Scribed alongside the map it belongs to (see
    /// <see cref="ExposeData"/>) so a save/load keeps enforcing exactly what the last manager job
    /// cycle decided. Maps a cell to whichever rotation entry's plant it's been assigned to grow (see
    /// <see cref="ComputeCascadingSowPlan"/>); a cell missing from this plan gets no sow job at
    /// all. An assigned plant that isn't <see cref="TargetPlantDef"/> means the cascade has handed
    /// this otherwise-idle cell to a later crop in the rotation (see
    /// <see cref="GetCellSowPlantOverride"/>) rather than leaving it fallow while the active crop's
    /// own need is already covered by fewer cells.
    /// </summary>
    private Dictionary<IntVec3, ThingDef>? _cellSowPlan;

    /// <summary>
    /// Pure decision behind <see cref="IsCellWithinSowBudget"/>: unrestricted (<see langword="true"/>)
    /// while <paramref name="limitSowingToComputedNeed"/> is off; while it's on, disallowed
    /// everywhere until <paramref name="cellSowPlan"/> has actually been computed at least once for
    /// this job rather than defaulting to unrestricted in the meantime - a missing plan means the
    /// manager hasn't decided yet, not that there's no limit. Split out so it's unit-testable.
    /// </summary>
    internal static bool ComputeIsCellWithinSowBudget(
        bool limitSowingToComputedNeed,
        IReadOnlyDictionary<IntVec3, ThingDef>? cellSowPlan,
        IntVec3 cell
    ) => !limitSowingToComputedNeed || (cellSowPlan is { } plan && plan.ContainsKey(cell));

    /// <summary>
    /// Whether <see cref="WorkGiver_GrowerSow"/> is currently allowed to sow <paramref name="cell"/>
    /// - see <see cref="CmrHysteresisController.IsCellSowAllowed"/>, this job's own entry point, and
    /// <see cref="ComputeIsCellWithinSowBudget"/> for the actual decision.
    /// </summary>
    internal bool IsCellWithinSowBudget(IntVec3 cell) =>
        ComputeIsCellWithinSowBudget(LimitSowingToComputedNeed, _cellSowPlan, cell);

    /// <summary>
    /// Which plant <paramref name="cell"/> should actually be sown with, if this job's cascading
    /// sow plan (see <see cref="_cellSowPlan"/>) has assigned it to a rotation entry other than
    /// the active one - <see langword="null"/> (meaning "use the grower's own plant as normal")
    /// whenever the plan agrees with <see cref="TargetPlantDef"/>, the mode is off, or nothing's
    /// been computed yet. See <see cref="CmrHysteresisController.GetCellSowPlantOverride"/>, this
    /// job's own entry point.
    /// </summary>
    internal ThingDef? GetCellSowPlantOverride(IntVec3 cell) =>
        LimitSowingToComputedNeed
        && _cellSowPlan is { } plan
        && plan.TryGetValue(cell, out var plantDef)
        && plantDef != TargetPlantDef
            ? plantDef
            : null;

    /// <summary>
    /// Pure decision behind <see cref="GetComputedNeedStats"/>: the same yield-per-cell/cells-needed
    /// figures <see cref="ComputeCascadingSowPlan"/> itself derives for one entry, plus how many
    /// cells <paramref name="cellSowPlan"/> currently has assigned to <paramref name="plantDef"/> -
    /// shown in the crop rotation UI so a player can see what "limit sowing to computed need" is
    /// actually doing for each crop. <c>CellsNeeded</c> is <see langword="null"/> for an
    /// <paramref name="unbound"/> entry, which always claims every cell still available to it rather
    /// than a fixed count. Split out so it's unit-testable without a live job.
    /// </summary>
    internal static (int YieldPerCell, int? CellsNeeded, int CellsPlanned) ComputeNeedStats(
        ThingDef? plantDef,
        int trackedThingCount,
        int upper,
        float safetyMultiplier,
        bool unbound,
        IReadOnlyDictionary<IntVec3, ThingDef>? cellSowPlan
    )
    {
        if (plantDef == null)
        {
            return (0, 0, 0);
        }

        var yieldPerCell = (int)plantDef.plant.harvestYield;
        var cellsNeeded = unbound
            ? (int?)null
            : ComputeRequiredCellCount(
                Math.Max(0, upper - trackedThingCount),
                yieldPerCell,
                safetyMultiplier
            );
        var cellsPlanned = cellSowPlan?.Count(kvp => kvp.Value == plantDef) ?? 0;
        return (yieldPerCell, cellsNeeded, cellsPlanned);
    }

    /// <summary>
    /// Live glue behind <see cref="ComputeNeedStats"/> for <paramref name="entry"/>, this job's own
    /// entry point for <c>ManagerTab_FarmingHysteresis</c>'s per-crop stats display.
    /// </summary>
    internal (int YieldPerCell, int? CellsNeeded, int CellsPlanned) GetComputedNeedStats(
        CropRotationEntry entry
    ) =>
        ComputeNeedStats(
            entry.PlantDef,
            entry.TrackedThingCount,
            entry.Upper,
            SowingSafetyMultiplier,
            Trigger_Hysteresis.IsEffectivelyUnbound(entry, RotationEntries),
            _cellSowPlan
        );

    /// <summary>
    /// Pure decision behind <see cref="ComputeCascadingSowPlan"/>'s per-entry cell count: how many
    /// cells, combined across every cell this job could still sow, are needed to close the gap
    /// between <paramref name="remainingNeed"/> (an entry's upper bound minus its current tracked
    /// count, clamped to non-negative) and 0, at <paramref name="yieldPerCell"/> product per cell
    /// (a plant's declared <c>harvestYield</c> - fertility only affects how fast a cell matures,
    /// never how much a mature cell ultimately yields), inflated by
    /// <paramref name="safetyMultiplier"/>. Split out so it's unit-testable without a live
    /// job/map.
    /// </summary>
    internal static int ComputeRequiredCellCount(
        int remainingNeed,
        int yieldPerCell,
        float safetyMultiplier
    ) =>
        remainingNeed <= 0
            ? 0
            : (int)
                Math.Ceiling(remainingNeed / (double)Math.Max(1, yieldPerCell) * safetyMultiplier);

    /// <summary>
    /// Pure decision behind <see cref="ComputeCascadingSowPlan"/>'s entry visit order: every
    /// rotation entry id, starting at <paramref name="activeEntryId"/>'s own position and wrapping
    /// back around through whichever entries precede it - so a crop whose own need is already
    /// covered by fewer cells than are available hands its excess on to the next crop in the
    /// rotation (see <see cref="ComputeCascadingSowPlan"/>), cycling all the way through rather
    /// than stopping after just one crop ahead. Falls back to list order if
    /// <paramref name="activeEntryId"/> doesn't match anything in <paramref name="entryIds"/>.
    /// </summary>
    internal static IReadOnlyList<int> OrderEntryIdsStartingAtActive(
        IReadOnlyList<int> entryIds,
        int? activeEntryId
    )
    {
        if (entryIds.Count == 0)
        {
            return [];
        }

        var startIndex = activeEntryId is { } id ? entryIds.ToList().IndexOf(id) : -1;
        if (startIndex < 0)
        {
            startIndex = 0;
        }

        return [.. entryIds.Skip(startIndex), .. entryIds.Take(startIndex)];
    }

    /// <summary>
    /// Pure decision behind <see cref="RecomputeSowBudget"/>'s cell assignment: which rotation
    /// entry's plant, if any, each of <paramref name="cells"/> should be sown with. Visits
    /// <paramref name="entriesInOrder"/> in order (see <see cref="OrderEntryIdsStartingAtActive"/> -
    /// normally starting at the active entry), and for each one first reserves every cell already
    /// growing its own plant (no new sow job needed, but it still counts against that entry's own
    /// <see cref="ComputeRequiredCellCount"/>), then hands it as many of the remaining, still-empty
    /// cells as it still needs, favoring the highest-fertility cells first (they mature fastest)
    /// with a stable tie-break. Any cell left over once every entry's need is covered - or handed
    /// to whichever entry is <paramref name="entriesInOrder"/>'s effectively-unbound last entry,
    /// which always takes every cell still available to it - stays unassigned (fallow) rather than
    /// sowing anything. A cell already occupied by a plant that isn't any listed entry's own (e.g.
    /// a leftover from a crop no longer in the rotation, or a wild filler plant) is never handed to
    /// another entry - it stays occupied until whatever already-existing cut/leftover handling
    /// clears it. Split out so it's unit-testable without a live job/map.
    /// </summary>
    internal static Dictionary<IntVec3, ThingDef> ComputeCascadingSowPlan(
        IReadOnlyList<(IntVec3 Cell, float Fertility, ThingDef? StandingPlantDef)> cells,
        IReadOnlyList<(
            ThingDef PlantDef,
            int RemainingNeed,
            int YieldPerCell,
            bool Unbound
        )> entriesInOrder,
        float safetyMultiplier
    )
    {
        var plan = new Dictionary<IntVec3, ThingDef>();

        foreach (var (plantDef, remainingNeed, yieldPerCell, unbound) in entriesInOrder)
        {
            var alreadyGrowingCount = 0;
            foreach (var (cell, _, standingPlantDef) in cells)
            {
                if (!plan.ContainsKey(cell) && standingPlantDef == plantDef)
                {
                    plan[cell] = plantDef;
                    alreadyGrowingCount++;
                }
            }

            var stillNeeded = unbound
                ? int.MaxValue
                : Math.Max(
                    0,
                    ComputeRequiredCellCount(remainingNeed, yieldPerCell, safetyMultiplier)
                        - alreadyGrowingCount
                );
            if (stillNeeded <= 0)
            {
                continue;
            }

            var candidates = cells
                .Where(c => !plan.ContainsKey(c.Cell) && c.StandingPlantDef == null)
                .OrderByDescending(c => c.Fertility)
                .ThenBy(c => c.Cell.x)
                .ThenBy(c => c.Cell.z);

            foreach (
                var (cell, _, _) in stillNeeded == int.MaxValue
                    ? candidates
                    : candidates.Take(stillNeeded)
            )
            {
                plan[cell] = plantDef;
            }
        }

        return plan;
    }

    /// <summary>
    /// Live glue behind <see cref="RecomputeSowBudget"/>'s <see cref="ComputeCascadingSowPlan"/>
    /// call: every cell across <see cref="ManagedGrowers"/>, alongside its own fertility and
    /// whatever plant currently stands on it (if any).
    /// </summary>
    private IEnumerable<(
        IntVec3 Cell,
        float Fertility,
        ThingDef? StandingPlantDef
    )> GatherSowBudgetInputCells() =>
        ManagedGrowers.SelectMany(grower =>
            grower.Cells.Select(cell =>
                (cell, grower.Map.fertilityGrid.FertilityAt(cell), cell.GetPlant(grower.Map)?.def)
            )
        );

    /// <summary>
    /// Recomputes <see cref="_cellSowPlan"/> from this job's current state - a no-op (leaving it
    /// unrestricted) while <see cref="LimitSowingToComputedNeed"/> is off or before a target plant
    /// has been chosen. Only ever called from <see cref="ExecuteJobDataCoroutine"/>: every rotation
    /// entry's own
    /// <see cref="CropRotationEntry.TrackedThingCount"/>/<see cref="CropRotationEntry.Upper"/> this
    /// depends on is itself only refreshed once per cycle (see
    /// <see cref="Trigger_Hysteresis.ComputeCycleUpdate"/>). The plan itself stays valid
    /// between cycles and across a save/load regardless of live cell state changing (colonists
    /// sowing/harvesting): it's a decision made once per cycle and enforced live by
    /// <see cref="IsCellWithinSowBudget"/>/<see cref="GetCellSowPlantOverride"/> until the next
    /// cycle reconsiders it.
    /// </summary>
    private void RecomputeSowBudget()
    {
        if (!LimitSowingToComputedNeed || TargetPlantDef == null)
        {
            _cellSowPlan = null;
            return;
        }

        var orderedIds = OrderEntryIdsStartingAtActive(
            [.. RotationEntries.Select(e => e.Id)],
            ActiveEntryId
        );
        var entriesById = RotationEntries.ToDictionary(e => e.Id);
        var entryPlans = orderedIds
            .Select(id => entriesById[id])
            .Where(e => e.PlantDef != null)
            .Select(e =>
                (
                    PlantDef: e.PlantDef!,
                    RemainingNeed: Math.Max(0, e.Upper - e.TrackedThingCount),
                    YieldPerCell: (int)e.PlantDef!.plant.harvestYield,
                    Unbound: Trigger_Hysteresis.IsEffectivelyUnbound(e, RotationEntries)
                )
            )
            .ToList();

        _cellSowPlan = ComputeCascadingSowPlan(
            [.. GatherSowBudgetInputCells()],
            entryPlans,
            SowingSafetyMultiplier
        );
    }

    /// <summary>
    /// Whether <see cref="HysteresisMode"/> has already been resolved for this job - <see
    /// langword="true"/> by default (a freshly created job's field initializer above already gave
    /// it a sensible value), but scribed with a default of <see langword="false"/>
    /// (<see cref="ExposeData"/>), so a job saved before this field existed - which has no scribed
    /// node for it at all - loads as <see langword="false"/> instead of keeping this in-memory
    /// default. <see cref="ExposeData"/>'s <c>PostLoadInit</c> pass uses that <see
    /// langword="false"/> to detect exactly this case and copy <see
    /// cref="FarmingHysteresisMod.Settings"/>'s old global <c>HysteresisMode</c> - what this job
    /// actually used to be controlled by - into <see cref="HysteresisMode"/> once, then flips this
    /// back to <see langword="true"/> so it never runs again for this job.
    /// </summary>
    public bool HasMigratedHysteresisMode = true;

    /// <summary>
    /// The plant currently being pushed onto every grower this job manages (see
    /// <see cref="ExecuteJobDataCoroutine"/>) - <see cref="ActiveEntry"/>'s plant, or
    /// <see langword="null"/> if the list is empty (nothing configured yet).
    /// </summary>
    public ThingDef? TargetPlantDef => ActiveEntry?.PlantDef;

    /// <summary>
    /// Pure decision behind <see cref="AddRotationEntry"/>'s auto-clear step, split out so it's
    /// unit-testable without a live job: <see cref="CropRotationEntry.Unbound"/> is only
    /// meaningful for the actual last entry, so whichever entry was previously last must give it
    /// up as soon as a new entry is appended below it. A no-op on an empty list (nothing yet to
    /// have been last).
    /// </summary>
    internal static void ClearUnboundOnCurrentLastEntry(IReadOnlyList<CropRotationEntry> entries)
    {
        if (entries.Count > 0)
        {
            entries[^1].Unbound = false;
        }
    }

    /// <summary>
    /// Appends <paramref name="plantDef"/> as a new rotation entry, seeded with the mod's default
    /// bounds. The new entry syncs its own tracked filter to <paramref name="plantDef"/> itself
    /// (see <see cref="CropRotationEntry.PlantDef"/>'s setter) - no job-level resync needed now
    /// that tracked items live per entry rather than once per job. Becomes the active entry only
    /// if the list was previously empty (nothing else to have been active). See
    /// <see cref="ClearUnboundOnCurrentLastEntry"/> for why whichever entry was previously last
    /// has its <see cref="CropRotationEntry.Unbound"/> flag cleared here.
    /// </summary>
    public void AddRotationEntry(ThingDef plantDef)
    {
        ClearUnboundOnCurrentLastEntry(RotationEntries);

        var entry = new CropRotationEntry { Id = AllocateNextEntryId(), PlantDef = plantDef };
        RotationEntries.Add(entry);
        ActiveEntryId ??= entry.Id;
    }

    /// <summary>
    /// Pure decision behind <see cref="RemoveRotationEntry"/>'s active-entry fallback, split out so
    /// it's unit-testable without a live job: unaffected unless <paramref name="removedEntryId"/>
    /// was itself the active one,
    /// in which case falls back to whichever id now occupies the same list position (clamped into
    /// <paramref name="remainingEntryIds"/>), or <see langword="null"/> if that's now empty. A
    /// removal that *isn't* the active entry leaves <paramref name="activeEntryId"/> completely
    /// untouched - no index-shifting arithmetic needed, since it's tracked by identity rather than
    /// position.
    /// </summary>
    internal static int? ComputeActiveEntryIdAfterRemoval(
        int? activeEntryId,
        int removedEntryId,
        int removedIndex,
        IReadOnlyList<int> remainingEntryIds
    ) =>
        activeEntryId != removedEntryId ? activeEntryId
        : remainingEntryIds.Count == 0 ? null
        : remainingEntryIds[Math.Min(removedIndex, remainingEntryIds.Count - 1)];

    /// <summary>
    /// Removes the rotation entry at <paramref name="index"/> - see
    /// <see cref="ComputeActiveEntryIdAfterRemoval"/> for how <see cref="ActiveEntryId"/> is
    /// affected.
    /// </summary>
    public void RemoveRotationEntry(int index)
    {
        var removedEntry = RotationEntries[index];
        RotationEntries.RemoveAt(index);

        ActiveEntryId = ComputeActiveEntryIdAfterRemoval(
            ActiveEntryId,
            removedEntry.Id,
            index,
            [.. RotationEntries.Select(e => e.Id)]
        );
    }

    /// <summary>
    /// Moves the rotation entry at <paramref name="index"/> by <paramref name="delta"/> positions
    /// (e.g. -1/+1 for up/down), doing nothing if that would move it out of bounds.
    /// <see cref="ActiveEntryId"/> needs no adjustment here - it tracks the active entry's
    /// identity, not its position, so it's unaffected by reordering.
    /// </summary>
    public void MoveRotationEntry(int index, int delta)
    {
        var newIndex = index + delta;
        if (newIndex < 0 || newIndex >= RotationEntries.Count)
        {
            return;
        }

        (RotationEntries[index], RotationEntries[newIndex]) = (
            RotationEntries[newIndex],
            RotationEntries[index]
        );
    }

    /// <summary>
    /// Pure decision behind <see cref="ComputeRoundRobinActiveEntryId"/>'s advance step, split out
    /// so it's unit-testable without a live job: the id one position after
    /// <paramref name="activeEntryId"/>'s current spot in
    /// <paramref name="entryIds"/>, cycling back to the start after the last one. Falls back to
    /// the first id if <paramref name="activeEntryId"/> doesn't match anything in
    /// <paramref name="entryIds"/> (shouldn't normally happen, but safer than throwing).
    /// </summary>
    internal static int ComputeNextActiveEntryId(IReadOnlyList<int> entryIds, int? activeEntryId)
    {
        var currentIndex =
            activeEntryId == null ? -1 : entryIds.ToList().IndexOf(activeEntryId.Value);
        var nextIndex = (currentIndex + 1) % entryIds.Count;
        return entryIds[nextIndex];
    }

    /// <summary>
    /// Pure decision behind <see cref="RotationMode.Priority"/>'s active-entry selection, split out
    /// so it's unit-testable without a live job: the first entry (by list position - i.e. by
    /// priority) whose own latch is enabled per hysteresis (<see cref="LatchMode.BelowLowerBound"/>
    /// or <see cref="LatchMode.BetweenBoundsEnabled"/> - matching <see
    /// cref="Trigger_Hysteresis.State"/>). <see cref="LatchMode.BetweenBoundsDisabled"/> must be
    /// treated the same as <see cref="LatchMode.AboveUpperBound"/> here: it means the entry went
    /// above its upper bound and hasn't dropped below its lower bound since, so it's still
    /// disallowed even though the count is currently under the upper bound again.
    /// Falls back to <paramref name="previousActiveEntryId"/> if every entry is already satisfied
    /// (nothing needs growing right now, so there's no reason to change what's active), or the
    /// first entry in <paramref name="entries"/> if there was no previous active entry either.
    /// </summary>
    internal static int? ComputePriorityActiveEntryId(
        IReadOnlyList<(int Id, LatchMode Latch)> entries,
        int? previousActiveEntryId
    )
    {
        foreach (var (id, latch) in entries)
        {
            if (latch is LatchMode.BelowLowerBound or LatchMode.BetweenBoundsEnabled)
            {
                return id;
            }
        }

        return previousActiveEntryId ?? entries.Select(e => (int?)e.Id).FirstOrDefault();
    }

    /// <summary>
    /// Pure decision behind <see cref="RotationMode.RoundRobin"/>'s active-entry selection, split
    /// out (same reasoning as <see cref="ComputePriorityActiveEntryId"/>) so it's unit-testable
    /// without a live job: stays on <paramref name="activeEntryId"/> unless its own latch just
    /// made a fresh transition into <see cref="LatchMode.AboveUpperBound"/> this cycle (see
    /// <see cref="Trigger_Hysteresis.ShouldAdvanceRotation"/>) - <paramref name="previousActiveLatch"/>
    /// is what that entry's latch was *before* this cycle's update, so a latch that was already
    /// sitting at <see cref="LatchMode.AboveUpperBound"/> last cycle doesn't re-trigger an advance
    /// every cycle thereafter.
    /// </summary>
    internal static int? ComputeRoundRobinActiveEntryId(
        IReadOnlyList<(int Id, LatchMode Latch)> entries,
        int? activeEntryId,
        LatchMode? previousActiveLatch
    )
    {
        if (previousActiveLatch is not { } previous || activeEntryId is not { } activeId)
        {
            return activeEntryId;
        }

        var currentLatch = entries.FirstOrDefault(e => e.Id == activeId).Latch;
        return Trigger_Hysteresis.ShouldAdvanceRotation(previous, currentLatch, entries.Count)
            ? ComputeNextActiveEntryId([.. entries.Select(e => e.Id)], activeId)
            : activeEntryId;
    }

    /// <summary>
    /// Dispatches to <see cref="ComputeRoundRobinActiveEntryId"/> or
    /// <see cref="ComputePriorityActiveEntryId"/> according to <paramref name="mode"/> - the single
    /// entry point <see cref="Trigger_Hysteresis.ComputeCycleUpdate"/> uses, so the two modes'
    /// selection logic lives in exactly one place each.
    /// </summary>
    internal static int? ComputeNewActiveEntryId(
        RotationMode mode,
        IReadOnlyList<(int Id, LatchMode Latch)> entries,
        int? previousActiveEntryId,
        LatchMode? previousActiveLatch
    ) =>
        mode switch
        {
            RotationMode.RoundRobin => ComputeRoundRobinActiveEntryId(
                entries,
                previousActiveEntryId,
                previousActiveLatch
            ),
            RotationMode.Priority => ComputePriorityActiveEntryId(entries, previousActiveEntryId),
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };

    private string? _tmpGrowerAreaLabel;

    /// <summary>
    /// Pure decision logic behind <see cref="IsGrowerInScope(IPlantToGrowSettable)"/>, split out
    /// so it's unit-testable without a live map/grower (mirrors
    /// <c>ManagerJob_Production.IsWorkTableInScope(mode, inArea, isSpecificallySelected)</c>).
    /// </summary>
    internal static bool IsGrowerInScope(
        GrowerAssignmentMode mode,
        bool inArea,
        bool isSpecificallySelected
    ) =>
        mode switch
        {
            GrowerAssignmentMode.All => true,
            GrowerAssignmentMode.Area => inArea,
            GrowerAssignmentMode.Specific => isSpecificallySelected,
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };

    /// <summary>
    /// Whether <paramref name="grower"/> matches this job's configured scope (assignment mode +
    /// area/specific selection), <b>ignoring</b> whether another job already claims it — see
    /// <see cref="ScopeEligiblePlantGrowers"/>/<see cref="ManagedGrowers"/> for that.
    /// </summary>
    public bool IsGrowerInScope(IPlantToGrowSettable grower) =>
        IsGrowerInScope(
            AssignmentMode,
            AssignmentMode == GrowerAssignmentMode.Area
                && GrowerArea != null
                && grower.Cells.Any(cell => GrowerArea[cell]) != InvertGrowerArea,
            AssignmentMode == GrowerAssignmentMode.Specific
                && grower switch
                {
                    Zone zone => SpecificGrowingZones.Contains(zone),
                    Building_PlantGrower building => SpecificPlantGrowerBuildings.Contains(
                        building
                    ),
                    _ => false,
                }
        );

    /// <summary>
    /// Every grower on the map that matches this job's own scope, regardless of whether another
    /// job already claims it. Used both to resolve <see cref="ManagedGrowers"/> (this job's scope
    /// minus growers claimed by earlier jobs) and to populate the "Specific" mode picker, where
    /// growers claimed by another job are shown but disabled (see
    /// <c>ManagerTab_FarmingHysteresis.DrawSpecificGrowers</c>).
    /// </summary>
    public IEnumerable<IPlantToGrowSettable> ScopeEligiblePlantGrowers =>
        FarmingHysteresisControlDef.AllControlledPlantGrowers(Manager.map).Where(IsGrowerInScope);

    /// <summary>
    /// This job's actual controlled growers: <see cref="ScopeEligiblePlantGrowers"/> minus any
    /// grower already claimed by a <see cref="ManagerJob_FarmingHysteresis"/> earlier in
    /// <c>Manager.JobTracker</c>'s job order. Ties are resolved first-claim-wins by creation
    /// order — this is what makes ownership exclusive: a grower can never end up in more than
    /// one job's <see cref="ManagedGrowers"/> at a time.
    /// </summary>
    public IReadOnlyList<IPlantToGrowSettable> ManagedGrowers
    {
        get
        {
            var claimedByEarlierJobs = Manager
                .JobTracker.JobsOfType<ManagerJob_FarmingHysteresis>()
                .TakeWhile(job => job != this)
                .SelectMany(job => job.ScopeEligiblePlantGrowers)
                .ToHashSet();
            return
            [
                .. ScopeEligiblePlantGrowers.Where(grower =>
                    !claimedByEarlierJobs.Contains(grower)
                ),
            ];
        }
    }

    /// <summary>
    /// Finds the <see cref="ManagerJob_FarmingHysteresis"/> (if any) that currently controls
    /// <paramref name="grower"/> — the first job (in job-tracker order) whose own scope includes
    /// it. Used to disable/annotate already-claimed growers in the "Specific" picker, and could
    /// similarly show a "managed by" indicator anywhere the grower's own UI would otherwise let
    /// the player toggle hysteresis on it directly.
    /// </summary>
    public static ManagerJob_FarmingHysteresis? FindOwningJob(
        Manager manager,
        IPlantToGrowSettable grower
    ) =>
        manager
            .JobTracker.JobsOfType<ManagerJob_FarmingHysteresis>()
            .FirstOrDefault(job => job.ScopeEligiblePlantGrowers.Contains(grower));

    /// <summary>
    /// A friendly, user-facing label for <paramref name="grower"/>. <see cref="Thing.ToString"/>
    /// (used by <see cref="Building_PlantGrower"/>) returns the internal <c>ThingID</c> rather
    /// than anything player-facing, so this dispatches per concrete type instead:
    /// <see cref="Zone"/> already overrides <c>ToString()</c> to return its label, but
    /// <see cref="Thing"/>s need <see cref="Thing.LabelCap"/>.
    /// </summary>
    internal static string GrowerLabel(IPlantToGrowSettable grower) =>
        grower switch
        {
            Zone zone => zone.ToString(),
            Thing thing => thing.LabelCap,
            _ => grower.GetType().Name,
        };

    public override IEnumerable<string> Targets => ManagedGrowers.Select(GrowerLabel);

    /// <summary>
    /// The plants every one of this job's <see cref="ManagedGrowers"/> can currently grow -
    /// reuses vanilla's own grower/plant compatibility checks (the same ones behind the "Plant: X"
    /// gizmo's floating menu) rather than reimplementing sow-tag/research/darkness filtering:
    /// <see cref="PlantUtility.ValidPlantTypesForGrowers"/> for the per-grower sow-tag
    /// intersection, <see cref="Command_SetPlantToGrow.IsPlantAvailable"/> for research/darkness/
    /// wild-only gating. Empty when there are no managed growers, or when they share no common
    /// growable plant (e.g. mixing a hydroponics basin with an aquatic growing zone).
    /// </summary>
    /// <remarks>
    /// Also excludes plants with no <c>harvestedThingDef</c> (e.g. purely decorative plants like
    /// roses) - there's nothing for <see cref="Trigger_Hysteresis"/> to ever count for those, so
    /// the job could never do anything but sit disabled. The legacy per-grower engine only
    /// noticed this after the fact and disabled itself (see
    /// <c>FarmingHysteresis.DisabledDueToMissingHarvestedThingDef</c>); filtering it out here
    /// means the player is never offered a choice that can't work in the first place.
    /// </remarks>
    public IEnumerable<ThingDef> ValidTargetPlants
    {
        get
        {
            var growers = ManagedGrowers;
            if (growers.Count == 0)
            {
                yield break;
            }

            foreach (var plantDef in PlantUtility.ValidPlantTypesForGrowers([.. growers]))
            {
                if (
                    IsValidTargetPlantCandidate(
                        plantDef,
                        def => Command_SetPlantToGrow.IsPlantAvailable(def, Manager.map)
                    )
                )
                {
                    yield return plantDef;
                }
            }
        }
    }

    /// <summary>
    /// Pure decision logic behind <see cref="ValidTargetPlants"/>'s per-candidate filter, split
    /// out so it's unit-testable without a live map (<paramref name="isPlantAvailable"/> stands in
    /// for <see cref="Command_SetPlantToGrow.IsPlantAvailable"/>, which needs one).
    /// </summary>
    internal static bool IsValidTargetPlantCandidate(
        ThingDef plantDef,
        Func<ThingDef, bool> isPlantAvailable
    ) => plantDef.plant.harvestedThingDef != null && isPlantAvailable(plantDef);

    public override WorkTypeDef WorkTypeDef => WorkTypeDefOf.Growing;

    public override void CleanUp(ManagerLog? jobLog = null) { }

    /// <summary>
    /// How often <see cref="Tick"/> reasserts sow/harvest state, in game ticks - matches
    /// <c>DefaultHysteresisController</c>'s own per-tick cadence for the legacy engine, so a
    /// soft-dependency veto (e.g. Smart Farming's "No petty jobs" - see
    /// <see cref="FarmingHysteresisMod.AllowSowVeto"/>) gets reasserted this often too, rather
    /// than waiting for this job's own (potentially much less frequent) work cycle.
    /// </summary>
    private const int TickInterval = 250;

    /// <summary>
    /// Recomputes whether <paramref name="grower"/> has a not-yet-transitioned leftover plant from
    /// a previous rotation entry (see <see cref="GrowerHasLeftoverPlants"/>), clears it immediately
    /// if <paramref name="switchMode"/> calls for that, then reapplies this job's current
    /// hysteresis latch state (<see cref="Trigger_Hysteresis.State"/>) to
    /// <paramref name="grower"/>'s allow-sow/allow-harvest gating via
    /// <see cref="PlantToGrowSettableExtensions.SetHysteresisControlState"/> - which also consults
    /// FH core's <see cref="FarmingHysteresisMod.AllowSowVeto"/> hook, so a soft-dependency veto is
    /// applied here too. Shared by <see cref="ExecuteJobDataCoroutine"/> (once per job cycle) and
    /// <see cref="Tick"/> (every <see cref="TickInterval"/> ticks) so a veto doesn't have to wait
    /// for the next job cycle to take effect.
    /// </summary>
    private void ReassertSowState(
        IPlantToGrowSettable grower,
        ThingDef targetPlantDef,
        HashSet<ThingDef> rotationPlantDefs,
        RotationSwitchMode switchMode
    )
    {
        var hasLeftoverPlants = GrowerHasLeftoverPlants(
            grower.Cells.Select(c => c.GetPlant(grower.Map)?.def),
            targetPlantDef,
            rotationPlantDefs
        );
        if (hasLeftoverPlants && switchMode == RotationSwitchMode.SwitchImmediately)
        {
            ForceClearLeftoverPlants(grower, targetPlantDef);
        }

        grower.SetHysteresisControlState(
            HysteresisMode,
            HysteresisTrigger.State,
            forceHarvestEnabled: hasLeftoverPlants
        );
    }

    /// <summary>
    /// Reasserts this job's current hysteresis latch state onto every managed grower every
    /// <see cref="TickInterval"/> ticks (see <see cref="ReassertSowState"/>) - independent of this
    /// job's own work-cycle cadence, which can otherwise be far less frequent. A no-op while this
    /// job is dormant (<see cref="IsManaged"/> false, e.g. the legacy engine is the active
    /// controller instead) or before a target plant has been chosen.
    /// </summary>
    public override void Tick()
    {
        if (!IsManaged || Find.TickManager.TicksGame % TickInterval != 0)
        {
            return;
        }

        var targetPlantDef = TargetPlantDef;
        if (targetPlantDef == null)
        {
            return;
        }

        HashSet<ThingDef> rotationPlantDefs =
        [
            .. RotationEntries.Select(e => e.PlantDef).OfType<ThingDef>(),
        ];

        foreach (var grower in ManagedGrowers)
        {
            ReassertSowState(grower, targetPlantDef, rotationPlantDefs, SwitchMode);
        }
    }

    protected override Coroutine GatherJobDataCoroutine(ManagerLog jobLog, AnyBoxed<WorkData?> data)
    {
        var growers = ManagedGrowers;
        if (growers.Count == 0)
        {
            JobState = ManagerJobState.Completed;
            yield break;
        }

        var targetPlantDef = TargetPlantDef;
        if (targetPlantDef == null || !ValidTargetPlants.Contains(targetPlantDef))
        {
            // No target plant chosen yet, or the previously chosen one is no longer growable by
            // every currently-managed grower (e.g. the scope changed) - nothing to push down or
            // gate off of until the player (re-)picks one in the "Target plant" section.
            JobState = ManagerJobState.Completed;
            yield break;
        }

        // Gather must not change anything in the game - see the base ManagerJob<TWorkData>'s own
        // doc comments on GatherJobDataCoroutine/ExecuteJobDataCoroutine and JobTracker's matching
        // Gather/Execute pairing. This only computes what this cycle's latch/rotation update would
        // be (see Trigger_Hysteresis.ComputeCycleUpdate); nothing is actually written until
        // ExecuteJobDataCoroutine applies it, so a gather whose result never reaches execute (job
        // errors out, gets suspended, etc.) never leaves persistent state half-advanced.
        var cycleUpdate = HysteresisTrigger.ComputeCycleUpdate();

        JobState = ManagerJobState.Active;

        data.Value = new WorkData(growers, cycleUpdate, SwitchMode);
    }

    /// <summary>
    /// Whether any of <paramref name="standingPlantDefs"/> (a grower's currently-standing plants,
    /// one per cell) belongs to a crop this job's rotation has grown - i.e. one of
    /// <paramref name="rotationPlantDefs"/> - other than <paramref name="targetPlantDef"/>, meaning
    /// the grower hasn't fully transitioned to the active rotation entry yet. A standing plant
    /// outside <paramref name="rotationPlantDefs"/> (e.g. a wild filler plant sprouting in a cell
    /// this job hasn't gotten around to sowing yet) is never a rotation leftover, regardless of its
    /// def. Split out as a pure function (fed by a thin live wrapper in
    /// <see cref="ExecuteJobDataCoroutine"/>) so it's unit-testable without a live map/grower.
    /// </summary>
    internal static bool GrowerHasLeftoverPlants(
        IEnumerable<ThingDef?> standingPlantDefs,
        ThingDef targetPlantDef,
        HashSet<ThingDef> rotationPlantDefs
    ) => standingPlantDefs.Any(def => IsLeftoverPlant(def, targetPlantDef, rotationPlantDefs));

    /// <summary>
    /// Whether a single standing plant def is itself a rotation leftover - the per-plant test
    /// backing <see cref="GrowerHasLeftoverPlants"/> above, and also used directly wherever a cut
    /// decision is made about one specific plant (see
    /// <see cref="CmrHysteresisController.ShouldProtectLeftoverFromCut"/>) rather than about a
    /// grower as a whole - a grower having a leftover somewhere doesn't mean every plant on it is
    /// one.
    /// </summary>
    internal static bool IsLeftoverPlant(
        ThingDef? plantDef,
        ThingDef targetPlantDef,
        HashSet<ThingDef> rotationPlantDefs
    ) => plantDef != null && plantDef != targetPlantDef && rotationPlantDefs.Contains(plantDef);

    /// <summary>
    /// Force-clears <paramref name="grower"/>'s not-yet-ripe leftover plants (any standing plant
    /// whose def isn't <paramref name="targetPlantDef"/> and isn't <see cref="Plant.HarvestableNow"/>
    /// yet) by designating them for cutting - <see cref="RotationSwitchMode.SwitchImmediately"/>'s
    /// "don't wait for growth to finish" behavior, losing their eventual yield in exchange for an
    /// instant cutover. Already-ripe leftovers are deliberately left alone here - they're collected
    /// for free via the <c>forceHarvestEnabled</c> override in <see cref="ExecuteJobDataCoroutine"/>,
    /// which applies regardless of switch mode. Mirrors CMR's own Forestry job's clear-cut pattern
    /// (<c>ManagerJob_Forestry</c>, <c>DesignationDefOf.CutPlant</c>).
    /// </summary>
    private static void ForceClearLeftoverPlants(
        IPlantToGrowSettable grower,
        ThingDef targetPlantDef
    )
    {
        var map = grower.Map;
        foreach (var cell in grower.Cells)
        {
            var plant = cell.GetPlant(map);
            if (plant == null || plant.def == targetPlantDef || plant.HarvestableNow)
            {
                continue;
            }

            if (map.designationManager.DesignationOn(plant) == null)
            {
                map.designationManager.AddDesignation(
                    new Designation(plant, DesignationDefOf.CutPlant)
                );
            }
        }
    }

    protected override Coroutine ExecuteJobDataCoroutine(
        ManagerLog jobLog,
        WorkData data,
        Boxed<bool> workDone
    )
    {
        // The only place latch state/rotation-advance is ever actually written - see
        // Trigger_Hysteresis.ApplyCycleUpdate's own doc comment. TargetPlantDef/State are read
        // fresh afterward, since applying the update may have changed the active entry.
        HysteresisTrigger.ApplyCycleUpdate(data.CycleUpdate);
        jobLog.AddDetail(
            "FarmingHysteresis.CMR.Logs.LatchState".Translate(HysteresisTrigger.StatusTooltip)
        );

        var targetPlantDef = TargetPlantDef!;
        var enabled = HysteresisTrigger.State;

        HashSet<ThingDef> rotationPlantDefs =
        [
            .. RotationEntries.Select(e => e.PlantDef).OfType<ThingDef>(),
        ];

        RecomputeSowBudget();

        foreach (var grower in data.Growers)
        {
            if (grower.GetPlantDefToGrow() != targetPlantDef)
            {
                grower.SetPlantDefToGrow(targetPlantDef);
                workDone.Value = true;
                jobLog.AddDetail(
                    "FarmingHysteresis.CMR.Logs.GrowerPlantSet".Translate(
                        GrowerLabel(grower),
                        targetPlantDef.LabelCap
                    )
                );
            }

            var beforeSow = grower.GetAllowSow();
            var beforeHarvest = grower.GetAllowHarvest();

            // Regardless of switch mode, a leftover plant from a crop this job has already
            // rotated away from must never be stranded unharvested - that would permanently
            // occupy its cell and stall the rotation.
            //
            // In WaitForGrowthToFinish mode specifically, a leftover plant's own cell must stay
            // protected from vanilla's sow work-giver, which otherwise cuts down any occupying
            // plant that isn't the wanted def to make room, regardless of maturity, whenever the
            // zone's "allow cutting" is on. This is handled live by
            // CmrHysteresisController.ShouldProtectLeftoverFromCut (checked by
            // WorkGiver_GrowerSow_JobOnCell) rather than by disallowing sow on the whole grower -
            // that would also block sowing into cells that are already clear.
            ReassertSowState(grower, targetPlantDef, rotationPlantDefs, data.SwitchMode);

            if (grower.GetAllowSow() != beforeSow || grower.GetAllowHarvest() != beforeHarvest)
            {
                workDone.Value = true;
                jobLog.AddDetail(
                    "FarmingHysteresis.CMR.Logs.GrowerStateChanged".Translate(
                        GrowerLabel(grower),
                        enabled
                            ? "FarmingHysteresis.CMR.Logs.Enabled".Translate()
                            : "FarmingHysteresis.CMR.Logs.Disabled".Translate()
                    )
                );
            }
        }
        yield break;
    }

    /// <summary>
    /// Pure decision logic behind the <see cref="SpecificGrowingZones"/> post-load cleanup, split
    /// out so it's unit-testable without a live <see cref="Zone"/> — see
    /// <see cref="ShouldRemoveUnresolvedPlantGrowerBuilding"/> for the building counterpart. A
    /// scribed <c>LookMode.Reference</c> entry resolves to <see langword="null"/> whenever the
    /// referenced zone was already gone at save time, which is ordinary (not corruption), so it
    /// must be pruned rather than dereferenced.
    /// </summary>
    internal static bool ShouldRemoveUnresolvedGrowingZone(bool isNull, int cellCount) =>
        isNull || cellCount == 0;

    /// <summary>
    /// Pure decision logic behind the <see cref="SpecificPlantGrowerBuildings"/> post-load
    /// cleanup — see <see cref="ShouldRemoveUnresolvedGrowingZone"/>.
    /// </summary>
    internal static bool ShouldRemoveUnresolvedPlantGrowerBuilding(
        bool isNull,
        bool destroyed,
        bool spawned
    ) => isNull || destroyed || !spawned;

    /// <summary>
    /// Pure decision logic behind <see cref="ExposeData"/>'s one-time <see cref="HysteresisMode"/>
    /// migration: a job whose scribed data already carried a <see cref="HasMigratedHysteresisMode"/>
    /// node (i.e. it was saved after this field existed) keeps whatever
    /// <see cref="HysteresisMode"/> it loaded with; one saved before this field existed - which
    /// loads <paramref name="hasMigratedHysteresisMode"/> as <see langword="false"/>, since that's
    /// <see cref="ExposeData"/>'s scribed default - instead picks up
    /// <paramref name="legacyHysteresisMode"/>, the old global mod setting this job actually used
    /// to be controlled by.
    /// </summary>
    internal static HysteresisMode ResolveHysteresisModeAfterLoad(
        bool hasMigratedHysteresisMode,
        HysteresisMode loadedHysteresisMode,
        HysteresisMode legacyHysteresisMode
    ) => hasMigratedHysteresisMode ? loadedHysteresisMode : legacyHysteresisMode;

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref AssignmentMode, "assignmentMode", GrowerAssignmentMode.All);
        Scribe_Values.Look(ref InvertGrowerArea, "invertGrowerArea");
        Scribe_Collections.Look(ref RotationEntries, "rotationEntries", LookMode.Deep);
        Scribe_Values.Look(ref ActiveEntryId, "activeEntryId");
        Scribe_Values.Look(ref _nextEntryId, "nextEntryId", 1);
        Scribe_Values.Look(ref SwitchMode, "switchMode", RotationSwitchMode.WaitForGrowthToFinish);
        Scribe_Values.Look(ref Mode, "rotationMode", RotationMode.Priority);
        Scribe_Values.Look(ref HysteresisMode, "hysteresisMode", HysteresisMode.Sowing);
        Scribe_Values.Look(ref LimitSowingToComputedNeed, "limitSowingToComputedNeed");
        Scribe_Values.Look(ref SowingSafetyMultiplier, "sowingSafetyMultiplier", 1.2f);
        // Defaulted here to false, not the field initializer's true, precisely so a save from
        // before this field existed - which has no scribed node for it - loads as false rather
        // than keeping the in-memory default; see HasMigratedHysteresisMode's own doc comment.
        Scribe_Values.Look(ref HasMigratedHysteresisMode, "hasMigratedHysteresisMode", false);
        RotationEntries ??= [];

        if (Manager.ScribeSameMapData)
        {
            Scribe_References.Look(ref GrowerArea, "growerArea");
            Scribe_Collections.Look(
                ref SpecificGrowingZones,
                "specificGrowingZones",
                LookMode.Reference
            );
            Scribe_Collections.Look(
                ref SpecificPlantGrowerBuildings,
                "specificPlantGrowerBuildings",
                LookMode.Reference
            );
            Scribe_Collections.Look(ref _cellSowPlan, "cellSowPlan", LookMode.Value, LookMode.Def);
        }
        else
        {
            // Cross-map/export copies can't carry area or specific-grower references along
            // (same reasoning as ManagerJob_Production.SpecificWorkbenches) — deliberately not
            // scribed, and lost the same way.
            Utilities.Scribe_AreaByLabel(
                ref GrowerArea,
                ref _tmpGrowerAreaLabel,
                "growerArea",
                Manager.map.areaManager
            );
        }

        SpecificGrowingZones ??= [];
        SpecificPlantGrowerBuildings ??= [];

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            HysteresisMode = ResolveHysteresisModeAfterLoad(
                HasMigratedHysteresisMode,
                HysteresisMode,
                FarmingHysteresisMod.Settings.HysteresisMode
            );
            HasMigratedHysteresisMode = true;

            _ = SpecificGrowingZones.RemoveWhere(zone =>
                ShouldRemoveUnresolvedGrowingZone(zone == null, zone?.Cells.Count ?? 0)
            );
            _ = SpecificPlantGrowerBuildings.RemoveWhere(building =>
                ShouldRemoveUnresolvedPlantGrowerBuilding(
                    building == null,
                    building?.Destroyed ?? false,
                    building?.Spawned ?? false
                )
            );

            foreach (var entry in RotationEntries)
            {
                // Stockpile references aren't scribable directly - each entry resolves its own
                // from its scribed label.
                entry.ResolveStockpileReference(Manager.map);
            }
        }
    }

    /// <summary>
    /// Pure decision logic behind <see cref="Notify_AreaRemoved"/>.
    /// </summary>
    internal static bool IsRemovedArea(Area? growerArea, Area removedArea) =>
        growerArea == removedArea;

    protected override void Notify_AreaRemoved(Area area)
    {
        base.Notify_AreaRemoved(area);
        if (IsRemovedArea(GrowerArea, area))
        {
            GrowerArea = null;
        }
    }

    /// <summary>
    /// Called by <c>Patch.Verse_Zone_Deregister</c> for every zone deregistration on this job's
    /// map. Without this, a zone deleted mid-play staysin <see cref="SpecificGrowingZones"/>
    /// until the next save/load cycle, which is what <see cref="ShouldRemoveUnresolvedGrowingZone"/>
    /// otherwise has to clean up after the fact.
    /// </summary>
    internal void Notify_ZoneRemoved(Zone zone) => SpecificGrowingZones.Remove(zone);
}
