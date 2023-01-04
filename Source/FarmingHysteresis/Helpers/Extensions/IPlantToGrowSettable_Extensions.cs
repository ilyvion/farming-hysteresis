using System;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace FarmingHysteresis.Helpers.Extensions
{
    internal static class IPlantToGrowSettableExtensions
    {
        private class Building_PlantGrowerCustomFields
        {
            public bool allowSow;
            public bool allowCut;
        }

        internal static (ThingDef?, int) PlantHarvestInfo(this IPlantToGrowSettable plantToGrowSettable)
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

        private static readonly ConditionalWeakTable<Building_PlantGrower, Building_PlantGrowerCustomFields> plantGrowerCustomFieldsTable = new();
        internal static void SetHysteresisControlState(this IPlantToGrowSettable plantToGrowSettable, bool state)
        {
            if (plantToGrowSettable is Zone_Growing zoneGrowing)
            {
                zoneGrowing.allowSow = Settings.ControlSowing ? state : true;
                zoneGrowing.allowCut = Settings.ControlHarvesting ? state : true;
            }
            else if (plantToGrowSettable is Building_PlantGrower buildingPlantGrower)
            {
                var buildingPlantGrowerCustomFields = plantGrowerCustomFieldsTable.GetValue(buildingPlantGrower, (b) => new());
                buildingPlantGrowerCustomFields.allowSow = Settings.ControlSowing ? state : true;
                buildingPlantGrowerCustomFields.allowCut = Settings.ControlHarvesting ? state : true;
            }
            else
            {
                Log.Error($"Called {nameof(SetHysteresisControlState)} on an unknown IPlantToGrowSettable: {plantToGrowSettable.GetType().FullName}.");
            }
        }

        internal static bool GetAllowSow(this IPlantToGrowSettable plantToGrowSettable)
        {
            if (plantToGrowSettable is Zone_Growing zoneGrowing)
            {
                return zoneGrowing.allowSow;
            }
            else if (plantToGrowSettable is Building_PlantGrower buildingPlantGrower)
            {
                if (plantGrowerCustomFieldsTable.TryGetValue(buildingPlantGrower, out var buildingPlantGrowerCustomFields))
                {
                    return buildingPlantGrowerCustomFields.allowSow;
                }
                return true;
            }
            else
            {
                throw new Exception($"Called {nameof(GetAllowSow)} on an unknown IPlantToGrowSettable: {plantToGrowSettable.GetType().FullName}.");
            }
        }

        internal static bool GetAllowHarvest(this IPlantToGrowSettable plantToGrowSettable)
        {
            if (plantToGrowSettable is Zone_Growing zoneGrowing)
            {
                return zoneGrowing.allowCut;
            }
            else if (plantToGrowSettable is Building_PlantGrower buildingPlantGrower)
            {
                if (plantGrowerCustomFieldsTable.TryGetValue(buildingPlantGrower, out var buildingPlantGrowerCustomFields))
                {
                    return buildingPlantGrowerCustomFields.allowCut;
                }
                return true;
            }
            else
            {
                throw new Exception($"Called {nameof(GetAllowHarvest)} on an unknown IPlantToGrowSettable: {plantToGrowSettable.GetType().FullName}.");
            }
        }
    }
}
