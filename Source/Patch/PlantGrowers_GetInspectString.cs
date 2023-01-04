using System;
using FarmingHysteresis.Helpers.Extensions;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FarmingHysteresis.Patch
{
    [HarmonyPatch(typeof(Zone_Growing), nameof(Zone_Growing.GetInspectString))]
    internal static class RimWorld_Zone_Growing_GetInspectString
    {
        private static void Postfix(Zone_Growing __instance, ref string __result)
        {
            GetInspectStringPatcher.Patch(
                __instance,
                ref __result,
                (i) => i.GetFarmingHysteresisData()
            );
        }
    }

    [HarmonyPatch(typeof(Building_PlantGrower), nameof(Building_PlantGrower.GetInspectString))]
    internal static class RimWorld_Building_PlantGrower_GetInspectString
    {
        private static void Postfix(Building_PlantGrower __instance, ref string __result)
        {
            GetInspectStringPatcher.Patch(
                __instance,
                ref __result,
                (i) => i.GetFarmingHysteresisData()
            );
        }
    }



    internal class GetInspectStringPatcher
    {
        internal static void Patch<T>(
            T plantToGrowSettable,
            ref string __result,
            Func<T, FarmingHysteresisData> getHysteresisData
        )
            where T : IPlantToGrowSettable
        {
            var data = getHysteresisData(plantToGrowSettable);
            var (harvestedThingDef, harvestedThingCount) = plantToGrowSettable.PlantHarvestInfo();
            if (data.Enabled)
            {
                if (harvestedThingDef == null)
                {
                    data.DisableDueToMissingHarvestedThingDef(plantToGrowSettable);
                    return;
                }

                var plant = plantToGrowSettable.GetPlantDefToGrow();
                __result += "\n" + "FarmingHysteresis.UseGlobalBounds".Translate(harvestedThingDef.label, data.useGlobalValues ? "Yes".Translate() : "No".Translate());
                __result += "\n" + "FarmingHysteresis.LowerBound".Translate(plant.label, data.LowerBound, harvestedThingDef.label);
                __result += "\n" + "FarmingHysteresis.UpperBound".Translate(plant.label, data.UpperBound, harvestedThingDef.label);
                __result += "\n" + "FarmingHysteresis.InStorage".Translate(harvestedThingDef.label, harvestedThingCount);
                __result += "\n" + "FarmingHysteresis.LatchModeDesc".Translate(("FarmingHysteresis.LatchModeDesc." + data.latchMode.ToString()).Translate(Settings.HysteresisMode.AsString()));
            }
            else if (harvestedThingDef == null)
            {
                __result += "\n" + "FarmingHysteresis.DisabledDueToMissingHarvestedThingDef".Translate();
            }
        }
    }
}
