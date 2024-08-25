using FarmingHysteresis.Extensions;

namespace FarmingHysteresis.Patch
{
    [HarmonyPatch(typeof(WorkGiver_GrowerHarvest), nameof(WorkGiver_GrowerHarvest.HasJobOnCell))]
    internal static class WorkGiver_GrowerHarvest_HasJobOnCell
    {
        private static void Postfix(ref Pawn pawn, ref IntVec3 c, ref bool __result)
        {
            if (__result && Settings.ControlHarvesting)
            {
                // We check that __result was true first so we don't mess up the game's
                // other reasons for disallowing harvesting. But if harvesting normally is
                // allowed, override it with what the hysteresis value is at any given time.
                if (c.GetFirstBuilding(pawn.Map) is Building_PlantGrower buildingPlantGrower)
                {
                    __result = buildingPlantGrower.GetAllowHarvest();
                }
                else if (c.GetZone(pawn.Map) is Zone_Growing zoneGrowing)
                {
                    __result = zoneGrowing.GetAllowHarvest();
                }
            }
        }
    }
}
