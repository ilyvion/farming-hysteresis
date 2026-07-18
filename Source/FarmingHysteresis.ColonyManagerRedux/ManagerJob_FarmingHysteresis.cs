using ColonyManagerRedux;

namespace FarmingHysteresis.ColonyManagerRedux;

/// <summary>
/// Step 1 skeleton: can be created, deleted, and saved/loaded, but doesn't control any
/// growers yet. Real behavior lands in Step 2 (see <c>Docs/CMRIntegrationRework.md</c>).
/// </summary>
internal sealed class ManagerJob_FarmingHysteresis(Manager manager)
    : ManagerJob<ManagerSettings_FarmingHysteresis, ManagerJob_FarmingHysteresis.WorkData>(manager)
{
    // Placeholder gather->execute payload; Step 2 replaces this with real designation/bounds data.
    internal sealed class WorkData;

    public override IEnumerable<string> Targets => [];

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
}
