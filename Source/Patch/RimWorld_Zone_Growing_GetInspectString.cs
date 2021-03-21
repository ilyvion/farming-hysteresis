using System.Linq;
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
			var data = __instance.GetFarmingHysteresisData();
			var (harvestedThingDef, harvestedThingCount) = __instance.PlantHarvestInfo();
			if (data.Enabled)
			{
				if (harvestedThingDef == null)
				{
					data.DisableDueToMissingHarvestedThingDef(__instance);
					return;
				}

				var plant = __instance.GetPlantDefToGrow();
				__result += "\n" + "FarmingHysteresis.LowerBound".Translate(plant.label, data.LowerBound, harvestedThingDef.label);
				__result += "\n" + "FarmingHysteresis.UpperBound".Translate(plant.label, data.UpperBound, harvestedThingDef.label);
				__result += "\n" + "FarmingHysteresis.InStorage".Translate(harvestedThingDef.label, harvestedThingCount);
				__result += "\n" + "FarmingHysteresis.LatchModeDesc".Translate(("FarmingHysteresis.LatchModeDesc." + data.latchMode.ToString()).Translate());
			}
			else if (harvestedThingDef == null)
			{
				__result += "\n" + "FarmingHysteresis.DisabledDueToMissingHarvestedThingDef".Translate();
			}
		}
	}
}