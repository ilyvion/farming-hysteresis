namespace FarmingHysteresis;

/// <summary>
/// Shared UI helpers for picking a <see cref="BoundsSource"/>, used by the ITab button, the
/// legacy gizmo, and the mod settings window.
/// </summary>
internal static class BoundsSourceUi
{
    /// <summary>
    /// Gets the translated, human-readable name of the given <paramref name="source"/>.
    /// </summary>
    internal static string Label(BoundsSource source) =>
        source switch
        {
            BoundsSource.Self => "FarmingHysteresis.BoundsSourceSelf".Translate(),
            BoundsSource.Map => "FarmingHysteresis.BoundsSourceMap".Translate(),
            BoundsSource.Game => "FarmingHysteresis.BoundsSourceGame".Translate(),
            _ => throw new InvalidOperationException($"Uncovered BoundsSource: {source}."),
        };

    /// <summary>
    /// Decides whether switching a grower to <paramref name="newSource"/> should seed that
    /// tier's storage with the grower's currently-resolved bound values, versus leaving an
    /// already-populated shared bucket untouched.
    /// </summary>
    /// <param name="newSource">The tier being switched to.</param>
    /// <param name="destinationHasExistingBounds">Whether the destination tier already has
    /// stored bounds for this harvest type (irrelevant for <see cref="BoundsSource.Self"/>,
    /// whose storage always already exists).</param>
    internal static bool ShouldSeedOnSwitch(
        BoundsSource newSource,
        bool destinationHasExistingBounds
    ) =>
        newSource switch
        {
            BoundsSource.Self => false,
            BoundsSource.Map or BoundsSource.Game => !destinationHasExistingBounds,
            _ => throw new InvalidOperationException($"Uncovered BoundsSource: {newSource}."),
        };

    /// <summary>
    /// Opens a Self/Map/Game <see cref="FloatMenu"/> for <paramref name="data"/>, switching its
    /// <see cref="FarmingHysteresisData.boundsSource"/> and, the first time a shared tier is used
    /// for this harvest type, seeding it from the grower's current bound values.
    /// </summary>
    internal static void OpenFloatMenu(
        FarmingHysteresisData data,
        IPlantToGrowSettable plantGrower,
        ThingDef harvestedThingDef,
        Action? onSwitched = null
    )
    {
        List<FloatMenuOption> options =
        [
            new(Label(BoundsSource.Self), () => Switch(BoundsSource.Self)),
            new(Label(BoundsSource.Map), () => Switch(BoundsSource.Map)),
            new(Label(BoundsSource.Game), () => Switch(BoundsSource.Game)),
        ];
        Find.WindowStack.Add(new FloatMenu(options));

        void Switch(BoundsSource newSource)
        {
            if (data.boundsSource == newSource)
            {
                return;
            }

            var currentLower = data.LowerBound;
            var currentUpper = data.UpperBound;

            var destinationHasExistingBounds = newSource switch
            {
                BoundsSource.Self => true,
                BoundsSource.Map => FarmingHysteresisMapComponent
                    .For(plantGrower.Map)
                    .HasBoundsFor(harvestedThingDef),
                BoundsSource.Game => FarmingHysteresisGameComponent
                    .For(Current.Game)
                    .HasBoundsFor(harvestedThingDef),
                _ => throw new InvalidOperationException($"Uncovered BoundsSource: {newSource}."),
            };
            var shouldSeed = ShouldSeedOnSwitch(newSource, destinationHasExistingBounds);

            data.boundsSource = newSource;

            if (shouldSeed)
            {
                data.LowerBound = currentLower;
                data.UpperBound = currentUpper;
            }

            onSwitched?.Invoke();
        }
    }
}
