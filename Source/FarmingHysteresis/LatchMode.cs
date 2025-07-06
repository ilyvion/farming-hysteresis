namespace FarmingHysteresis;

internal enum LatchMode
{
    // Not yet determined - unknown state
    Unknown,
    // Below lower bound - enabled
    BelowLowerBound,
    // Lower bound met, but not upper bound - enabled
    BetweenBoundsEnabled,
    // Upper bound met, but not yet below lower bound - disabled
    BetweenBoundsDisabled,
    // Above upper bound - disabled
    AboveUpperBound,

#if v1_3 || v1_4
    // We need to keep these around to avoid breaking saves; they get converted to the correct values on load

    [Obsolete]
    AboveLowerBoundEnabled,

    [Obsolete]
    AboveLowerBoundDisabled,
#endif
}
