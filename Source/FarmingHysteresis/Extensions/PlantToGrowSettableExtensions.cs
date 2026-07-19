using FarmingHysteresis.Defs;

namespace FarmingHysteresis.Extensions;

internal static class PlantToGrowSettableExtensions
{
    private static readonly ConditionalWeakTable<
        IPlantToGrowSettable,
        FarmingHysteresisData
#pragma warning disable IDE0028 // Simplify collection initialization
    > dataTable = new();
#pragma warning restore IDE0028 // Simplify collection initialization

    private static FarmingHysteresisControlDef GetControlDefForPlantGrower(
        IPlantToGrowSettable plantToGrowSettable,
        string method
    )
    {
        var controlDef = DefDatabase<FarmingHysteresisControlDef>.AllDefs.SingleOrDefault(d =>
            d.controlledClass == plantToGrowSettable.GetType()
        );
        controlDef ??= DefDatabase<FarmingHysteresisControlDef>.AllDefs.SingleOrDefault(d =>
            d.controlledClass.IsAssignableFrom(plantToGrowSettable.GetType())
        );
        if (controlDef == null)
        {
            ThrowError(plantToGrowSettable, method);
        }

        return controlDef!;
    }

    internal static (ThingDef?, int) PlantHarvestInfo(this IPlantToGrowSettable plantToGrowSettable)
    {
        var harvestedThingDef = plantToGrowSettable.GetPlantDefToGrow()?.plant.harvestedThingDef;
        return harvestedThingDef != null
            ? (
                harvestedThingDef,
                plantToGrowSettable.Map.CountOfHarvestedThingDef(harvestedThingDef)
            )
            : (null, 0);
    }

    /// <summary>
    /// The current map-wide stock of <paramref name="harvestedThingDef"/> - shared by the
    /// per-grower <see cref="PlantHarvestInfo"/> lookup (default engine) and
    /// <c>Trigger_Hysteresis</c> (CMR integration), which tracks a job-chosen def directly rather
    /// than deriving it from any one grower's current selection.
    /// </summary>
    internal static int CountOfHarvestedThingDef(this Map map, ThingDef harvestedThingDef) =>
        FarmingHysteresisMod.Settings.CountAllOnMap
            ? map
                .listerThings.ThingsOfDef(harvestedThingDef)
                .Where(t => !t.IsForbidden(Faction.OfPlayer) && !t.Position.Fogged(map))
                .Sum(t => t.stackCount)
            : map.resourceCounter.GetCount(harvestedThingDef);

    /// <summary>
    /// Applies the hysteresis latch's enabled/disabled <paramref name="state"/> to
    /// <paramref name="plantGrower"/>'s sow/harvest gating. <paramref name="forceHarvestEnabled"/>
    /// (used by the CMR integration's crop rotation, see <c>Docs/CMRIntegrationRework.md</c>,
    /// Step 5 - resolves #6) overrides harvest to stay allowed regardless of
    /// <paramref name="state"/>/<see cref="Settings.ControlHarvesting"/> - needed so a crop this
    /// job has already rotated away from never gets stranded unharvested, permanently occupying
    /// its cell and stalling the rotation.
    /// </summary>
    internal static void SetHysteresisControlState(
        this IPlantToGrowSettable plantGrower,
        bool state,
        bool forceHarvestEnabled = false
    )
    {
        var def = GetControlDefForPlantGrower(plantGrower, nameof(SetHysteresisControlState));

        def.SetAllowSow(
            plantGrower,
            ComputeAllowSow(FarmingHysteresisMod.Settings.ControlSowing, state)
        );
        def.SetAllowHarvest(
            plantGrower,
            ComputeAllowHarvest(
                FarmingHysteresisMod.Settings.ControlHarvesting,
                state,
                forceHarvestEnabled
            )
        );
    }

    /// <summary>Pure decision logic behind <see cref="SetHysteresisControlState"/>'s sow gating.</summary>
    internal static bool ComputeAllowSow(bool controlSowing, bool state) => !controlSowing || state;

    /// <summary>Pure decision logic behind <see cref="SetHysteresisControlState"/>'s harvest gating.</summary>
    internal static bool ComputeAllowHarvest(
        bool controlHarvesting,
        bool state,
        bool forceHarvestEnabled
    ) => forceHarvestEnabled || !controlHarvesting || state;

    private static void ThrowError(IPlantToGrowSettable plantGrower, string method) =>
        throw new InvalidOperationException(
            $"Called {nameof(PlantToGrowSettableExtensions)}.{method} with an IPlantToGrowSettable without a FarmingHysteresisControlDef. Type was {plantGrower.GetType().FullName}"
        );

    internal static bool GetAllowSow(this IPlantToGrowSettable plantGrower)
    {
        var def = GetControlDefForPlantGrower(plantGrower, nameof(GetAllowSow));
        return def.GetAllowSow(plantGrower);
    }

    internal static bool GetAllowHarvest(this IPlantToGrowSettable plantGrower)
    {
        var def = GetControlDefForPlantGrower(plantGrower, nameof(GetAllowHarvest));
        return def.GetAllowHarvest(plantGrower);
    }

    internal static FarmingHysteresisData GetFarmingHysteresisData(
        this IPlantToGrowSettable plantGrower
    ) => dataTable.GetValue(plantGrower, (pg) => new FarmingHysteresisData(pg));
}
