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

    /// <summary>
    /// The crop rotation list's up/down reorder buttons (see
    /// <c>ManagerTab_FarmingHysteresis.DrawCropRotation</c>). These aren't this mod's own asset -
    /// CMR ships them under this exact path (<c>Common/Textures/UI/Buttons/CMR_Arrow{Up,Down}.png</c>
    /// in the sibling <c>colony-manager-redux</c> repo), and since this integration only ever
    /// loads while CMR is active (<c>IfModActive</c>, see <c>Docs/CMRIntegrationRework.md</c>,
    /// Design decision 4), the path is guaranteed to already be in the shared content pool -
    /// no need to duplicate the asset.
    /// </summary>
    public static readonly Texture2D ArrowUp = ContentFinder<Texture2D>.Get("UI/Buttons/CMR_ArrowUp"),
        ArrowDown = ContentFinder<Texture2D>.Get("UI/Buttons/CMR_ArrowDown");
}
