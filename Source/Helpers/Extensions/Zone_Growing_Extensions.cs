using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FarmingHysteresis.Helpers.Extensions
{
	internal static class Zone_GrowingExtensions
	{
		internal static (ThingDef, int) PlantHarvestInfo(this Zone_Growing zone)
		{
			var harvestedThingDef = zone.GetPlantDefToGrow().plant.harvestedThingDef;
			if (harvestedThingDef != null)
			{
				return (harvestedThingDef, zone.Map.resourceCounter.GetCount(harvestedThingDef));
			}
			else
			{
				return (null, 0);
			}
		}
	}
}