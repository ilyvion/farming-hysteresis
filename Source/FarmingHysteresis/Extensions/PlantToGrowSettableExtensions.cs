using System;
using System.Linq;
using System.Runtime.CompilerServices;
using FarmingHysteresis.Defs;
using RimWorld;
using Verse;

namespace FarmingHysteresis.Extensions;

internal static class PlantToGrowSettableExtensions
{
    private static readonly ConditionalWeakTable<IPlantToGrowSettable, FarmingHysteresisData> dataTable = new();

    private static FarmingHysteresisControlDef GetControlDefForPlantGrower(IPlantToGrowSettable plantToGrowSettable, string method)
    {
        var controlDef = DefDatabase<FarmingHysteresisControlDef>.AllDefs.SingleOrDefault(d => d.controlledClass == plantToGrowSettable.GetType());
        controlDef ??= DefDatabase<FarmingHysteresisControlDef>.AllDefs.SingleOrDefault(d => d.controlledClass.IsAssignableFrom(plantToGrowSettable.GetType()));
        if (controlDef == null)
        {
            ThrowError(plantToGrowSettable, method);
        }

        return controlDef!;
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

    internal static void SetHysteresisControlState(this IPlantToGrowSettable plantGrower, bool state)
    {
        var def = GetControlDefForPlantGrower(plantGrower, nameof(SetHysteresisControlState));

        def.SetAllowSow(plantGrower, !Settings.ControlSowing || state);
        def.SetAllowHarvest(plantGrower, !Settings.ControlHarvesting || state);
    }

    private static void ThrowError(IPlantToGrowSettable plantGrower, string method)
    {
        throw new InvalidOperationException($"Called {nameof(PlantToGrowSettableExtensions)}.{method} with an IPlantToGrowSettable without a FarmingHysteresisControlDef. Type was {plantGrower.GetType().FullName}");
    }

    internal static bool GetAllowSow(this IPlantToGrowSettable plantGrower)
    {
        var def = GetControlDefForPlantGrower(plantGrower, nameof(SetHysteresisControlState));
        return def.GetAllowSow(plantGrower);
    }

    internal static bool GetAllowHarvest(this IPlantToGrowSettable plantGrower)
    {
        var def = GetControlDefForPlantGrower(plantGrower, nameof(SetHysteresisControlState));
        return def.GetAllowHarvest(plantGrower);
    }

    internal static FarmingHysteresisData GetFarmingHysteresisData(this IPlantToGrowSettable plantGrower)
    {
        return dataTable.GetValue(plantGrower, (pg) => new FarmingHysteresisData(new System.WeakReference<IPlantToGrowSettable>(pg)));
    }
}
