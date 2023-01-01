using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FarmingHysteresis
{
    internal class BoundValues
    {
        public int Upper;
        public int Lower;
    }

    internal class GlobalThingDefBoundValueAccessor : IBoundedValueAccessor
    {
        private FarmingHysteresisMapComponent mapComponent;
        private ThingDef thingDef;

        public GlobalThingDefBoundValueAccessor(FarmingHysteresisMapComponent mapComponent, ThingDef thingDef)
        {
            this.mapComponent = mapComponent;
            this.thingDef = thingDef;
        }

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
                        Upper = Settings.DefaultHysteresisUpperBound,
                        Lower = Settings.DefaultHysteresisLowerBound,
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

        private Dictionary<ThingDef, BoundValues> globalBoundValues;

        internal Dictionary<ThingDef, BoundValues> GlobalBoundValues
        {
            get
            {
                if (globalBoundValues == null)
                {
                    globalBoundValues = new Dictionary<ThingDef, BoundValues>();
                }
                return globalBoundValues;
            }
        }

        public FarmingHysteresisMapComponent(Map map) : base(map)
        {
            // if not created in SavingLoading, give yourself the ID of the map you were constructed on.
            if (Scribe.mode == Verse.LoadSaveMode.Inactive) id = map.uniqueID;
        }

        internal bool HasBoundsFor(ThingDef harvestedThingDef)
        {
            return globalBoundValues.ContainsKey(harvestedThingDef);
        }

        public string GetUniqueLoadID()
        {
            return "FarmingHysteresisMapComponent_" + id;
        }

        public static FarmingHysteresisMapComponent For(Map map)
        {
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

            foreach (var zone in map.zoneManager.AllZones.OfType<Zone_Growing>())
            {
                var data = zone.GetFarmingHysteresisData();
                if (data.Enabled)
                {
                    data.UpdateLatchModeAndSowing(zone);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref id, "id", -1, true);
            Scribe_Collections.Look(ref globalBoundValues, "globalBoundValues", LookMode.Def, LookMode.Value);
            if (globalBoundValues == null || globalBoundValues.Count == 0)
            {
                if (Scribe.mode == LoadSaveMode.LoadingVars)
                {
                    TransferOldBounds();
                }
            }

            void TransferOldBounds()
            {
                Dictionary<ThingDef, int> globalLowerBoundValues = new();
                Dictionary<ThingDef, int> globalUpperBoundValues = new();
                Scribe_Collections.Look(ref globalLowerBoundValues, "globalLowerBoundValues", LookMode.Def, LookMode.Value);
                Scribe_Collections.Look(ref globalUpperBoundValues, "globalUpperBoundValues", LookMode.Def, LookMode.Value);

                foreach (var thingDef in globalLowerBoundValues.Keys.Union(globalUpperBoundValues.Keys))
                {
                    var boundValues = new BoundValues
                    {
                        Upper = Settings.DefaultHysteresisUpperBound,
                        Lower = Settings.DefaultHysteresisLowerBound,
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
        }
    }
}
