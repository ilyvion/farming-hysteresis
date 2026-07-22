using System.Diagnostics.CodeAnalysis;
using FarmingHysteresis.Extensions;

namespace FarmingHysteresis.Defs;

// Rimworld Defs have values set through reflection
#pragma warning disable CS8618

/// <summary>
/// A def describing how a class of plant growers (e.g. plant pots or growing zones) is
/// discovered and controlled by Farming Hysteresis.
/// </summary>
public class FarmingHysteresisControlDef : Def
{
    private class PlantToGrowSettableCustomFields
    {
        public bool allowSow = true;
        public bool allowHarvest = true;
    }

    private static readonly ConditionalWeakTable<
        IPlantToGrowSettable,
        PlantToGrowSettableCustomFields
#pragma warning disable IDE0028 // Simplify collection initialization
    > plantGrowerControlFields = new();
#pragma warning restore IDE0028 // Simplify collection initialization

    /// <summary>
    /// Gets every plant grower on <paramref name="map"/>, across all registered
    /// <see cref="FarmingHysteresisControlDef"/>s. Each grower is attributed to exactly one def -
    /// its most specific controller as resolved by <see cref="PlantToGrowSettableExtensions.ResolveControlDef"/>
    /// - so a grower whose concrete type is also matched (via inheritance) by a broader def's
    /// worker isn't double-enumerated.
    /// </summary>
    /// <param name="map">The map to search.</param>
    public static IEnumerable<IPlantToGrowSettable> AllControlledPlantGrowers(Map map)
    {
        var allDefs = DefDatabase<FarmingHysteresisControlDef>.AllDefsListForReading;
        return allDefs.SelectMany(d =>
            d.Worker.GetControlledPlantGrowers(map)
                .Where(g => IsMostSpecificControllerFor(allDefs, g.GetType(), d))
        );
    }

    /// <summary>Pure logic behind <see cref="AllControlledPlantGrowers"/>'s per-grower dedup filter: true when <paramref name="def"/> is the def <see cref="PlantToGrowSettableExtensions.ResolveControlDef"/> would pick for a grower of <paramref name="growerType"/> - i.e. <paramref name="def"/> is that grower's most specific controller, so its worker (rather than a broader def's worker whose enumeration also happens to match this type via inheritance) is the one that should yield it.</summary>
    internal static bool IsMostSpecificControllerFor(
        IEnumerable<FarmingHysteresisControlDef> allDefs,
        Type growerType,
        FarmingHysteresisControlDef def
    ) => PlantToGrowSettableExtensions.ResolveControlDef(allDefs, growerType) == def;

    /// <summary>The <see cref="FarmingHysteresisControlWorker"/> subclass to instantiate for this def.</summary>
    public Type workerClass = typeof(FarmingHysteresisControlWorker);

    /// <summary>The <see cref="IPlantToGrowSettable"/> implementation this def controls.</summary>
    public Type controlledClass = typeof(IPlantToGrowSettable);

    /// <inheritdoc/>
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
        {
            yield return error;
        }

        foreach (
            var error in FindControlledClassCollisions(
                DefDatabase<FarmingHysteresisControlDef>.AllDefsListForReading,
                this
            )
        )
        {
            yield return error;
        }
    }

    /// <summary>Pure logic behind <see cref="ConfigErrors"/>'s cross-def check: yields one error per other def in <paramref name="allDefs"/> that claims the same <see cref="controlledClass"/> as <paramref name="def"/>.</summary>
    internal static IEnumerable<string> FindControlledClassCollisions(
        IEnumerable<FarmingHysteresisControlDef> allDefs,
        FarmingHysteresisControlDef def
    ) =>
        allDefs
            .Where(d => d != def && d.controlledClass == def.controlledClass)
            .Select(other =>
                $"controlledClass {def.controlledClass.FullName} is already controlled by {other.defName}"
            );

    /// <summary>
    /// Gets the worker instance for this def, created from <see cref="workerClass"/> on first access.
    /// </summary>
    [field: Unsaved(false)]
    public FarmingHysteresisControlWorker Worker
    {
        get
        {
            if (field == null)
            {
                field = (FarmingHysteresisControlWorker)Activator.CreateInstance(workerClass);
                field.def = this;
            }
            return field;
        }
    }

    private void ValidatePlantGrowerType([NotNull] IPlantToGrowSettable? plantGrower, string method)
    {
        if (plantGrower == null)
        {
            throw new ArgumentNullException(nameof(plantGrower));
        }
        if (!controlledClass.IsAssignableFrom(plantGrower.GetType()))
        {
            throw new InvalidOperationException(
                $"Called {nameof(FarmingHysteresisControlDef)}.{method} with an IPlantToGrowSettable of the wrong type. Expected {controlledClass.FullName}, got {plantGrower.GetType().FullName}"
            );
        }
    }

    /// <summary>
    /// Gets whether sowing is currently allowed for the given <paramref name="plantGrower"/>.
    /// </summary>
    /// <param name="plantGrower">The plant grower to check.</param>
    public bool GetAllowSow(IPlantToGrowSettable plantGrower)
    {
        ValidatePlantGrowerType(plantGrower, nameof(GetAllowSow));

        return Worker.HandleAllowSow
            ? Worker.GetAllowSow(plantGrower)
            : plantGrowerControlFields.GetValue(plantGrower, (z) => new()).allowSow;
    }

    /// <summary>
    /// Sets whether sowing is allowed for the given <paramref name="plantGrower"/>.
    /// </summary>
    /// <param name="plantGrower">The plant grower to update.</param>
    /// <param name="value">Whether sowing should be allowed.</param>
    public void SetAllowSow(IPlantToGrowSettable plantGrower, bool value)
    {
        ValidatePlantGrowerType(plantGrower, nameof(SetAllowSow));

        if (Worker.HandleAllowSow)
        {
            Worker.SetAllowSow(plantGrower, value);
        }
        else
        {
            plantGrowerControlFields.GetValue(plantGrower, (z) => new()).allowSow = value;
        }
    }

    /// <summary>
    /// Gets whether harvesting is currently allowed for the given <paramref name="plantGrower"/>.
    /// </summary>
    /// <param name="plantGrower">The plant grower to check.</param>
    public bool GetAllowHarvest(IPlantToGrowSettable plantGrower)
    {
        ValidatePlantGrowerType(plantGrower, nameof(GetAllowHarvest));

        return Worker.HandleAllowHarvest
            ? Worker.GetAllowHarvest(plantGrower)
            : plantGrowerControlFields.GetValue(plantGrower, (z) => new()).allowHarvest;
    }

    /// <summary>
    /// Sets whether harvesting is allowed for the given <paramref name="plantGrower"/>.
    /// </summary>
    /// <param name="plantGrower">The plant grower to update.</param>
    /// <param name="value">Whether harvesting should be allowed.</param>
    public void SetAllowHarvest(IPlantToGrowSettable plantGrower, bool value)
    {
        ValidatePlantGrowerType(plantGrower, nameof(SetAllowHarvest));

        if (Worker.HandleAllowHarvest)
        {
            Worker.SetAllowHarvest(plantGrower, value);
        }
        else
        {
            plantGrowerControlFields.GetValue(plantGrower, (z) => new()).allowHarvest = value;
        }
    }
}

