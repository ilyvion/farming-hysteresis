using System;
using System.Runtime.CompilerServices;
using FarmingHysteresis.Helpers.Extensions;
using RimWorld;
using Verse;

namespace FarmingHysteresis
{
    internal class FarmingHysteresisData : IBoundedValueAccessor
    {
        private System.WeakReference<Zone_Growing> zoneWeakReference;

        private bool enabled;
        private BoundValues bounds;
        public LatchMode latchMode;

        public bool useGlobalValues;

        public FarmingHysteresisData(System.WeakReference<Zone_Growing> weakReference)
        {
            zoneWeakReference = weakReference;
            enabled = Settings.EnabledByDefault;
            bounds = new()
            {
                Upper = Settings.DefaultHysteresisUpperBound,
                Lower = Settings.DefaultHysteresisLowerBound
            };
            useGlobalValues = Settings.UseGlobalValuesByDefault;
            latchMode = LatchMode.Unknown;
        }

        internal void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "farmingHysteresisEnabled", Settings.EnabledByDefault);
            Scribe_Values.Look(ref bounds, "farmingHysteresisBounds", new()
            {
                Upper = Settings.DefaultHysteresisUpperBound,
                Lower = Settings.DefaultHysteresisLowerBound
            });
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                TransferOldBounds();
            }

            Scribe_Values.Look(ref latchMode, "farmingHysteresisLatchMode", LatchMode.Unknown);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                // Ignore obsolete warning (612) since we're explicitly
                // transferring from the obsolete state to the new
                // state here.
#pragma warning disable 612
                switch (latchMode)
                {
                    case LatchMode.AboveLowerBoundDisabled:
                        latchMode = LatchMode.BetweenBoundsDisabled;
                        break;
                    case LatchMode.AboveLowerBoundEnabled:
                        latchMode = LatchMode.BetweenBoundsEnabled;
                        break;
                }
#pragma warning restore 612
            }
            Scribe_Values.Look(ref useGlobalValues, "farmingHysteresisUseGlobalValues", Settings.UseGlobalValuesByDefault);

            void TransferOldBounds()
            {
                int lowerBound = 0;
                int upperBound = 0;
                Scribe_Values.Look(ref lowerBound, "farmingHysteresisLowerBound", 0);
                if (lowerBound != 0)
                {
                    bounds.Lower = lowerBound;
                }
                Scribe_Values.Look(ref upperBound, "farmingHysteresisUpperBound", 0);
                if (upperBound != 0)
                {
                    bounds.Upper = upperBound;
                }
            }
        }

        BoundValues IBoundedValueAccessor.BoundValueRaw => bounds;

        private IBoundedValueAccessor GetBoundedValueAccessor()
        {
            IBoundedValueAccessor values;
            if (!useGlobalValues)
            {
                values = this;
            }
            else
            {
                if (zoneWeakReference.TryGetTarget(out var zone))
                {
                    var (harvestedThingDef, _) = zone.PlantHarvestInfo();
                    values = FarmingHysteresisMapComponent.For(Find.CurrentMap).GetGlobalBoundedValueAccessorFor(harvestedThingDef);
                }
                else
                {
                    throw new Exception("This should not happen. Code: FHD-GBVA");
                }
            }
            return values;
        }

        public int LowerBound
        {
            get => GetBoundedValueAccessor().BoundValueRaw.Lower;
            set
            {
                IBoundedValueAccessor values = GetBoundedValueAccessor();
                ref var lower = ref values.BoundValueRaw.Lower;
                var upper = values.BoundValueRaw.Upper;
                if (value < 0)
                {
                    // Don't allow value to be lower than 0
                    value = 0;
                }
                else if (value > upper)
                {
                    // Don't allow lower to be more than upper
                    value = upper;
                }
                lower = value;
            }
        }

        public int UpperBound
        {
            get => GetBoundedValueAccessor().BoundValueRaw.Upper;
            set
            {
                IBoundedValueAccessor values = GetBoundedValueAccessor();
                ref var upper = ref values.BoundValueRaw.Upper;
                var lower = values.BoundValueRaw.Lower;
                if (value < lower)
                {
                    // Don't allow upper to be less than lower
                    value = lower;
                }
                upper = value;
            }
        }

        internal bool Enabled
        {
            get { return enabled; }
        }

        internal void Enable(Zone_Growing zone)
        {
            enabled = true;
            UpdateLatchModeAndSowing(zone);
        }

        internal void Disable(Zone_Growing zone)
        {
            enabled = false;
        }

        internal void UpdateLatchModeAndSowing(Zone_Growing zone)
        {
            var (harvestedThingDef, harvestedThingCount) = zone.PlantHarvestInfo();
            if (harvestedThingDef == null)
            {
                DisableDueToMissingHarvestedThingDef(zone);
                return;
            }

            IBoundedValueAccessor values = GetBoundedValueAccessor();

            // First, check the simple cases
            if (harvestedThingCount < values.BoundValueRaw.Lower)
            {
                // Below lower bound: Enabled
                latchMode = LatchMode.BelowLowerBound;
            }
            else if (harvestedThingCount > values.BoundValueRaw.Upper)
            {
                // Above upper bound: Disabled
                latchMode = LatchMode.AboveUpperBound;
            }
            else
            {
                // We know harvestedThingCount is between lower and upper bound
                // at this point thanks to the above checks.

                switch (latchMode)
                {
                    case LatchMode.BelowLowerBound:
                    case LatchMode.Unknown:
                        // If we were previously below the lower bound, it's time to enter
                        // the "above lower bound enabled" state.
                        if (harvestedThingCount > values.BoundValueRaw.Lower)
                        {
                            latchMode = LatchMode.BetweenBoundsEnabled;
                        }
                        break;

                    case LatchMode.AboveUpperBound:
                        // If we were previously above the upper bound, it's time to enter
                        // the "above lower bound disabled" state.
                        if (harvestedThingCount < values.BoundValueRaw.Upper)
                        {
                            latchMode = LatchMode.BetweenBoundsDisabled;
                        }
                        break;
                }
            }

            switch (latchMode)
            {
                case LatchMode.AboveUpperBound:
                case LatchMode.BetweenBoundsDisabled:
                    zone.allowSow = false;
                    break;

                case LatchMode.BelowLowerBound:
                case LatchMode.BetweenBoundsEnabled:
                    zone.allowSow = true;
                    break;

                default:
                    throw new Exception($"We should never be in this state. This is a bug! State was {latchMode}.");
            }
        }

        internal void DisableDueToMissingHarvestedThingDef(Zone_Growing zone)
        {
            enabled = false;
            Log.Warning($"Zone {zone.ID} has a plant type without a harvestable product. Disabling farming hysteresis.");
        }
    }

    internal static class FarmingHysteresisDataExtensions
    {
        private static readonly ConditionalWeakTable<Zone_Growing, FarmingHysteresisData> dataTable = new ConditionalWeakTable<Zone_Growing, FarmingHysteresisData>();

        internal static FarmingHysteresisData GetFarmingHysteresisData(this Zone_Growing zone)
        {
            return dataTable.GetValue(zone, (z) => new FarmingHysteresisData(new System.WeakReference<Zone_Growing>(z)));
        }
    }
}
