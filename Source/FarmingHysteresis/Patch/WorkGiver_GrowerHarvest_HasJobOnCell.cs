using FarmingHysteresis.Extensions;

namespace FarmingHysteresis.Patch;

[HarmonyPatch(typeof(WorkGiver_GrowerHarvest), nameof(WorkGiver_GrowerHarvest.HasJobOnCell))]
internal static class WorkGiver_GrowerHarvest_HasJobOnCell
{
    private static void Postfix(ref Pawn pawn, ref IntVec3 c, ref bool __result)
    {
        if (!__result)
        {
            // Don't mess up the game's other reasons for disallowing harvesting.
            return;
        }

        if (c.GetFirstBuilding(pawn.Map) is Building_PlantGrower buildingPlantGrower)
        {
            __result = ComputeResultFor(buildingPlantGrower, __result);
        }
        else if (c.GetZone(pawn.Map) is Zone_Growing zoneGrowing)
        {
            __result = ComputeResultFor(zoneGrowing, __result);
        }
    }

    /// <summary>
    /// The postfix's grower-found branch: <paramref name="originalResult"/>, overridden by
    /// <paramref name="grower"/>'s allow-harvest flag whenever the active
    /// <see cref="IHysteresisController"/> controls its harvesting.
    /// </summary>
    internal static bool ComputeResultFor(IPlantToGrowSettable grower, bool originalResult) =>
        ComputeResult(
            originalResult,
            FarmingHysteresisMod.HysteresisController.ControlsHarvestingOf(grower),
            grower.GetAllowHarvest()
        );

    /// <summary>
    /// Pure decision logic behind <see cref="ComputeResultFor"/>. A grower whose harvesting isn't
    /// controlled (<paramref name="controlled"/> false) must leave
    /// <paramref name="originalResult"/> untouched rather than applying its persisted
    /// <paramref name="allowHarvest"/> flag, so harvesting doesn't stay stuck blocked after
    /// control ended while <paramref name="allowHarvest"/> happened to be false.
    /// </summary>
    internal static bool ComputeResult(bool originalResult, bool controlled, bool allowHarvest) =>
        controlled ? allowHarvest : originalResult;
}
