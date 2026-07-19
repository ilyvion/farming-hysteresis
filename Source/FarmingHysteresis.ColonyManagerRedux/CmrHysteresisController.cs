namespace FarmingHysteresis.ColonyManagerRedux;

/// <summary>
/// Installed in place of <see cref="DefaultHysteresisController"/> whenever the player has
/// enabled "take over Farming Hysteresis control" in <see cref="ManagerSettings_FarmingHysteresis"/>
/// and no per-save <see cref="CmrMigrationGate"/> is suppressing it. Suppresses the mod's own UI
/// mod-wide and is what actually permits
/// <see cref="ManagerJob_FarmingHysteresis"/> jobs to act on their growers - this is only ever
/// installed when it's genuinely safe to do so (see <c>ManagerSettings_FarmingHysteresis.ApplyControllerState</c>).
/// </summary>
internal sealed class CmrHysteresisController : IHysteresisController
{
    public static CmrHysteresisController Instance { get; } = new();

    private CmrHysteresisController() { }

    public void Tick(Map map)
    {
        // Growers are driven by ManagerJob_FarmingHysteresis's own gather/execute coroutines
        // under CMR's own job-tracker ticking, not this map-tick hook.
    }

    public bool ShowGrowerUi => false;

    public bool ShowMainTab => false;
}
