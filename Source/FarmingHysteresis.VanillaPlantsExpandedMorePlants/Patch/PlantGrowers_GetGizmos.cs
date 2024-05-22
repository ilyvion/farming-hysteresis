using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;
using FarmingHysteresis.Patch;
using FarmingHysteresis.Extensions;
using VanillaPlantsExpandedMorePlants;

namespace FarmingHysteresis.VanillaPlantsExpandedMorePlants.Patch;

/// <summary>
/// Add Farming Hysteresis gizmos to the growing zone
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
/// Add Farming Hysteresis gizmos to any Building_PlantGrower
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
