using FarmingHysteresis.Extensions;

namespace FarmingHysteresis.Patch;

[HarmonyPatch(typeof(WorkGiver_GrowerHarvest), nameof(WorkGiver_GrowerHarvest.HasJobOnCell))]
internal static class WorkGiver_GrowerHarvest_HasJobOnCell
{
    private static void Postfix(ref Pawn pawn, ref IntVec3 c, ref bool __result)
    {
        if (__result && FarmingHysteresisMod.Settings.ControlHarvesting)
        {
            // We check that __result was true first so we don't mess up the game's
            // other reasons for disallowing harvesting. But if harvesting normally is
            // allowed, override it with what the hysteresis value is at any given time.
            if (c.GetFirstBuilding(pawn.Map) is Building_PlantGrower buildingPlantGrower)
            {
                var data = buildingPlantGrower.GetFarmingHysteresisData();
                __result = ComputeResult(
                    __result,
                    data.Enabled,
                    buildingPlantGrower.GetAllowHarvest()
                );
            }
            else if (c.GetZone(pawn.Map) is Zone_Growing zoneGrowing)
            {
                var data = zoneGrowing.GetFarmingHysteresisData();
                __result = ComputeResult(__result, data.Enabled, zoneGrowing.GetAllowHarvest());
            }
        }
    }

    /// <summary>
    /// Pure decision logic behind the postfix's grower-found branch. A grower whose
    /// hysteresis is disabled (<paramref name="enabled"/> false) must leave
    /// <paramref name="originalResult"/> untouched rather than applying its persisted
    /// <paramref name="allowHarvest"/> flag, so harvesting doesn't stay stuck blocked after
    /// the latch was disabled while <paramref name="allowHarvest"/> happened to be false.
    /// </summary>
    internal static bool ComputeResult(bool originalResult, bool enabled, bool allowHarvest) =>
        enabled ? allowHarvest : originalResult;
}
