using ColonyManagerRedux;

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

    /// <summary>
    /// Recomputed fresh from <paramref name="grower"/>'s current owning job (if any) rather than
    /// from any cached per-grower state, so a job being deleted, going dormant, or simply no
    /// longer including this grower in its scope stops the protection immediately - there's no
    /// stored flag left over that a removal path would need to remember to clear. Checked against
    /// <paramref name="plant"/> itself, not just whether the grower has a leftover somewhere -
    /// otherwise a single leftover cell would suppress cutting for every plant on the grower,
    /// including ones that were never part of the rotation.
    /// </summary>
    public bool ShouldProtectLeftoverFromCut(IPlantToGrowSettable grower, Plant plant)
    {
        var manager = Manager.For(grower.Map);
        var job = ManagerJob_FarmingHysteresis.FindOwningJob(manager, grower);
        if (job is not { IsManaged: true, SwitchMode: RotationSwitchMode.WaitForGrowthToFinish })
        {
            return false;
        }

        var targetPlantDef = job.TargetPlantDef;
        return targetPlantDef != null
            && ManagerJob_FarmingHysteresis.IsLeftoverPlant(
                plant.def,
                targetPlantDef,
                [.. job.RotationEntries.Select(e => e.PlantDef).OfType<ThingDef>()]
            );
    }

    public bool ShowGrowerUi => false;

    public bool ShowMainTab => false;
}
