using FarmingHysteresis.Defs;
using FarmingHysteresis.Extensions;

namespace FarmingHysteresis;

internal class MapThingDefBoundValueAccessor(
    FarmingHysteresisMapComponent mapComponent,
    ThingDef thingDef
) : IBoundedValueAccessor
{
    private readonly FarmingHysteresisMapComponent mapComponent = mapComponent;
    private readonly ThingDef thingDef = thingDef;

    public BoundValues BoundValueRaw
    {
        get
        {
            if (mapComponent.MapBoundValues.TryGetValue(thingDef, out var value))
            {
                return value;
            }
            else
            {
                var boundValues = new BoundValues
                {
                    Upper = FarmingHysteresisMod.Settings.DefaultHysteresisUpperBound,
                    Lower = FarmingHysteresisMod.Settings.DefaultHysteresisLowerBound,
                };
                mapComponent.MapBoundValues.Add(thingDef, boundValues);
                return boundValues;
            }
        }
    }
}

/// <summary>
/// Tracks per-map hysteresis bounds and periodically re-evaluates plant growers that use them.
/// </summary>
public class FarmingHysteresisMapComponent : MapComponent, ILoadReferenceable
{
    private int id = -1;

    private Dictionary<ThingDef, BoundValues>? globalBoundValues;

    internal Dictionary<ThingDef, BoundValues> MapBoundValues
    {
        get
        {
            globalBoundValues ??= [];
            return globalBoundValues;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FarmingHysteresisMapComponent"/> class.
    /// </summary>
    /// <param name="map">The map this component belongs to.</param>
    public FarmingHysteresisMapComponent(Map map)
        : base(map)
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

    internal bool HasBoundsFor(ThingDef harvestedThingDef) =>
        globalBoundValues != null && globalBoundValues.ContainsKey(harvestedThingDef);

    internal IBoundedValueAccessor GetMapBoundedValueAccessorFor(ThingDef thingDef) =>
        new MapThingDefBoundValueAccessor(this, thingDef);

    /// <inheritdoc/>
    public string GetUniqueLoadID() => "FarmingHysteresisMapComponent_" + id;

    /// <summary>
    /// Gets the <see cref="FarmingHysteresisMapComponent"/> for the given <paramref name="map"/>,
    /// creating and attaching one if it doesn't already exist.
    /// </summary>
    /// <param name="map">The map to get the component for.</param>
    public static FarmingHysteresisMapComponent For(Map map)
    {
        if (map == null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        var instance = map.GetComponent<FarmingHysteresisMapComponent>();
        if (instance != null)
        {
            return instance;
        }

        instance = new FarmingHysteresisMapComponent(map);
        map.components.Add(instance);
        return instance;
    }

    /// <inheritdoc/>
    public override void MapComponentTick()
    {
        base.MapComponentTick();

        // No need to make these checks every single tick; once every 6 in-game minutes (4.16 seconds real time) should be enough.
        if (Find.TickManager.TicksGame % 250 != 0)
        {
            return;
        }

        foreach (
            var plantGrower in DefDatabase<FarmingHysteresisControlDef>.AllDefs.SelectMany(d =>
                d.Worker.GetControlledPlantGrowers(map)
            )
        )
        {
            var data = plantGrower.GetFarmingHysteresisData();
            if (data.Enabled)
            {
                data.UpdateLatchModeAndHandling(plantGrower);
            }
        }
    }

    /// <inheritdoc/>
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref id, "id", -1, true);
        Scribe_Collections.Look(
            ref globalBoundValues,
            "globalBoundValues",
            LookMode.Def,
            LookMode.Deep
        );

#if v1_3 || v1_4
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
            Scribe_Collections.Look(
                ref globalLowerBoundValues,
                "globalLowerBoundValues",
                LookMode.Def,
                LookMode.Value
            );
            Scribe_Collections.Look(
                ref globalUpperBoundValues,
                "globalUpperBoundValues",
                LookMode.Def,
                LookMode.Value
            );

            if (globalLowerBoundValues == null || globalUpperBoundValues == null)
            {
                FarmingHysteresisMod.Instance.LogWarning(
                    "globalLowerBoundValues or globalUpperBoundValues was null; expected a value. No bounds transferred from old game."
                );
                return;
            }

            foreach (var thingDef in globalLowerBoundValues.Keys.Union(globalUpperBoundValues.Keys))
            {
                var boundValues = new BoundValues
                {
                    Upper = FarmingHysteresisMod.Settings.DefaultHysteresisUpperBound,
                    Lower = FarmingHysteresisMod.Settings.DefaultHysteresisLowerBound,
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
                MapBoundValues.Add(thingDef, boundValues);
            }
        }
#endif
    }
}
