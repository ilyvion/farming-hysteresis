using FarmingHysteresis.Helpers.Extensions;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;

namespace FarmingHysteresis.Patch
{
	[HarmonyPatch(typeof(Zone_Growing), nameof(Zone_Growing.GetGizmos))]
	internal static class RimWorld_Zone_Growing_GetGizmos
	{
		private static void Postfix(Zone_Growing __instance, ref IEnumerable<Gizmo> __result)
		{
			var data = __instance.GetFarmingHysteresisData();
			var harvestedThingDef = __instance.GetPlantDefToGrow().plant.harvestedThingDef;
			var harvestHysteresisCommand = new Command_Toggle
			{
				defaultLabel = "FarmingHysteresis.EnableFarmingHysteresis".Translate(),
				defaultDesc = "FarmingHysteresis.EnableFarmingHysteresisisDesc".Translate(),
				icon = TexCommand.ForbidOff,
				isActive = () => data.enabled,
				toggleAction = () => data.enabled = !data.enabled
			};

			var result = new List<Gizmo>(__result);
			if (harvestedThingDef != null)
			{
				result.Add(harvestHysteresisCommand);
			}

			if (data.enabled)
			{
				if (harvestedThingDef == null)
				{
					data.DisableDueToMissingHarvestedThingDef(__instance);
					return;
				}

				// If hysteresis is enabled, disable the manual sowing enabled button
				var sowingGizmo = result.Find(g => g is Command_Toggle t && t.defaultLabel == "CommandAllowSow".Translate());
				result.Remove(sowingGizmo);

				Texture2D uiIcon = harvestedThingDef.uiIcon;
				var decrementLowerHysteresisCommand = new Command_Decrement
				{
					defaultLabel = "FarmingHysteresis.DecrementLowerHysteresis".Translate(),
					defaultDesc = "FarmingHysteresis.DecrementLowerHysteresisDesc".Translate(),
					icon = uiIcon,
					action = () => data.LowerBound -= 100
				};
				result.Add(decrementLowerHysteresisCommand);

				var incrementLowerHysteresisCommand = new Command_Increment
				{
					defaultLabel = "FarmingHysteresis.IncrementLowerHysteresis".Translate(),
					defaultDesc = "FarmingHysteresis.IncrementLowerHysteresisDesc".Translate(),
					icon = uiIcon,
					action = () => data.LowerBound += 100
				};
				result.Add(incrementLowerHysteresisCommand);

				var decrementUpperHysteresisCommand = new Command_Decrement
				{
					defaultLabel = "FarmingHysteresis.DecrementUpperHysteresis".Translate(),
					defaultDesc = "FarmingHysteresis.DecrementUpperHysteresisDesc".Translate(),
					icon = uiIcon,
					action = () => data.UpperBound -= 100
				};
				result.Add(decrementUpperHysteresisCommand);

				var incrementUpperHysteresisCommand = new Command_Increment
				{
					defaultLabel = "FarmingHysteresis.IncrementUpperHysteresis".Translate(),
					defaultDesc = "FarmingHysteresis.IncrementUpperHysteresisDesc".Translate(),
					icon = uiIcon,
					action = () => data.UpperBound += 100
				};
				result.Add(incrementUpperHysteresisCommand);
			}
			__result = result;
		}
	}
}