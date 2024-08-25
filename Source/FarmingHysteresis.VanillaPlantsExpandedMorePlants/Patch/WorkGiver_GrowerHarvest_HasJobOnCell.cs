using FarmingHysteresis.Extensions;
using VanillaPlantsExpandedMorePlants;

namespace FarmingHysteresis.VanillaPlantsExpandedMorePlants.Patch;

[HarmonyPatch(typeof(WorkGiver_GrowerHarvestAquatic), nameof(WorkGiver_GrowerHarvestAquatic.HasJobOnCell))]
internal static class WorkGiver_GrowerHarvestAquatic_HasJobOnCell
{
    private static void Postfix(ref Pawn pawn, ref IntVec3 c, ref bool __result)
    {
        if (__result && Settings.ControlHarvesting)
        {
            // We check that __result was true first so we don't mess up the game's
            // other reasons for disallowing harvesting. But if harvesting normally is
            // allowed, override it with what the hysteresis value is at any given time.
            if (c.GetZone(pawn.Map) is Zone_GrowingAquatic zoneGrowingAquatic)
            {
                __result = zoneGrowingAquatic.GetAllowHarvest();
            }
        }
    }
}

[HarmonyPatch(typeof(WorkGiver_GrowerHarvestSandy), nameof(WorkGiver_GrowerHarvestSandy.HasJobOnCell))]
internal static class WorkGiver_GrowerHarvestSandy_HasJobOnCell
{
    private static void Postfix(ref Pawn pawn, ref IntVec3 c, ref bool __result)
    {
        if (__result && Settings.ControlHarvesting)
        {
            // We check that __result was true first so we don't mess up the game's
            // other reasons for disallowing harvesting. But if harvesting normally is
            // allowed, override it with what the hysteresis value is at any given time.
            if (c.GetZone(pawn.Map) is Zone_GrowingSandy zoneGrowingSandy)
            {
                __result = zoneGrowingSandy.GetAllowHarvest();
            }
        }
    }
}
