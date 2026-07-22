namespace FarmingHysteresis;

/// <summary>
/// Shared clamp rules for a hysteresis lower/upper bound pair: the lower bound is never
/// negative and never above the upper bound; the upper bound is never below the
/// (already-clamped) lower bound. Every bound editor in the mod (main tab, per-grower ITab,
/// mod settings) enforces these same constraints and should go through here rather than
/// duplicating the comparisons.
/// </summary>
internal static class HysteresisBoundClamp
{
    internal static int ClampLower(int lower, int upper)
    {
        if (lower < 0)
        {
            return 0;
        }
        else if (lower > upper)
        {
            return upper;
        }
        return lower;
    }

    internal static int ClampUpper(int upper, int lower) => upper < lower ? lower : upper;

    /// <summary>
    /// Clamps a candidate lower/upper pair together: the lower bound against the raw upper
    /// bound, then the upper bound against the already-clamped lower bound.
    /// </summary>
    internal static (int Lower, int Upper) Clamp(int lower, int upper)
    {
        var clampedLower = ClampLower(lower, upper);
        var clampedUpper = ClampUpper(upper, clampedLower);
        return (clampedLower, clampedUpper);
    }
}
