using FarmingHysteresis.Extensions;
using HarmonyLib;
using RimWorld;

namespace FarmingHysteresis.Patch
{
    [HarmonyPatch(typeof(Building_PlantGrower), nameof(Building_PlantGrower.CanAcceptSowNow))]
    internal static class Building_PlantGrower_CanAcceptSowNow
    {
        private static void Postfix(ref Building_PlantGrower __instance, ref bool __result)
        {
            if (!__instance.GetFarmingHysteresisData().Enabled)
            {
                return;
            }

            if (__result && Settings.ControlSowing)
            {
                // We check that __result was true first so we don't mess up the game's
                // other reasons for disallowing sowing. But if sowing normally is
                // allowed, override it with what the hysteresis value is at any given time.
                __result = __instance.GetAllowSow();
            }
        }
    }
}
