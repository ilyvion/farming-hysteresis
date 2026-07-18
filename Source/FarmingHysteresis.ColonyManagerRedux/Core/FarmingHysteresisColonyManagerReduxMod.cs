namespace FarmingHysteresis.ColonyManagerRedux;

/// <summary>
/// The interop mod class enabling the Colony Manager Redux integration for Farming Hysteresis.
/// Only ever loads when Colony Manager Redux is active, via <c>LoadFolders.xml</c>'s
/// <c>IfModActive</c> gating - this mod never needs to check for CMR's presence itself.
/// </summary>
public class FarmingHysteresisColonyManagerReduxMod : Mod
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FarmingHysteresisColonyManagerReduxMod"/> class.
    /// </summary>
    /// <param name="content">The mod content pack.</param>
    public FarmingHysteresisColonyManagerReduxMod(ModContentPack content)
        : base(content)
    {
        FarmingHysteresisMod.Instance.LogMessage(
            "Colony Manager Redux integration loaded successfully!"
        );
    }
}
