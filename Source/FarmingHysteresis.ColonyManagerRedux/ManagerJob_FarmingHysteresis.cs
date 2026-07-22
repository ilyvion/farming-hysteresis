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
    /// leftovers or leaves them to mature and harvest normally - see <see cref="RotationSwitchMode"/>.
    /// </summary>
    public RotationSwitchMode SwitchMode = RotationSwitchMode.WaitForGrowthToFinish;

    /// <summary>
    /// Which rotation semantics (see <see cref="RotationMode"/>) this job uses to pick
    /// <see cref="ActiveEntryId"/> each manager job cycle - defaults to
    /// <see cref="RotationMode.Priority"/> so an old save (or a save from before this field
    /// existed) keeps behaving exactly as it did before, with zero behavior change.
    /// </summary>
    public RotationMode Mode = RotationMode.Priority;

    /// <summary>
    /// The plant currently being pushed onto every grower this job manages (see
    /// <see cref="ExecuteJobDataCoroutine"/>) - <see cref="ActiveEntry"/>'s plant, or
    /// <see langword="null"/> if the list is empty (nothing configured yet).
    /// </summary>
    public ThingDef? TargetPlantDef => ActiveEntry?.PlantDef;

    /// <summary>
    /// Appends <paramref name="plantDef"/> as a new rotation entry, seeded with the mod's default
    /// bounds. The new entry syncs its own tracked filter to <paramref name="plantDef"/> itself
    /// (see <see cref="CropRotationEntry.PlantDef"/>'s setter) - no job-level resync needed now
    /// that tracked items live per entry rather than once per job. Becomes the active entry only
    /// if the list was previously empty (nothing else to have been active).
    /// </summary>
    public void AddRotationEntry(ThingDef plantDef)
    {
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
    /// one per cell) belongs to a crop other than <paramref name="targetPlantDef"/> - i.e. the
    /// grower hasn't fully transitioned to the active rotation entry yet. Split out as a pure
    /// function (fed by a thin live wrapper in <see cref="ExecuteJobDataCoroutine"/>) so it's
    /// unit-testable without a live map/grower.
    /// </summary>
    internal static bool GrowerHasLeftoverPlants(
        IEnumerable<ThingDef?> standingPlantDefs,
        ThingDef targetPlantDef
    ) => standingPlantDefs.Any(def => def != null && def != targetPlantDef);

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

            var hasLeftoverPlants = GrowerHasLeftoverPlants(
                grower.Cells.Select(c => c.GetPlant(grower.Map)?.def),
                targetPlantDef
            );
            if (hasLeftoverPlants && data.SwitchMode == RotationSwitchMode.SwitchImmediately)
            {
                ForceClearLeftoverPlants(grower, targetPlantDef);
            }

            var beforeSow = grower.GetAllowSow();
            var beforeHarvest = grower.GetAllowHarvest();

            // Regardless of switch mode, a leftover plant from a crop this job has already
            // rotated away from must never be stranded unharvested - that would permanently
            // occupy its cell and stall the rotation.
            grower.SetHysteresisControlState(enabled, forceHarvestEnabled: hasLeftoverPlants);

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
}
