using FarmingHysteresis.Extensions;
using VanillaPlantsExpandedMorePlants;

namespace FarmingHysteresis.VanillaPlantsExpandedMorePlants.Patch;

[HarmonyPatch(typeof(Zone_GrowingAquatic), nameof(Zone_GrowingAquatic.ExposeData))]
internal static class Zone_GrowingAquatic_ExposeData
{
    private static void Postfix(ref Zone_GrowingAquatic __instance)
    {
        __instance.GetFarmingHysteresisData().ExposeData();
    }
}

[HarmonyPatch(typeof(Zone_GrowingSandy), nameof(Zone_GrowingSandy.ExposeData))]
internal static class Zone_GrowingSandy_ExposeData
{
    private static void Postfix(ref Zone_GrowingSandy __instance)
    {
        __instance.GetFarmingHysteresisData().ExposeData();
    }
}
