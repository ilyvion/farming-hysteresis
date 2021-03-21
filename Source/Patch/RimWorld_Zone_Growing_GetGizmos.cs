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
			if (Find.Selector.NumSelected != 1)
			{
				return;
			}

			var data = __instance.GetFarmingHysteresisData();
			var harvestedThingDef = __instance.GetPlantDefToGrow().plant.harvestedThingDef;
			var harvestHysteresisCommand = new Command_Toggle
			{
				defaultLabel = "FarmingHysteresis.EnableFarmingHysteresis".Translate(),
				defaultDesc = "FarmingHysteresis.EnableFarmingHysteresisisDesc".Translate(),
				icon = TexCommand.ForbidOff,
				isActive = () => data.Enabled,
				toggleAction = () =>
				{
					if (data.Enabled)
					{
						data.Disable(__instance);
					}
					else
					{
						data.Enable(__instance);
					}
				}
			};

			var result = new List<Gizmo>(__result);
			if (harvestedThingDef != null)
			{
				result.Add(harvestHysteresisCommand);
			}

			if (data.Enabled)
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
					defaultLabel = "FarmingHysteresis.DecrementLowerHysteresis".Translate(GenUI.CurrentAdjustmentMultiplier()),
					defaultDesc = "FarmingHysteresis.DecrementLowerHysteresisDesc".Translate(GenUI.CurrentAdjustmentMultiplier()),
					icon = uiIcon,
					action = () => data.LowerBound -= GenUI.CurrentAdjustmentMultiplier()
				};
				result.Add(decrementLowerHysteresisCommand);

				var incrementLowerHysteresisCommand = new Command_Increment
				{
					defaultLabel = "FarmingHysteresis.IncrementLowerHysteresis".Translate(GenUI.CurrentAdjustmentMultiplier()),
					defaultDesc = "FarmingHysteresis.IncrementLowerHysteresisDesc".Translate(GenUI.CurrentAdjustmentMultiplier()),
					icon = uiIcon,
					action = () => data.LowerBound += GenUI.CurrentAdjustmentMultiplier()
				};
				result.Add(incrementLowerHysteresisCommand);

				var decrementUpperHysteresisCommand = new Command_Decrement
				{
					defaultLabel = "FarmingHysteresis.DecrementUpperHysteresis".Translate(GenUI.CurrentAdjustmentMultiplier()),
					defaultDesc = "FarmingHysteresis.DecrementUpperHysteresisDesc".Translate(GenUI.CurrentAdjustmentMultiplier()),
					icon = uiIcon,
					action = () => data.UpperBound -= GenUI.CurrentAdjustmentMultiplier()
				};
				result.Add(decrementUpperHysteresisCommand);

				var incrementUpperHysteresisCommand = new Command_Increment
				{
					defaultLabel = "FarmingHysteresis.IncrementUpperHysteresis".Translate(GenUI.CurrentAdjustmentMultiplier()),
					defaultDesc = "FarmingHysteresis.IncrementUpperHysteresisDesc".Translate(GenUI.CurrentAdjustmentMultiplier()),
					icon = uiIcon,
					action = () => data.UpperBound += GenUI.CurrentAdjustmentMultiplier()
				};
				result.Add(incrementUpperHysteresisCommand);
			}
			__result = result;
		}
	}
}