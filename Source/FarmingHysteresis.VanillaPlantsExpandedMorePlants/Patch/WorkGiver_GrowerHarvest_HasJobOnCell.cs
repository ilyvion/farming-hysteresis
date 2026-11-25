using FarmingHysteresis.Patch;
using VanillaPlantsExpandedMorePlants;

namespace FarmingHysteresis.VanillaPlantsExpandedMorePlants.Patch;

[HarmonyPatch(
    typeof(WorkGiver_GrowerHarvestAquatic),
    nameof(WorkGiver_GrowerHarvestAquatic.HasJobOnCell)
)]
internal static class WorkGiver_GrowerHarvestAquatic_HasJobOnCell
{
    private static void Postfix(ref Pawn pawn, ref IntVec3 c, ref bool __result)
    {
        // We check that __result was true first so we don't mess up the game's
        // other reasons for disallowing harvesting.
        if (__result && c.GetZone(pawn.Map) is Zone_GrowingAquatic zoneGrowingAquatic)
        {
            __result = WorkGiver_GrowerHarvest_HasJobOnCell.ComputeResultFor(
                zoneGrowingAquatic,
                __result
            );
        }
    }
}

[HarmonyPatch(
    typeof(WorkGiver_GrowerHarvestSandy),
    nameof(WorkGiver_GrowerHarvestSandy.HasJobOnCell)
)]
internal static class WorkGiver_GrowerHarvestSandy_HasJobOnCell
{
    private static void Postfix(ref Pawn pawn, ref IntVec3 c, ref bool __result)
    {
        // We check that __result was true first so we don't mess up the game's
        // other reasons for disallowing harvesting.
        if (__result && c.GetZone(pawn.Map) is Zone_GrowingSandy zoneGrowingSandy)
        {
            __result = WorkGiver_GrowerHarvest_HasJobOnCell.ComputeResultFor(
                zoneGrowingSandy,
                __result
            );
        }
    }
}
