using FarmingHysteresis.Patch;
using FarmingHysteresis.Extensions;
using VanillaPlantsExpandedMorePlants;

namespace FarmingHysteresis.VanillaPlantsExpandedMorePlants.Patch;

/// <summary>
/// Add Farming Hysteresis gizmos to the Zone_GrowingAquatic
/// </summary>
[HarmonyPatch(typeof(Zone_GrowingAquatic), nameof(Zone_GrowingAquatic.GetGizmos))]
internal static class Zone_GrowingAquatic_GetGizmos
{
    private static void Postfix(Zone_GrowingAquatic __instance, ref IEnumerable<Gizmo> __result)
    {
        GetGizmosPatcher.Patch(
            __instance,
            ref __result,
            (i) => i.GetFarmingHysteresisData(),
            (r) => r.Where(g =>
                g is Command_Toggle t &&
                (t.defaultLabel == "CommandAllowSow".Translate())).ToList()
        );
    }
}

/// <summary>
/// Add Farming Hysteresis gizmos to the Zone_GrowingSandy
/// </summary>
[HarmonyPatch(typeof(Zone_GrowingSandy), nameof(Zone_GrowingSandy.GetGizmos))]
internal static class Zone_GrowingSandy_GetGizmos
{
    private static void Postfix(Zone_GrowingSandy __instance, ref IEnumerable<Gizmo> __result)
    {
        GetGizmosPatcher.Patch(
            __instance,
            ref __result,
            (i) => i.GetFarmingHysteresisData(),
            (r) => r.Where(g =>
                g is Command_Toggle t &&
                (t.defaultLabel == "CommandAllowSow".Translate())).ToList()
        );
    }
}
