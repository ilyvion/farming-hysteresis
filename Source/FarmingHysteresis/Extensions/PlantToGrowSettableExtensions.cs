using System.Diagnostics.CodeAnalysis;
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

    private static readonly Dictionary<Type, FarmingHysteresisControlDef> controlDefCache = [];

    private static readonly Dictionary<object, bool> canEverYieldHarvestCache = [];

    /// <summary>
    /// Cache key for <see cref="CanEverYieldHarvest"/>: <see cref="PlantUtility.CanSowOnGrower"/>
    /// decides sowability from a <see cref="Thing"/> grower's own <see cref="ThingDef"/> (e.g.
    /// plant pots vs. hydroponics basins have different <c>sowTag</c>s despite sharing a C#
    /// type), so those key by <see cref="ThingDef"/> rather than by <see cref="Type"/>. Non-<see
    /// cref="Thing"/> growers (zones and any other <see cref="IPlantToGrowSettable"/>
    /// implementation) key by their concrete <see cref="Type"/> instead.
    /// </summary>
    private static object CanEverYieldHarvestCacheKey(IPlantToGrowSettable grower) =>
        grower is Thing thing ? thing.def : grower.GetType();

    /// <summary>
    /// Whether <paramref name="grower"/> can ever grow a plant that produces a harvested item -
    /// false for growers restricted (by <c>sowTag</c>) to purely decorative plants, such as plant
    /// pots. Depends only on <paramref name="grower"/>'s def/type (via <see
    /// cref="PlantUtility.ValidPlantTypesForGrowers"/>), never on its current state, so the result
    /// is cached per <see cref="CanEverYieldHarvestCacheKey"/> rather than recomputed on every
    /// call.
    /// </summary>
    internal static bool CanEverYieldHarvest(this IPlantToGrowSettable grower)
    {
        var key = CanEverYieldHarvestCacheKey(grower);
        if (canEverYieldHarvestCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var result = ComputeCanEverYieldHarvest(PlantUtility.ValidPlantTypesForGrowers([grower]));
        canEverYieldHarvestCache[key] = result;
        return result;
    }

    /// <summary>Pure decision logic behind <see cref="CanEverYieldHarvest"/>: whether any of <paramref name="validPlantTypesForGrower"/> produces a harvested item.</summary>
    internal static bool ComputeCanEverYieldHarvest(
        IEnumerable<ThingDef> validPlantTypesForGrower
    ) => validPlantTypesForGrower.Any(plantDef => plantDef.plant?.harvestedThingDef != null);

    /// <summary>Pure lookup logic behind <see cref="GetControlDefForPlantGrower"/>'s def resolution: an exact <see cref="FarmingHysteresisControlDef.controlledClass"/> match wins, falling back to the first def whose <c>controlledClass</c> is assignable from <paramref name="type"/>. If more than one def claims the same <c>controlledClass</c>, this arbitrarily picks the first rather than throwing; <see cref="FarmingHysteresisControlDef.ConfigErrors"/> is what surfaces that collision to the modder.</summary>
    internal static FarmingHysteresisControlDef? ResolveControlDef(
        IEnumerable<FarmingHysteresisControlDef> defs,
        Type type
    )
    {
        var defList = defs as IReadOnlyCollection<FarmingHysteresisControlDef> ?? [.. defs];
        return defList.FirstOrDefault(d => d.controlledClass == type)
            ?? defList.FirstOrDefault(d => d.controlledClass.IsAssignableFrom(type));
    }

    private static FarmingHysteresisControlDef GetControlDefForPlantGrower(
        IPlantToGrowSettable plantToGrowSettable,
        string method
    )
    {
        var type = plantToGrowSettable.GetType();
        if (controlDefCache.TryGetValue(type, out var controlDef))
        {
            return controlDef;
        }

        controlDef = ResolveControlDef(DefDatabase<FarmingHysteresisControlDef>.AllDefs, type);
        if (controlDef == null)
        {
            ThrowError(plantToGrowSettable, method);
        }

        controlDefCache[type] = controlDef;
        return controlDef;
    }

    internal static (ThingDef?, int) PlantHarvestInfo(this IPlantToGrowSettable plantToGrowSettable)
    {
        var harvestedThingDef = plantToGrowSettable.PlantHarvestDef();
        return harvestedThingDef != null
            ? (
                harvestedThingDef,
                plantToGrowSettable.Map.CountOfHarvestedThingDef(harvestedThingDef)
            )
            : (null, 0);
    }

    /// <summary>
    /// The <see cref="ThingDef"/> half of <see cref="PlantHarvestInfo"/>, without computing the
    /// map-wide count - for call sites that only need to know whether a harvest def is chosen.
    /// </summary>
    internal static ThingDef? PlantHarvestDef(this IPlantToGrowSettable plantToGrowSettable) =>
        plantToGrowSettable.GetPlantDefToGrow()?.plant?.harvestedThingDef;

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
    /// <paramref name="plantGrower"/>'s sow/harvest gating, per <paramref name="mode"/> (the
    /// legacy per-grower engine passes <see cref="Settings.HysteresisMode"/>; the CMR integration
    /// passes its own job-level mode). <paramref name="forceHarvestEnabled"/> (used by the CMR
    /// integration's crop rotation) overrides harvest to stay allowed regardless of
    /// <paramref name="state"/>/<paramref name="mode"/> - needed so a crop this job has already
    /// rotated away from never gets stranded unharvested, permanently occupying its cell and
    /// stalling the rotation.
    /// </summary>
    internal static void SetHysteresisControlState(
        this IPlantToGrowSettable plantGrower,
        HysteresisMode mode,
        bool state,
        bool forceHarvestEnabled = false
    )
    {
        var def = GetControlDefForPlantGrower(plantGrower, nameof(SetHysteresisControlState));

        var vetoAllowsSow = FarmingHysteresisMod.AllowSowVeto?.Invoke(plantGrower) ?? true;
        def.SetAllowSow(plantGrower, ComputeAllowSow(mode.ControlsSowing(), state, vetoAllowsSow));
        def.SetAllowHarvest(
            plantGrower,
            ComputeAllowHarvest(mode.ControlsHarvesting(), state, forceHarvestEnabled)
        );
    }

    /// <summary>
    /// Hands <paramref name="plantGrower"/> back from hysteresis control: sowing and harvesting
    /// are both allowed again, except that a registered <see cref="FarmingHysteresisMod.AllowSowVeto"/>
    /// can still keep sowing off.
    /// </summary>
    internal static void ReleaseHysteresisControl(this IPlantToGrowSettable plantGrower) =>
        plantGrower.SetHysteresisControlState(HysteresisMode.SowingAndHarvesting, state: true);

    /// <summary>
    /// Pure decision logic behind <see cref="SetHysteresisControlState"/>'s sow gating.
    /// <paramref name="vetoAllowsSow"/> is <see cref="FarmingHysteresisMod.AllowSowVeto"/>'s
    /// result for this grower (true when no veto is registered or it doesn't apply) - ANDed in
    /// last so a registered veto can only ever turn sowing off, never force it on.
    /// </summary>
    internal static bool ComputeAllowSow(
        bool controlSowing,
        bool state,
        bool vetoAllowsSow = true
    ) => (!controlSowing || state) && vetoAllowsSow;

    /// <summary>Pure decision logic behind <see cref="SetHysteresisControlState"/>'s harvest gating.</summary>
    internal static bool ComputeAllowHarvest(
        bool controlHarvesting,
        bool state,
        bool forceHarvestEnabled
    ) => forceHarvestEnabled || !controlHarvesting || state;

    [DoesNotReturn]
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
