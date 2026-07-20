namespace FarmingHysteresis;

/// <summary>
/// Helpers that paper over UI API differences between supported RimWorld versions.
/// </summary>
internal static class UiCompat
{
    /// <summary>
    /// Draws a label/button row, using <c>Listing_Standard.ButtonTextLabeledPct</c> where
    /// available (1.4+) and falling back to <see cref="Listing_Standard.ButtonTextLabeled"/> on
    /// 1.3, which doesn't have the percentage-based overload.
    /// </summary>
    internal static bool ButtonTextLabeledCompat(
        this Listing_Standard listingStandard,
        string label,
        string buttonLabel
    ) =>
#if v1_3
        listingStandard.ButtonTextLabeled(label, buttonLabel);
#else
        listingStandard.ButtonTextLabeledPct(label, buttonLabel, 0.6f, TextAnchor.MiddleLeft);
#endif
}
