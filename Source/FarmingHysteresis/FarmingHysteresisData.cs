using FarmingHysteresis.Extensions;

namespace FarmingHysteresis;

internal class BoundValues : IExposable
{
    public int Upper;
    public int Lower;

    public void ExposeData()
    {
        Scribe_Values.Look(
            ref Upper,
            "upper",
            FarmingHysteresisMod.Settings.DefaultHysteresisUpperBound
        );
        Scribe_Values.Look(
            ref Lower,
            "lower",
            FarmingHysteresisMod.Settings.DefaultHysteresisLowerBound
        );
    }
}

/// <summary>
/// Shared "does this dictionary of bound values have an entry for this key" check behind
/// <see cref="FarmingHysteresisGameComponent.HasBoundsFor"/> and
/// <see cref="FarmingHysteresisMapComponent.HasBoundsFor"/>, split out so it's unit-testable
/// against a bare <see cref="Dictionary{TKey, TValue}"/> without either component's own
/// constructor-guarded <see cref="Map"/>/<see cref="Game"/> dependency.
/// </summary>
internal static class BoundValuesLookup
{
    internal static bool HasBounds<TKey>(Dictionary<TKey, BoundValues>? values, TKey key)
        where TKey : notnull => values != null && values.ContainsKey(key);

    /// <summary>
    /// Returns the existing entry for <paramref name="key"/> if present, otherwise a detached
    /// default that is <em>not</em> added to <paramref name="values"/>. Backs the read-only
    /// display path so merely listing/viewing values never materializes a dictionary entry.
    /// </summary>
    internal static BoundValues Peek<TKey>(
        Dictionary<TKey, BoundValues>? values,
        TKey key,
        int defaultLower,
        int defaultUpper
    )
        where TKey : notnull =>
        values != null && values.TryGetValue(key, out var value)
            ? value
            : new BoundValues { Lower = defaultLower, Upper = defaultUpper };

    /// <summary>
    /// Ensures <paramref name="value"/> (as previously returned by <see cref="Peek"/>) is present
    /// in <paramref name="values"/>. Called only once the player actually edits a row.
    /// </summary>
    internal static void Commit<TKey>(
        Dictionary<TKey, BoundValues> values,
        TKey key,
        BoundValues value
    )
        where TKey : notnull => values.TryAdd(key, value);
}

internal class FarmingHysteresisData : IBoundedValueAccessor
{
    private readonly System.WeakReference<IPlantToGrowSettable> _plantGrowerWeakReference;

    private bool _enabled;
    private BoundValues _bounds;

    public LatchMode latchMode;
    public BoundsSource boundsSource;

    public FarmingHysteresisData(IPlantToGrowSettable plantGrower)
    {
        _plantGrowerWeakReference = new(plantGrower);
        _enabled = FarmingHysteresisMod.Settings.EnabledByDefault;
        _bounds = new BoundValues
        {
            Upper = FarmingHysteresisMod.Settings.DefaultHysteresisUpperBound,
            Lower = FarmingHysteresisMod.Settings.DefaultHysteresisLowerBound,
        };
        boundsSource = FarmingHysteresisMod.Settings.DefaultBoundsSource;
        latchMode = LatchMode.Unknown;
    }

    internal void ExposeData()
    {
        Scribe_Values.Look(
            ref _enabled,
            "farmingHysteresisEnabled",
            FarmingHysteresisMod.Settings.EnabledByDefault,
            true
        );
        Scribe_Deep.Look(ref _bounds, "farmingHysteresisBounds");
        _bounds ??= new BoundValues
        {
            Upper = FarmingHysteresisMod.Settings.DefaultHysteresisUpperBound,
            Lower = FarmingHysteresisMod.Settings.DefaultHysteresisLowerBound,
        };

#if !v1_5_OR_GREATER
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            TransferOldBounds();
        }
#endif

        Scribe_Values.Look(ref latchMode, "farmingHysteresisLatchMode", LatchMode.Unknown, true);
#if !v1_5_OR_GREATER
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

                case LatchMode.Unknown:
                case LatchMode.BelowLowerBound:
                case LatchMode.BetweenBoundsEnabled:
                case LatchMode.BetweenBoundsDisabled:
                case LatchMode.AboveUpperBound:
                default:
                    break;
            }
#pragma warning restore 612
        }
