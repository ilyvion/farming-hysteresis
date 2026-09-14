using Verse.AI;

namespace FarmingHysteresis.Patch;

/// <summary>
/// Stops vanilla's sow work-giver from cutting down a protected leftover plant (see
/// <see cref="IHysteresisController.ShouldProtectLeftoverFromCut"/>) to clear its cell for the
/// incoming crop. <see cref="WorkGiver_GrowerSow.JobOnCell"/> issues a plain
/// <see cref="JobDefOf.CutPlant"/> job on whatever plant occupies a cell that isn't the wanted
/// def, with no maturity check, whenever the zone's "allow cutting" is on - independent of this
/// mod's own control state.
/// </summary>
[HarmonyPatch(typeof(WorkGiver_GrowerSow), nameof(WorkGiver_GrowerSow.JobOnCell))]
internal static class WorkGiver_GrowerSow_JobOnCell
{
    private static void Postfix(ref Job? __result)
    {
        if (__result is not { def: var jobDef } job || job.targetA.Thing is not Plant plant)
        {
            return;
        }

        var grower = plant.Position.GetPlantToGrowSettable(plant.Map);
        var protectLeftoverFromCut =
            grower != null
            && FarmingHysteresisMod.HysteresisController.ShouldProtectLeftoverFromCut(
                grower,
                plant
            );
        if (ShouldSuppressCut(jobDef, protectLeftoverFromCut))
        {
            __result = null;
        }
    }

    /// <summary>Pure decision logic behind the postfix: only a cut job on a protected leftover's cell is ever suppressed.</summary>
    internal static bool ShouldSuppressCut(JobDef jobDef, bool protectLeftoverFromCut) =>
        jobDef == JobDefOf.CutPlant && protectLeftoverFromCut;
}
