namespace FarmingHysteresis.ColonyManagerRedux;

/// <summary>
/// This integration's own progress bar textures - CMR's own <c>Resources</c> class (which
/// <see cref="global::ColonyManagerRedux.Trigger_Threshold"/> draws its bars with) is
/// <c>internal</c> to the core assembly and not visible here, so
/// <see cref="Trigger_Hysteresis"/> needs its own copy. Colors match CMR's own bar textures for
/// visual consistency with the rest of the manager tab.
/// </summary>
[StaticConstructorOnStartup]
internal static class Resources
{
    public static readonly Texture2D BarBackgroundActiveTexture = SolidColorMaterials.NewSolidColorTexture(
            new Color(0.2f, 0.8f, 0.85f)
        ),
        BarBackgroundInactiveTexture = SolidColorMaterials.NewSolidColorTexture(
            new Color(0.7f, 0.7f, 0.7f)
        );
}
