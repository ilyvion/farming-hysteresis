using System.Diagnostics.CodeAnalysis;

namespace FarmingHysteresis.Defs;

// Rimworld Defs have values set through reflection
#pragma warning disable CS8618

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

    public Type workerClass = typeof(FarmingHysteresisControlWorker);
    public Type controlledClass = typeof(IPlantToGrowSettable);

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

    public bool GetAllowSow(IPlantToGrowSettable plantGrower)
    {
        ValidatePlantGrowerType(plantGrower, nameof(GetAllowSow));

        return Worker.HandleAllowSow
            ? Worker.GetAllowSow(plantGrower)
            : plantGrowerControlFields.GetValue(plantGrower, (z) => new()).allowSow;
    }

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

    public bool GetAllowHarvest(IPlantToGrowSettable plantGrower)
    {
        ValidatePlantGrowerType(plantGrower, nameof(GetAllowHarvest));

        return Worker.HandleAllowHarvest
            ? Worker.GetAllowHarvest(plantGrower)
            : plantGrowerControlFields.GetValue(plantGrower, (z) => new()).allowHarvest;
    }

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

public abstract class FarmingHysteresisControlWorker
{
    public FarmingHysteresisControlDef def;

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

public class FarmingHysteresisControlWorker_Building_PlantGrower : FarmingHysteresisControlWorker
{
    public override IEnumerable<IPlantToGrowSettable> GetControlledPlantGrowers(Map map) =>
        (
            map ?? throw new ArgumentNullException(nameof(map))
        ).listerBuildings.AllBuildingsColonistOfClass<Building_PlantGrower>();
}

public class FarmingHysteresisControlWorker_Zone_Growing : FarmingHysteresisControlWorker
{
    public override IEnumerable<IPlantToGrowSettable> GetControlledPlantGrowers(Map map) =>
        (
            map ?? throw new ArgumentNullException(nameof(map))
        ).zoneManager.AllZones.OfType<Zone_Growing>();

    public override bool HandleAllowSow => true;

    public override bool GetAllowSow(IPlantToGrowSettable plantGrower) =>
        plantGrower == null
            ? throw new ArgumentNullException(nameof(plantGrower))
            : ((Zone_Growing)plantGrower).allowSow;

    public override void SetAllowSow(IPlantToGrowSettable plantGrower, bool value)
    {
        if (plantGrower == null)
        {
            throw new ArgumentNullException(nameof(plantGrower));
        }

        ((Zone_Growing)plantGrower).allowSow = value;
    }
}
