using ColonyManagerRedux;
using FarmingHysteresis.Defs;

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

internal sealed class ManagerJob_FarmingHysteresis(Manager manager)
    : ManagerJob<ManagerSettings_FarmingHysteresis, ManagerJob_FarmingHysteresis.WorkData>(manager)
{
    // Placeholder gather->execute payload; a later slice of Step 2 replaces this with real
    // trigger/bounds evaluation. See Docs/CMRIntegrationRework.md.
    internal sealed class WorkData;

    public GrowerAssignmentMode AssignmentMode = GrowerAssignmentMode.All;
    public Area? GrowerArea;
    public bool InvertGrowerArea;
    public HashSet<Zone_Growing> SpecificGrowingZones = [];
    public HashSet<Building_PlantGrower> SpecificPlantGrowerBuildings = [];

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
            grower.Cells.Any(cell => GrowerArea?.ActiveCells.Contains(cell) ?? false) != InvertGrowerArea
                && GrowerArea != null,
            grower switch
            {
                Zone_Growing zone => SpecificGrowingZones.Contains(zone),
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
        FarmingHysteresisControlDef
            .AllControlledPlantGrowers(Manager.map)
            .Where(IsGrowerInScope);

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
                .. ScopeEligiblePlantGrowers.Where(grower => !claimedByEarlierJobs.Contains(grower)),
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

    public override WorkTypeDef WorkTypeDef => WorkTypeDefOf.Growing;

    public override void CleanUp(ManagerLog? jobLog = null) { }

    protected override Coroutine GatherJobDataCoroutine(ManagerLog jobLog, AnyBoxed<WorkData?> data)
    {
        yield break;
    }

    protected override Coroutine ExecuteJobDataCoroutine(
        ManagerLog jobLog,
        WorkData data,
        Boxed<bool> workDone
    )
    {
        yield break;
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref AssignmentMode, "assignmentMode", GrowerAssignmentMode.All);
        Scribe_Values.Look(ref InvertGrowerArea, "invertGrowerArea");

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
