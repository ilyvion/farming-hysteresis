namespace FarmingHysteresis;

internal class GameThingDefBoundValueAccessor(
    FarmingHysteresisGameComponent gameComponent,
    ThingDef thingDef
) : IBoundedValueAccessor
{
    private readonly FarmingHysteresisGameComponent gameComponent = gameComponent;
    private readonly ThingDef thingDef = thingDef;

    public BoundValues BoundValueRaw
    {
        get
        {
            if (gameComponent.GameBoundValues.TryGetValue(thingDef, out var value))
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
                gameComponent.GameBoundValues.Add(thingDef, boundValues);
                return boundValues;
            }
        }
    }
}

/// <summary>
/// Tracks save-wide (cross-map) hysteresis bounds.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FarmingHysteresisGameComponent"/> class.
/// </remarks>
/// <param name="game">The game this component belongs to.</param>
#pragma warning disable CS9113 // Parameter is unread.
public class FarmingHysteresisGameComponent(Game game) : GameComponent
#pragma warning restore CS9113 // Parameter is unread.
{
    private Dictionary<ThingDef, BoundValues>? gameBoundValues;

    internal Dictionary<ThingDef, BoundValues> GameBoundValues
    {
        get
        {
            gameBoundValues ??= [];
            return gameBoundValues;
        }
    }

    internal bool HasBoundsFor(ThingDef harvestedThingDef) =>
        BoundValuesLookup.HasBounds(gameBoundValues, harvestedThingDef);

    /// <summary>
    /// Gets the <see cref="FarmingHysteresisGameComponent"/> for the given <paramref name="game"/>,
    /// creating and attaching one if it doesn't already exist.
    /// </summary>
    /// <param name="game">The game to get the component for.</param>
    public static FarmingHysteresisGameComponent For(Game game)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        var instance = game.GetComponent<FarmingHysteresisGameComponent>();
        if (instance != null)
        {
            return instance;
        }

        instance = new FarmingHysteresisGameComponent(game);
        game.components.Add(instance);
        return instance;
    }

    internal IBoundedValueAccessor GetGameBoundedValueAccessorFor(ThingDef thingDef) =>
        new GameThingDefBoundValueAccessor(this, thingDef);

    /// <inheritdoc/>
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(
            ref gameBoundValues,
            "gameBoundValues",
            LookMode.Def,
            LookMode.Deep
        );
    }
}
