namespace FarmingHysteresis.VanillaExpandedFramework;

/// <summary>
/// The interop mod class enabling Farming Hysteresis' dual-crop (secondary product) support for
/// Vanilla Expanded Framework. Only ever loads when VEF is active, via <c>LoadFolders.xml</c>'s
/// <c>IfModActive</c> gating - this mod never needs to check for VEF's presence itself. Unrelated
/// to, and loads independently of, the Colony Manager Redux integration - the only thing it
/// contributes is a <see cref="FarmingHysteresis.Defs.SecondaryProductResolverDef"/> instance,
/// which is consumed elsewhere if and only if something (currently only the CMR integration)
/// asks for it.
/// </summary>
public class VanillaExpandedFrameworkMod : Mod
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VanillaExpandedFrameworkMod"/> class.
    /// </summary>
    /// <param name="content">The mod content pack.</param>
    public VanillaExpandedFrameworkMod(ModContentPack content)
        : base(content)
    {
        FarmingHysteresisMod.Instance.LogMessage(
            "Vanilla Expanded Framework dual-crop support loaded successfully!"
        );
    }
}
