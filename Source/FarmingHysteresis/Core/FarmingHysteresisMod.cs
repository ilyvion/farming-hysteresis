[assembly: InternalsVisibleTo("FarmingHysteresis.VanillaPlantsExpandedMorePlants")]
[assembly: InternalsVisibleTo("FarmingHysteresis.ColonyManagerRedux")]
[assembly: InternalsVisibleTo("FarmingHysteresis.SmartFarming")]

namespace FarmingHysteresis;

/// <summary>
/// The main mod class for Farming Hysteresis.
/// </summary>
public class FarmingHysteresisMod : IlyvionMod
{
#pragma warning disable CS8618 // Set by constructor
    /// <summary>
    /// Gets the singleton instance of the <see cref="FarmingHysteresisMod"/> class.
    /// </summary>
    public static FarmingHysteresisMod Instance { get; private set; }
#pragma warning restore CS8618

    /// <summary>
    /// Initializes a new instance of the <see cref="FarmingHysteresisMod"/> class.
    /// </summary>
    /// <param name="content">The mod content pack.</param>
    public FarmingHysteresisMod(ModContentPack content)
        : base(content)
    {
        // This is kind of stupid, but also kind of correct. Correct wins.
        ArgumentNullException.ThrowIfNull(content);

        Instance = this;

        // apply fixes
        var harmony = new Harmony(content.PackageId);
        //Harmony.DEBUG = true;
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        //Harmony.DEBUG = false;

        LongEventHandler.ExecuteWhenFinished(() =>
        {
            // We need to load settings here at the latest because if we end up waiting until during
            //  a game load, it leads to the ScribeLoader exception
            // "Called InitLoading() but current mode is LoadingVars"
            // because you can't Scribe multiple things at once.
            _ = Settings;
        });
    }

    /// <inheritdoc/>
    protected override bool HasSettings => true;

    /// <summary>
    /// Gets the settings for the Farming Hysteresis mod.
    /// </summary>
    public static Settings Settings => Instance.GetSettings<Settings>();

    /// <summary>
    /// Gets the currently active hysteresis controller. Defaults to the mod's own always-on
    /// engine (<see cref="DefaultHysteresisController"/>); may be swapped out by an external
    /// integration, e.g. Colony Manager Redux.
    /// </summary>
    public static IHysteresisController HysteresisController { get; internal set; } =
        DefaultHysteresisController.Instance;

    /// <summary>
    /// Optional veto consulted by <see
    /// cref="Extensions.PlantToGrowSettableExtensions.SetHysteresisControlState"/> (used by both
    /// hysteresis engines) before enabling sowing on a grower - lets a soft-dependency
    /// integration (e.g. the Smart Farming integration's "No petty jobs" cooperation) defer to
    /// another mod's own sow decision for growers it manages. Returns <see langword="true"/> (no
    /// veto) when nothing is registered.
    /// </summary>
    public static Func<IPlantToGrowSettable, bool>? AllowSowVeto { get; internal set; }

    /// <inheritdoc/>
    public override void DoSettingsWindowContents(Rect inRect) =>
        Settings.DoSettingsWindowContents(inRect);

    /// <inheritdoc/>
    public override string SettingsCategory() => Content.Name;
}