/// <summary>
/// Handles discovery of, and (optionally) sow/harvest control for, a class of plant growers
/// on behalf of a <see cref="FarmingHysteresisControlDef"/>.
/// </summary>
public abstract class FarmingHysteresisControlWorker
{
    /// <summary>The def this worker was created for.</summary>
    public FarmingHysteresisControlDef def;

    /// <summary>
    /// Gets all plant growers of the controlled class present on <paramref name="map"/>.
    /// </summary>
    /// <param name="map">The map to search.</param>
    public abstract IEnumerable<IPlantToGrowSettable> GetControlledPlantGrowers(Map map);

    /// <summary>
    /// If the plant grower doesn't have its own sowing control mechanism, return false
    /// in this property to have Farming Hysteresis track it. (This usually means the
    /// controlled type doesn't natively support this type of control natively and requires
    /// patching in order to function as expected.)
    /// </summary>
    public virtual bool HandleAllowSow => false;

    /// <summary>
    /// If the plant grower doesn't have its own harvest control mechanism, return false
    /// in this property to have Farming Hysteresis track it. (This usually means the
    /// controlled type doesn't natively support this type of control natively and requires
    /// patching in order to function as expected.)
    /// </summary>
    public virtual bool HandleAllowHarvest => false;

    /// <summary>
    /// Returns whether the plant grower allows or disallows sowing. If HandleAllowSow
    /// returns false, this method is unused.
    /// </summary>
    public virtual bool GetAllowSow(IPlantToGrowSettable plantGrower) =>
        throw new NotImplementedException();

    /// <summary>
    /// Tells the plant grower to allow or disallow sowing. If HandleAllowSow
    /// returns false, this method is unused.
    /// </summary>
    public virtual void SetAllowSow(IPlantToGrowSettable plantGrower, bool value) =>
        throw new NotImplementedException();

    /// <summary>
    /// Returns whether the plant grower allows or disallows harvesting. If HandleAllowSow
    /// returns false, this method is unused.
    /// </summary>
    public virtual bool GetAllowHarvest(IPlantToGrowSettable plantGrower) =>
        throw new NotImplementedException();

    /// <summary>
    /// Tells the plant grower to allow or disallow harvesting. If HandleAllowSow
    /// returns false, this method is unused.
    /// </summary>
    public virtual void SetAllowHarvest(IPlantToGrowSettable plantGrower, bool value) =>
        throw new NotImplementedException();
}

/// <summary>
/// A <see cref="FarmingHysteresisControlWorker"/> that controls <see cref="Building_PlantGrower"/> buildings.
/// </summary>
public class FarmingHysteresisControlWorker_Building_PlantGrower : FarmingHysteresisControlWorker
{
    /// <inheritdoc/>
    public override IEnumerable<IPlantToGrowSettable> GetControlledPlantGrowers(Map map) =>
        (
            map ?? throw new ArgumentNullException(nameof(map))
        ).listerBuildings.AllBuildingsColonistOfClass<Building_PlantGrower>();
}

/// <summary>
/// A <see cref="FarmingHysteresisControlWorker"/> that controls <see cref="Zone_Growing"/> zones.
/// </summary>
public class FarmingHysteresisControlWorker_Zone_Growing : FarmingHysteresisControlWorker
{
    /// <inheritdoc/>
    public override IEnumerable<IPlantToGrowSettable> GetControlledPlantGrowers(Map map) =>
        (
            map ?? throw new ArgumentNullException(nameof(map))
        ).zoneManager.AllZones.OfType<Zone_Growing>();

    /// <inheritdoc/>
    public override bool HandleAllowSow => true;

    /// <inheritdoc/>
    public override bool GetAllowSow(IPlantToGrowSettable plantGrower) =>
        plantGrower == null
            ? throw new ArgumentNullException(nameof(plantGrower))
            : ((Zone_Growing)plantGrower).allowSow;

    /// <inheritdoc/>
    public override void SetAllowSow(IPlantToGrowSettable plantGrower, bool value)
    {
        if (plantGrower == null)
        {
            throw new ArgumentNullException(nameof(plantGrower));
        }

        ((Zone_Growing)plantGrower).allowSow = value;
    }
}
