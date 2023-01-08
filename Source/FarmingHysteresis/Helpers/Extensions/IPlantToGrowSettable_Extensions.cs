using System;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace FarmingHysteresis.Helpers.Extensions
{
    internal static class IPlantToGrowSettableExtensions
    {
        private class IPlantToGrowSettableCustomFields
        {
            public bool allowSow;
            public bool allowHarvest;
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

        private static readonly ConditionalWeakTable<IPlantToGrowSettable, IPlantToGrowSettableCustomFields> plantGrowerCustomFieldsTable = new();
        internal static void SetHysteresisControlState(this IPlantToGrowSettable plantToGrowSettable, bool state)
        {
            if (plantToGrowSettable is Zone_Growing zoneGrowing)
            {
                var buildingPlantGrowerCustomFields = plantGrowerCustomFieldsTable.GetValue(zoneGrowing, (z) => new());
                zoneGrowing.allowSow = Settings.ControlSowing ? state : true;
                buildingPlantGrowerCustomFields.allowHarvest = Settings.ControlHarvesting ? state : true;
            }
            else if (plantToGrowSettable is Building_PlantGrower buildingPlantGrower)
            {
                var buildingPlantGrowerCustomFields = plantGrowerCustomFieldsTable.GetValue(buildingPlantGrower, (b) => new());
                buildingPlantGrowerCustomFields.allowSow = Settings.ControlSowing ? state : true;
                buildingPlantGrowerCustomFields.allowHarvest = Settings.ControlHarvesting ? state : true;
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
                if (plantGrowerCustomFieldsTable.TryGetValue(zoneGrowing, out var zoneGrowingCustomFields))
                {
                    return zoneGrowingCustomFields.allowHarvest;
                }
                return true;
            }
            else if (plantToGrowSettable is Building_PlantGrower buildingPlantGrower)
            {
                if (plantGrowerCustomFieldsTable.TryGetValue(buildingPlantGrower, out var buildingPlantGrowerCustomFields))
                {
                    return buildingPlantGrowerCustomFields.allowHarvest;
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
