using FarmingHysteresis.Extensions;

namespace FarmingHysteresis;

internal class BoundValues : IExposable
{
    public int Upper;
    public int Lower;

    public void ExposeData()
    {
        Scribe_Values.Look(ref Upper, "upper", FarmingHysteresisMod.Settings.DefaultHysteresisUpperBound);
        Scribe_Values.Look(ref Lower, "lower", FarmingHysteresisMod.Settings.DefaultHysteresisLowerBound);
    }
}

internal class FarmingHysteresisData : IBoundedValueAccessor
{
    private readonly System.WeakReference<IPlantToGrowSettable> _plantGrowerWeakReference;

    private bool _enabled;
    private BoundValues _bounds;

    public LatchMode latchMode;
    public bool useGlobalValues;

    public FarmingHysteresisData(IPlantToGrowSettable plantGrower)
    {
        _plantGrowerWeakReference = new(plantGrower);
        _enabled = FarmingHysteresisMod.Settings.EnabledByDefault;
        _bounds = new BoundValues
        {
            Upper = FarmingHysteresisMod.Settings.DefaultHysteresisUpperBound,
            Lower = FarmingHysteresisMod.Settings.DefaultHysteresisLowerBound
        };
        useGlobalValues = FarmingHysteresisMod.Settings.UseGlobalValuesByDefault;
        latchMode = LatchMode.Unknown;
    }

    internal void ExposeData()
    {
        Scribe_Values.Look(ref _enabled, "farmingHysteresisEnabled", FarmingHysteresisMod.Settings.EnabledByDefault, true);
        Scribe_Deep.Look(
            ref _bounds,
            "farmingHysteresisBounds"
        );
        _bounds ??= new BoundValues
        {
            Upper = FarmingHysteresisMod.Settings.DefaultHysteresisUpperBound,
            Lower = FarmingHysteresisMod.Settings.DefaultHysteresisLowerBound
        };

#if v1_5
#else
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            TransferOldBounds();
        }
#endif

        Scribe_Values.Look(ref latchMode, "farmingHysteresisLatchMode", LatchMode.Unknown, true);
#if v1_5
#else
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
#endif
        Scribe_Values.Look(ref useGlobalValues, "farmingHysteresisUseGlobalValues", FarmingHysteresisMod.Settings.UseGlobalValuesByDefault, true);

#if v1_5
#else
        void TransferOldBounds()
        {
            int lowerBound = 0;
            int upperBound = 0;
            Scribe_Values.Look(ref lowerBound, "farmingHysteresisLowerBound", 0);
            if (lowerBound != 0)
            {
                _bounds.Lower = lowerBound;
            }
            Scribe_Values.Look(ref upperBound, "farmingHysteresisUpperBound", 0);
            if (upperBound != 0)
            {
                _bounds.Upper = upperBound;
            }
        }
#endif
    }

    BoundValues IBoundedValueAccessor.BoundValueRaw => _bounds;

    private IBoundedValueAccessor GetBoundedValueAccessor()
    {
        IBoundedValueAccessor values;
        if (!useGlobalValues)
        {
            values = this;
        }
        else
        {
            if (_plantGrowerWeakReference.TryGetTarget(out var zone))
            {
                var (harvestedThingDef, _) = zone.PlantHarvestInfo();
                if (harvestedThingDef == null)
                {
                    throw new Exception("This should not happen. Code: FHD-GBVA-PI");
                }
                values = FarmingHysteresisMapComponent.For(Find.CurrentMap).GetGlobalBoundedValueAccessorFor(harvestedThingDef);
            }
            else
            {
                throw new Exception("This should not happen. Code: FHD-GBVA-ZWR");
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
        get { return _enabled; }
    }

    internal void Enable(IPlantToGrowSettable plantToGrowSettable)
    {
        _enabled = true;
        UpdateLatchModeAndHandling(plantToGrowSettable);
    }

    internal void Disable(IPlantToGrowSettable plantToGrowSettable)
    {
        _enabled = false;
    }

    internal void UpdateLatchModeAndHandling(IPlantToGrowSettable plantToGrowSettable)
    {
        var (harvestedThingDef, harvestedThingCount) = plantToGrowSettable.PlantHarvestInfo();
        if (harvestedThingDef == null)
        {
            DisableDueToMissingHarvestedThingDef(plantToGrowSettable, plantToGrowSettable.GetPlantDefToGrow());
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
                plantToGrowSettable.SetHysteresisControlState(false);
                break;

            case LatchMode.BelowLowerBound:
            case LatchMode.BetweenBoundsEnabled:
                plantToGrowSettable.SetHysteresisControlState(true);
                break;

            default:
                throw new Exception($"We should never be in this state. This is a bug! State was {latchMode}.");
        }
    }

    internal void DisableDueToMissingHarvestedThingDef(IPlantToGrowSettable plantToGrowSettable, ThingDef plantDef)
    {
        _enabled = false;

        string settableName;
        bool suppressWarning = false;

        if (plantToGrowSettable is Zone zone)
        {
            settableName = $"Zone '{zone.label}'";
        }
        else if (plantToGrowSettable is Building building)
        {
            // Let's not cause unnecessary spam from flower pots and similar
            string? sowTag = building.def.building?.sowTag;
            if (sowTag is "Decorative" or "DecorativeTree")
            {
                suppressWarning = true;
            }
            settableName = $"Building named '{building.Label}' @ {building.InteractionCell.ToIntVec2}";
        }
        else if (plantToGrowSettable is ILoadReferenceable loadReferenceable)
        {
            settableName = loadReferenceable.GetUniqueLoadID();
        }
        else
        {
            settableName = $"Unknown type {plantToGrowSettable.GetType().FullName}";
        }

        if (!suppressWarning)
        {
            if (plantDef == null)
            {
                // This should never happen, but some mods may make plantDef null.
                FarmingHysteresisMod.Instance.LogWarning($"{settableName} has no plant set. Disabling farming hysteresis.");
            }
            else
            {
                FarmingHysteresisMod.Instance.LogWarning($"{settableName} has a plant type without a harvestable product ({plantDef.label}). Disabling farming hysteresis.");
            }
        }
    }
}
