using System.Diagnostics.CodeAnalysis;

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

    /// <summary>The <see cref="FarmingHysteresisControlWorker"/> subclass to instantiate for this def.</summary>
    public Type workerClass = typeof(FarmingHysteresisControlWorker);

    /// <summary>The <see cref="IPlantToGrowSettable"/> implementation this def controls.</summary>
    public Type controlledClass = typeof(IPlantToGrowSettable);

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
