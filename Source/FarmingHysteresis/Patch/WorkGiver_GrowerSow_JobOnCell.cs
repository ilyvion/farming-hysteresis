using Verse.AI;

namespace FarmingHysteresis.Patch;

/// <summary>
/// Stops vanilla's sow work-giver from cutting down a protected leftover plant (see
/// <see cref="IHysteresisController.ShouldProtectLeftoverFromCut"/>) to clear its cell for the
/// incoming crop. <see cref="WorkGiver_GrowerSow.JobOnCell"/> issues a plain
/// <see cref="JobDefOf.CutPlant"/> job on whatever plant occupies a cell that isn't the wanted
/// def, with no maturity check, whenever the zone's "allow cutting" is on - independent of this
/// mod's own control state. Also stops it from issuing a <see cref="JobDefOf.Sow"/> job on a cell
/// outside the active controller's own sow-cell budget (see
/// <see cref="IHysteresisController.IsCellSowAllowed"/>) - only a CMR crop rotation job with
/// "limit sowing to computed need" on ever restricts this - and, for a cell that survives that
/// check, retargets which plant the job actually sows there (see
/// <see cref="IHysteresisController.GetCellSowPlantOverride"/>) whenever that same cascade has
/// handed the cell to a later crop in the rotation instead of the grower's own configured plant.
/// <see cref="Job.plantDefToSow"/> is set fresh per job by <see cref="WorkGiver_GrowerSow.JobOnCell"/>
/// itself rather than read live from the grower once sowing starts, so overriding it here is
/// enough - nothing downstream ever re-derives it from the grower's own plant.
/// </summary>
[HarmonyPatch(typeof(WorkGiver_GrowerSow), nameof(WorkGiver_GrowerSow.JobOnCell))]
internal static class WorkGiver_GrowerSow_JobOnCell
{
    private static void Postfix(Pawn pawn, IntVec3 c, ref Job? __result)
    {
        if (__result is not { def: var jobDef } job)
        {
            return;
        }

        if (job.targetA.Thing is Plant plant)
        {
            var cutGrower = plant.Position.GetPlantToGrowSettable(plant.Map);
            var protectLeftoverFromCut =
                cutGrower != null
                && FarmingHysteresisMod.HysteresisController.ShouldProtectLeftoverFromCut(
                    cutGrower,
                    plant
                );
            if (ShouldSuppressCut(jobDef, protectLeftoverFromCut))
            {
                __result = null;
                return;
            }
        }

        var sowGrower = c.GetPlantToGrowSettable(pawn.Map);
        var cellSowAllowed =
            sowGrower == null
            || FarmingHysteresisMod.HysteresisController.IsCellSowAllowed(sowGrower, c);
        if (ShouldSuppressSow(jobDef, cellSowAllowed))
        {
            __result = null;
            return;
        }

        if (sowGrower != null)
        {
            var plantOverride = FarmingHysteresisMod.HysteresisController.GetCellSowPlantOverride(
                sowGrower,
                c
            );
            if (ShouldApplySowPlantOverride(jobDef, plantOverride))
            {
                job.plantDefToSow = plantOverride;
            }
        }
    }

    /// <summary>Pure decision logic behind the postfix: only a cut job on a protected leftover's cell is ever suppressed.</summary>
    internal static bool ShouldSuppressCut(JobDef jobDef, bool protectLeftoverFromCut) =>
        jobDef == JobDefOf.CutPlant && protectLeftoverFromCut;

    /// <summary>Pure decision logic behind the postfix: only a sow job on a cell outside the current sow-cell budget is ever suppressed.</summary>
    internal static bool ShouldSuppressSow(JobDef jobDef, bool cellSowAllowed) =>
        jobDef == JobDefOf.Sow && !cellSowAllowed;

    /// <summary>Pure decision logic behind the postfix: only a surviving sow job with an actual override ever has its plant retargeted.</summary>
    internal static bool ShouldApplySowPlantOverride(JobDef jobDef, ThingDef? plantOverride) =>
        jobDef == JobDefOf.Sow && plantOverride != null;
}
