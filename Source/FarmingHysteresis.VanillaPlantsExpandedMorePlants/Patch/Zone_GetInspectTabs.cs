using System.Collections.Generic;
using System.Linq;
using FarmingHysteresis.ITabs;
using HarmonyLib;
using RimWorld;
using VanillaPlantsExpandedMorePlants;
using Verse;

namespace FarmingHysteresis.VanillaPlantsExpandedMorePlants.Patch;

/// <summary>
/// Add Hysteresis inspect tab to growing zones
/// </summary>
[HarmonyPatch(typeof(Zone), nameof(Zone.GetInspectTabs))]
internal static class Zone_GetInspectTabs
{
    private static readonly ITab[] ITabs =
    [
        new ITab_Hysteresis()
    ];

    private static IEnumerable<InspectTabBase> Postfix(IEnumerable<InspectTabBase> values, Zone __instance)
    {
        if (__instance is not Zone_GrowingAquatic && __instance is not Zone_GrowingSandy)
        {
            return values;
        }

        if (values == null)
        {
            values = ITabs;
        }
        else
        {
            values = values.Concat(ITabs);
        }

        return values;
    }
}
