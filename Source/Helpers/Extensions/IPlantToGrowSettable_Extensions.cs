using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FarmingHysteresis.Helpers.Extensions
{
    internal static class IPlantToGrowSettableExtensions
    {
        internal static (ThingDef, int) PlantHarvestInfo(this IPlantToGrowSettable plantToGrowSettable)
        {
            var harvestedThingDef = plantToGrowSettable.GetPlantDefToGrow().plant.harvestedThingDef;
            if (harvestedThingDef != null)
            {
                return (harvestedThingDef, plantToGrowSettable.Map.resourceCounter.GetCount(harvestedThingDef));
            }
            else
            {
                return (null, 0);
            }
        }

        internal static void SetAllow(this IPlantToGrowSettable plantToGrowSettable, bool allow)
        {
            if (plantToGrowSettable is Zone_Growing zoneGrowing)
            {
                zoneGrowing.allowSow = allow;
            }
            else if (plantToGrowSettable is Building_PlantGrower buildingPlantGrower)
            {
                ForbidUtility.SetForbidden(buildingPlantGrower, !allow);
            }
            else
            {
                Log.Error("Called SetAllow on an unknown Thing.");
            }
        }
    }
}
