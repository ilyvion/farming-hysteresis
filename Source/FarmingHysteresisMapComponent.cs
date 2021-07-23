using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FarmingHysteresis
{
    internal class GlobalThingDefBoundValueAccessor : IBoundedValueAccessor
    {
        private FarmingHysteresisMapComponent mapComponent;
        private ThingDef thingDef;

        public GlobalThingDefBoundValueAccessor(FarmingHysteresisMapComponent mapComponent, ThingDef thingDef)
        {
            this.mapComponent = mapComponent;
            this.thingDef = thingDef;
        }

        public int LowerBoundValueRaw
        {
            get
            {
                if (mapComponent.GlobalLowerBoundValues.TryGetValue(thingDef, out var value))
                {
                    return value;
                }
                return Settings.DefaultHysteresisLowerBound;
            }
            set
            {
                mapComponent.GlobalLowerBoundValues[thingDef] = value;
            }
        }

        public int UpperBoundValueRaw
        {
            get
            {
                if (mapComponent.GlobalUpperBoundValues.TryGetValue(thingDef, out var value))
                {
                    return value;
                }
                return Settings.DefaultHysteresisUpperBound;
            }
            set
            {
                mapComponent.GlobalUpperBoundValues[thingDef] = value;
            }
        }
    }

    public class FarmingHysteresisMapComponent : MapComponent, ILoadReferenceable
    {
        private int id = -1;

        private Dictionary<ThingDef, int> globalLowerBoundValues;
        private Dictionary<ThingDef, int> globalUpperBoundValues;

        internal Dictionary<ThingDef, int> GlobalLowerBoundValues
        {
            get
            {
                if (globalLowerBoundValues == null)
                {
                    globalLowerBoundValues = new Dictionary<ThingDef, int>();
                }
                return globalLowerBoundValues;
            }
        }

        internal Dictionary<ThingDef, int> GlobalUpperBoundValues
        {
            get
            {
                if (globalUpperBoundValues == null)
                {
                    globalUpperBoundValues = new Dictionary<ThingDef, int>();
                }
                return globalUpperBoundValues;
            }
        }

        public FarmingHysteresisMapComponent(Map map) : base(map)
        {
            // if not created in SavingLoading, give yourself the ID of the map you were constructed on.
            if (Scribe.mode == Verse.LoadSaveMode.Inactive) id = map.uniqueID;
        }

        internal bool HasBoundsFor(ThingDef harvestedThingDef)
        {
            return GlobalLowerBoundValues.ContainsKey(harvestedThingDef) || GlobalUpperBoundValues.ContainsKey(harvestedThingDef);
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
            Scribe_Collections.Look(ref globalLowerBoundValues, "globalLowerBoundValues", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref globalUpperBoundValues, "globalUpperBoundValues", LookMode.Def, LookMode.Value);
        }
    }
}
