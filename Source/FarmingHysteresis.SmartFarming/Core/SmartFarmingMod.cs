namespace FarmingHysteresis.SmartFarming;

/// <summary>
/// The interop mod class enabling Farming Hysteresis cooperation with Smart Farming's "No petty
/// jobs" per-zone setting.
/// </summary>
public class SmartFarmingMod : IlyvionMod
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SmartFarmingMod"/> class.
    /// </summary>
    /// <param name="content">The mod content pack.</param>
    public SmartFarmingMod(ModContentPack content)
        : base(content)
    {
        _ = PatchAll(Constants.Id);

        FarmingHysteresisMod.AddAllowSowVeto(SmartFarmingIntegration.AllowSow);

        FarmingHysteresisMod.Instance.LogMessage("\"Smart Farming\" interop loaded successfully!");
    }
}
