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

internal sealed class ManagerJob_FarmingHysteresis
    : ManagerJob<ManagerSettings_FarmingHysteresis, ManagerJob_FarmingHysteresis.WorkData>
{
    /// <summary>
    /// The gather phase's verdict: the growers to apply it to, and the single enabled/disabled
    /// state (from <see cref="Trigger_Hysteresis.State"/>) to apply uniformly to all of them.
    /// </summary>
    internal sealed class WorkData(
        IReadOnlyList<IPlantToGrowSettable> growers,
        bool enabled,
        ThingDef targetPlantDef
    )
    {
        public IReadOnlyList<IPlantToGrowSettable> Growers { get; } = growers;
        public bool Enabled { get; } = enabled;
        public ThingDef TargetPlantDef { get; } = targetPlantDef;
    }

    /// <summary>
    /// Charts <see cref="Trigger_Hysteresis.TrackedThingCount"/> alongside its two bounds (see
    /// <c>Docs/CMRIntegrationRework.md</c>, Step 3) as three independent flat/varying lines -
    /// "stock", "lower bound", and "upper bound" - each chapter's "count" is simply that value
    /// each tick. None of them use CMR's target-line mechanism.
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
            // Forces a fresh recompute if the cache has expired, same reasoning as
            // Trigger_Hysteresis.StatusTooltip - the graph shouldn't lag behind bounds the
            // player just edited.
            _ = managerJob.HysteresisTrigger.State;

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
    /// managing the same growers - exactly the "mix" Design decision 2 rules out. The job's own
    /// config (scope, target plant, bounds) is untouched either way; it simply goes dormant.
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
    /// The single plant this job assigns to every grower it manages (see
    /// <see cref="ExecuteJobDataCoroutine"/>) - analogous to <c>ManagerJob_Production</c> picking
    /// a recipe for the workbenches it manages. Restricted to <see cref="ValidTargetPlants"/> so a
    /// job never ends up demanding a plant one of its growers can't actually grow.
    /// </summary>
    public ThingDef? TargetPlantDef;

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
            grower.Cells.Any(cell => GrowerArea?.ActiveCells.Contains(cell) ?? false)
                != InvertGrowerArea
                && GrowerArea != null,
            grower switch
            {
                Zone zone => SpecificGrowingZones.Contains(zone),
                Building_PlantGrower building => SpecificPlantGrowerBuildings.Contains(building),
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
    /// it. Used both to disable/annotate already-claimed growers in the "Specific" picker and,
    /// in later Step 2 work, to show a "managed by" indicator anywhere the grower's own UI would
    /// otherwise let the player toggle hysteresis on it directly.
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

        var enabled = HysteresisTrigger.State;

        JobState = ManagerJobState.Active;
        jobLog.AddDetail(
            "FarmingHysteresis.CMR.Logs.LatchState".Translate(HysteresisTrigger.StatusTooltip)
        );

        data.Value = new WorkData(growers, enabled, targetPlantDef);
    }

    protected override Coroutine ExecuteJobDataCoroutine(
        ManagerLog jobLog,
        WorkData data,
        Boxed<bool> workDone
    )
    {
        foreach (var grower in data.Growers)
        {
            if (grower.GetPlantDefToGrow() != data.TargetPlantDef)
            {
                grower.SetPlantDefToGrow(data.TargetPlantDef);
                workDone.Value = true;
                jobLog.AddDetail(
                    "FarmingHysteresis.CMR.Logs.GrowerPlantSet".Translate(
                        GrowerLabel(grower),
                        data.TargetPlantDef.LabelCap
                    )
                );
            }

            var beforeSow = grower.GetAllowSow();
            var beforeHarvest = grower.GetAllowHarvest();

            grower.SetHysteresisControlState(data.Enabled);

            if (grower.GetAllowSow() != beforeSow || grower.GetAllowHarvest() != beforeHarvest)
            {
                workDone.Value = true;
                jobLog.AddDetail(
                    "FarmingHysteresis.CMR.Logs.GrowerStateChanged".Translate(
                        GrowerLabel(grower),
                        data.Enabled
                            ? "FarmingHysteresis.CMR.Logs.Enabled".Translate()
                            : "FarmingHysteresis.CMR.Logs.Disabled".Translate()
                    )
                );
            }
        }
        yield break;
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref AssignmentMode, "assignmentMode", GrowerAssignmentMode.All);
        Scribe_Values.Look(ref InvertGrowerArea, "invertGrowerArea");
        Scribe_Defs.Look(ref TargetPlantDef, "targetPlantDef");

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
            _ = SpecificGrowingZones.RemoveWhere(zone => zone.Cells.Count == 0);
            _ = SpecificPlantGrowerBuildings.RemoveWhere(building =>
                building.Destroyed || !building.Spawned
            );
        }
    }
}
