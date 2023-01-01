using System;
using System.Runtime.CompilerServices;
using FarmingHysteresis.Helpers.Extensions;
using RimWorld;
using Verse;

namespace FarmingHysteresis
{
    internal class BoundValues : IExposable
    {
        public int Upper;
        public int Lower;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Upper, "upper", Settings.DefaultHysteresisUpperBound);
            Scribe_Values.Look(ref Lower, "lower", Settings.DefaultHysteresisLowerBound);
        }
    }

    internal class FarmingHysteresisData : IBoundedValueAccessor
    {
        private System.WeakReference<IPlantToGrowSettable> zoneWeakReference;

        private bool enabled;
        private BoundValues bounds;
        public LatchMode latchMode;

        public bool useGlobalValues;

        public FarmingHysteresisData(System.WeakReference<IPlantToGrowSettable> weakReference)
        {
            zoneWeakReference = weakReference;
            enabled = Settings.EnabledByDefault;
            bounds = new BoundValues
            {
                Upper = Settings.DefaultHysteresisUpperBound,
                Lower = Settings.DefaultHysteresisLowerBound
            };
            useGlobalValues = Settings.UseGlobalValuesByDefault;
            latchMode = LatchMode.Unknown;
        }

        internal void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "farmingHysteresisEnabled", Settings.EnabledByDefault, true);
            Scribe_Deep.Look(
                ref bounds,
                "farmingHysteresisBounds"
            );
            if (bounds == null)
            {
                bounds = new BoundValues
                {
                    Upper = Settings.DefaultHysteresisUpperBound,
                    Lower = Settings.DefaultHysteresisLowerBound
                };
            }
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                TransferOldBounds();
            }

            Scribe_Values.Look(ref latchMode, "farmingHysteresisLatchMode", LatchMode.Unknown, true);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                // Ignore obsolete warning (612) since we're explicitly
                // transferring from the obsolete states to the new
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
            Scribe_Values.Look(ref useGlobalValues, "farmingHysteresisUseGlobalValues", Settings.UseGlobalValuesByDefault, true);

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

        internal void Enable(IPlantToGrowSettable plantToGrowSettable)
        {
            enabled = true;
            UpdateLatchModeAndSowing(plantToGrowSettable);
        }

        internal void Disable(IPlantToGrowSettable plantToGrowSettable)
        {
            enabled = false;
        }

        internal void UpdateLatchModeAndSowing(IPlantToGrowSettable plantToGrowSettable)
        {
            Log.Warning("Floob {plantToGrowSettable}");

            var (harvestedThingDef, harvestedThingCount) = plantToGrowSettable.PlantHarvestInfo();
            if (harvestedThingDef == null)
            {
                DisableDueToMissingHarvestedThingDef(plantToGrowSettable);
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
                    plantToGrowSettable.SetAllow(false);
                    break;

                case LatchMode.BelowLowerBound:
                case LatchMode.BetweenBoundsEnabled:
                    plantToGrowSettable.SetAllow(true);
                    break;

                default:
                    throw new Exception($"We should never be in this state. This is a bug! State was {latchMode}.");
            }
        }

        internal void DisableDueToMissingHarvestedThingDef(IPlantToGrowSettable plantToGrowSettable)
        {
            enabled = false;
            if (plantToGrowSettable is ILoadReferenceable loadReferenceable)
            {
                Log.Warning($"{loadReferenceable.GetUniqueLoadID()} has a plant type without a harvestable product. Disabling farming hysteresis.");
            }
            else
            {
                Log.Warning($"Something has a plant type without a harvestable product. Disabling farming hysteresis.");
            }
        }
    }

    internal static class FarmingHysteresisDataExtensions
    {
        private static readonly ConditionalWeakTable<IPlantToGrowSettable, FarmingHysteresisData> dataTable = new();

        internal static FarmingHysteresisData GetFarmingHysteresisData(this Zone_Growing zone)
        {
            return dataTable.GetValue(zone, (z) => new FarmingHysteresisData(new System.WeakReference<IPlantToGrowSettable>(z)));
        }

        internal static FarmingHysteresisData GetFarmingHysteresisData(this Building_PlantGrower plantGrower)
        {
            return dataTable.GetValue(plantGrower, (p) => new FarmingHysteresisData(new System.WeakReference<IPlantToGrowSettable>(p)));
        }
    }
}
