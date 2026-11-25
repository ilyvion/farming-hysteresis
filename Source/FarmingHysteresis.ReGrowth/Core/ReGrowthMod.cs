namespace FarmingHysteresis.ReGrowth;

/// <summary>
/// The interop mod class enabling Farming Hysteresis cooperation with the copy of Smart Farming
/// bundled into ReGrowth 2.
/// </summary>
public class ReGrowthMod : IlyvionMod
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReGrowthMod"/> class.
    /// </summary>
    /// <param name="content">The mod content pack.</param>
    public ReGrowthMod(ModContentPack content)
        : base(content)
    {
        _ = PatchAll(Constants.Id);

        FarmingHysteresisMod.AddAllowSowVeto(ReGrowthSmartFarmingIntegration.Cooperation.AllowSow);

        FarmingHysteresisMod.Instance.LogMessage("\"ReGrowth 2\" interop loaded successfully!");
    }
}
