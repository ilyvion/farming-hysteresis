using FarmingHysteresis.Defs;
using FarmingHysteresis.Extensions;

namespace FarmingHysteresis;

internal class GlobalThingDefBoundValueAccessor(FarmingHysteresisMapComponent mapComponent, ThingDef thingDef) : IBoundedValueAccessor
{
    private readonly FarmingHysteresisMapComponent mapComponent = mapComponent;
    private readonly ThingDef thingDef = thingDef;

    public BoundValues BoundValueRaw
    {
        get
        {
            if (mapComponent.GlobalBoundValues.TryGetValue(thingDef, out var value))
            {
                return value;
            }
            else
            {
                var boundValues = new BoundValues
                {
                    Upper = FarmingHysteresisMod.Settings.DefaultHysteresisUpperBound,
                    Lower = FarmingHysteresisMod.Settings.DefaultHysteresisLowerBound
                };
                mapComponent.GlobalBoundValues.Add(thingDef, boundValues);
                return boundValues;
            }
        }
    }
}

public class FarmingHysteresisMapComponent : MapComponent, ILoadReferenceable
{
    private int id = -1;

    private Dictionary<ThingDef, BoundValues>? globalBoundValues;

    internal Dictionary<ThingDef, BoundValues> GlobalBoundValues
    {
        get
        {
            globalBoundValues ??= [];
            return globalBoundValues;
        }
    }

    public FarmingHysteresisMapComponent(Map map) : base(map)
    {
        if (map == null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        // if not created in SavingLoading, give yourself the ID of the map you were constructed on.
        if (Scribe.mode == LoadSaveMode.Inactive)
        {
            id = map.uniqueID;
        }
    }

    internal bool HasBoundsFor(ThingDef harvestedThingDef)
    {
        if (globalBoundValues == null)
        {
            return false;
        }
        return globalBoundValues.ContainsKey(harvestedThingDef);
    }

    public string GetUniqueLoadID()
    {
        return "FarmingHysteresisMapComponent_" + id;
    }

    public static FarmingHysteresisMapComponent For(Map map)
    {
        if (map == null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        var instance = map.GetComponent<FarmingHysteresisMapComponent>();
        if (instance != null)
            return instance;

        instance = new FarmingHysteresisMapComponent(map);
        map.components.Add(instance);
        return instance;
    }

    internal IBoundedValueAccessor GetGlobalBoundedValueAccessorFor(ThingDef thingDef)
    {
        return new GlobalThingDefBoundValueAccessor(this, thingDef);
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();

        // No need to make these checks every single tick; once every 6 in-game minutes (4.16 seconds real time) should be enough.
        if (Find.TickManager.TicksGame % 250 != 0) return;

        foreach (var plantGrower in DefDatabase<FarmingHysteresisControlDef>.AllDefs.SelectMany(d => d.Worker.GetControlledPlantGrowers(map)))
        {
            var data = plantGrower.GetFarmingHysteresisData();
            if (data.Enabled)
            {
                data.UpdateLatchModeAndHandling(plantGrower);
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref id, "id", -1, true);
        Scribe_Collections.Look(ref globalBoundValues, "globalBoundValues", LookMode.Def, LookMode.Deep);

#if v1_5
#else
        if (globalBoundValues == null || globalBoundValues.Count == 0)
        {
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                TransferOldBounds();
            }
        }

        void TransferOldBounds()
        {
            Dictionary<ThingDef, int> globalLowerBoundValues = [];
            Dictionary<ThingDef, int> globalUpperBoundValues = [];
            Scribe_Collections.Look(ref globalLowerBoundValues, "globalLowerBoundValues", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref globalUpperBoundValues, "globalUpperBoundValues", LookMode.Def, LookMode.Value);

            if (globalLowerBoundValues == null || globalUpperBoundValues == null)
            {
                FarmingHysteresisMod.Instance.LogWarning("globalLowerBoundValues or globalUpperBoundValues was null; expected a value. No bounds transferred from old game.");
                return;
            }

            foreach (var thingDef in globalLowerBoundValues.Keys.Union(globalUpperBoundValues.Keys))
            {
                var boundValues = new BoundValues
                {
                    Upper = FarmingHysteresisMod.Settings.DefaultHysteresisUpperBound,
                    Lower = FarmingHysteresisMod.Settings.DefaultHysteresisLowerBound
                };
                {
                    if (globalLowerBoundValues.TryGetValue(thingDef, out var value))
                    {
                        boundValues.Lower = value;
                    }
                }
                {
                    if (globalUpperBoundValues.TryGetValue(thingDef, out var value))
                    {
                        boundValues.Upper = value;
                    }
                }
                GlobalBoundValues.Add(thingDef, boundValues);
            }
        }
#endif
    }
}