#endif
        string? oldUseGlobalValues = null;
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            Scribe_Values.Look(ref oldUseGlobalValues, "farmingHysteresisUseGlobalValues");
        }
        if (oldUseGlobalValues != null)
        {
            boundsSource = BoundsSourceMigration.FromOldUseGlobalValues(
                oldUseGlobalValues == "True"
            );
        }
        else
        {
            Scribe_Values.Look(
                ref boundsSource,
                "farmingHysteresisBoundsSource",
                FarmingHysteresisMod.Settings.DefaultBoundsSource,
                true
            );
        }

#if !v1_5_OR_GREATER
        void TransferOldBounds()
        {
            var lowerBound = 0;
            var upperBound = 0;
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

    // _bounds is always already materialized (constructor/ExposeData), so peeking and
    // committing it is a no-op beyond returning it.
    BoundValues IBoundedValueAccessor.PeekBoundValue() => _bounds;

    void IBoundedValueAccessor.CommitBoundValue(BoundValues value) { }

    private IBoundedValueAccessor GetBoundedValueAccessor()
    {
        if (boundsSource == BoundsSource.Self)
        {
            return SelectAccessor(BoundsSource.Self, this, null, null);
        }

        if (!_plantGrowerWeakReference.TryGetTarget(out var plantGrower))
        {
            throw new InvalidOperationException("This should not happen. Code: FHD-GBVA-ZWR");
        }

        var harvestedThingDefOrNull = plantGrower.PlantHarvestDef();
        var harvestedThingDef =
            harvestedThingDefOrNull
            ?? throw new InvalidOperationException("This should not happen. Code: FHD-GBVA-PI");

        return SelectAccessor(
            boundsSource,
            this,
            () =>
                FarmingHysteresisMapComponent
                    .For(plantGrower.Map)
                    .GetMapBoundedValueAccessorFor(harvestedThingDef),
            () =>
                FarmingHysteresisGameComponent
                    .For(Current.Game)
                    .GetGameBoundedValueAccessorFor(harvestedThingDef)
        );
    }

    /// <summary>
    /// Picks the accessor for <paramref name="boundsSource"/> out of the already-resolved
    /// <paramref name="self"/> accessor and the lazily-resolved <paramref name="map"/>/
    /// <paramref name="game"/> accessor factories. Extracted from
    /// <see cref="GetBoundedValueAccessor"/> so the tier-selection logic can be unit tested
    /// without a live <see cref="Map"/>/<see cref="Game"/>.
    /// </summary>
    internal static IBoundedValueAccessor SelectAccessor(
        BoundsSource boundsSource,
        IBoundedValueAccessor self,
        Func<IBoundedValueAccessor>? map,
        Func<IBoundedValueAccessor>? game
    ) =>
        boundsSource switch
        {
            BoundsSource.Self => self,
            BoundsSource.Map => map!(),
            BoundsSource.Game => game!(),
            _ => throw new InvalidOperationException($"Uncovered BoundsSource: {boundsSource}."),
        };

    public int LowerBound
    {
        get => GetBoundedValueAccessor().BoundValueRaw.Lower;
        set
        {
            var values = GetBoundedValueAccessor();
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
            var values = GetBoundedValueAccessor();
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

    /// <summary>
    /// Seeds the currently-selected bounds tier's raw <see cref="BoundValues"/> with
    /// <paramref name="lower"/>/<paramref name="upper"/> directly, without going through the
    /// <see cref="LowerBound"/>/<see cref="UpperBound"/> setters. Those setters each clamp
    /// against the *other* bound's current value, so assigning them one at a time when seeding
    /// a fresh tier (still holding its own default bounds) can silently corrupt the seeded
    /// value regardless of assignment order.
    /// </summary>
    internal void SeedBounds(int lower, int upper)
    {
        var values = GetBoundedValueAccessor().BoundValueRaw;
        values.Lower = lower;
        values.Upper = upper;
    }

    internal bool Enabled => _enabled;

    internal void Enable(IPlantToGrowSettable plantToGrowSettable)
    {
        _enabled = true;
        UpdateLatchModeAndHandling(plantToGrowSettable);
    }

    internal void Disable() => _enabled = false;

    internal void UpdateLatchModeAndHandling(IPlantToGrowSettable plantToGrowSettable)
    {
        var (harvestedThingDef, harvestedThingCount) = plantToGrowSettable.PlantHarvestInfo();
        if (harvestedThingDef == null)
        {
            DisableDueToMissingHarvestedThingDef(
                plantToGrowSettable,
                plantToGrowSettable.GetPlantDefToGrow()
            );
            return;
        }

        var values = GetBoundedValueAccessor();

#if !v1_5_OR_GREATER
#pragma warning disable CS0612
        // Deprecated pre-1.5 latch values are converted to their modern equivalent on load
        // (see LatchMode.cs); if one somehow survives to here, leave it alone rather than
        // running it through the modern transition/resolution logic below.
        if (latchMode is LatchMode.AboveLowerBoundDisabled or LatchMode.AboveLowerBoundEnabled)
        {
            return;
        }
#pragma warning restore CS0612
#endif

        latchMode = ComputeNextLatchMode(
            latchMode,
            harvestedThingCount,
            values.BoundValueRaw.Lower,
            values.BoundValueRaw.Upper
        );

        plantToGrowSettable.SetHysteresisControlState(ResolveControlState(latchMode));
    }

    /// <summary>
    /// Pure hysteresis transition table - identical to
    /// <c>Trigger_Hysteresis.ComputeNextLatchMode</c> in
    /// <c>Source/FarmingHysteresis.ColonyManagerRedux</c>, the CMR port of this same logic.
    /// Split out here so it's unit-testable without a live grower, and so an unresolved
    /// <see cref="LatchMode.Unknown"/> (e.g. the harvested count sitting exactly at
    /// <paramref name="lower"/> on first evaluation) has a definite fallback instead of getting
    /// stuck.
    /// </summary>
    internal static LatchMode ComputeNextLatchMode(
        LatchMode current,
        int count,
        int lower,
        int upper
    ) =>
        count < lower ? LatchMode.BelowLowerBound
        : count > upper ? LatchMode.AboveUpperBound
        : current switch
        {
            LatchMode.BelowLowerBound or LatchMode.Unknown => count > lower
                ? LatchMode.BetweenBoundsEnabled
                : current,
            LatchMode.AboveUpperBound => count < upper ? LatchMode.BetweenBoundsDisabled : current,
            LatchMode.BetweenBoundsEnabled
            or LatchMode.BetweenBoundsDisabled
#if !v1_5_OR_GREATER
#pragma warning disable 612
            or LatchMode.AboveLowerBoundEnabled
            or LatchMode.AboveLowerBoundDisabled
#pragma warning restore 612
#endif
            => current,
            _ => current,
        };

    /// <summary>
    /// Pure resolution from latch state to hysteresis control state. An unresolved
    /// <see cref="LatchMode.Unknown"/> resolves to disabled rather than throwing, matching
    /// <c>Trigger_Hysteresis.State</c> in <c>Source/FarmingHysteresis.ColonyManagerRedux</c>.
    /// </summary>
    internal static bool ResolveControlState(LatchMode latchMode) =>
        latchMode is LatchMode.BelowLowerBound or LatchMode.BetweenBoundsEnabled;

    internal void DisableDueToMissingHarvestedThingDef(
        IPlantToGrowSettable plantToGrowSettable,
        ThingDef? plantDef
    )
    {
        _enabled = false;

        string settableName;
        var suppressWarning = false;

        if (plantToGrowSettable is Zone zone)
        {
            settableName = $"Zone '{zone.label}'";
        }
        else if (plantToGrowSettable is Building building)
        {
            // Let's not cause unnecessary spam from flower pots and similar
            var sowTag = building.def.building?.sowTag;
            if (sowTag is "Decorative" or "DecorativeTree")
            {
                suppressWarning = true;
            }
            settableName =
                $"Building named '{building.Label}' @ {building.InteractionCell.ToIntVec2}";
        }
        else
        {
            settableName = plantToGrowSettable is ILoadReferenceable loadReferenceable
                ? loadReferenceable.GetUniqueLoadID()
                : $"Unknown type {plantToGrowSettable.GetType().FullName}";
        }

        if (!suppressWarning)
        {
            if (plantDef == null)
            {
                // This should normally never happen, but some mods may make plantDef null.
                FarmingHysteresisMod.Instance.LogWarning(
                    $"{settableName} has no plant set. Disabling farming hysteresis."
                );
            }
            else
            {
                FarmingHysteresisMod.Instance.LogWarning(
                    $"{settableName} has a plant type without a harvestable product ({plantDef.label}). Disabling farming hysteresis."
                );
            }
        }
    }
}
