[assembly: InternalsVisibleTo("FarmingHysteresis.VanillaPlantsExpandedMorePlants")]
[assembly: InternalsVisibleTo("FarmingHysteresis.ColonyManagerRedux")]
[assembly: InternalsVisibleTo("FarmingHysteresis.SmartFarming")]
[assembly: InternalsVisibleTo("FarmingHysteresis.ReGrowth")]

namespace FarmingHysteresis;

/// <summary>
/// The main mod class for Farming Hysteresis.
/// </summary>
public partial class FarmingHysteresisMod
{
    partial void Construct()
    {
        _ = PatchAll();
        LoadSettingsWhenFinished<Settings>();
    }

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

    /// <summary>
    /// Registers <paramref name="veto"/> in <see cref="AllowSowVeto"/> alongside any veto already
    /// registered: sowing is allowed only when every registered veto allows it.
    /// </summary>
    internal static void AddAllowSowVeto(Func<IPlantToGrowSettable, bool> veto)
    {
        var previousVeto = AllowSowVeto;
        AllowSowVeto = previousVeto == null ? veto : grower => previousVeto(grower) && veto(grower);
    }
}
