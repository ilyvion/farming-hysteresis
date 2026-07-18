using RimTestRedux;
using static FarmingHysteresis.ColonyManagerRedux.Trigger_Hysteresis;
using static FarmingHysteresis.LatchMode;

namespace FarmingHysteresis.ColonyManagerRedux.Tests;

// Regression guard for Trigger_Hysteresis's latch transition table, ported from
// FarmingHysteresisData.UpdateLatchModeAndHandling (see Docs/CMRIntegrationRework.md, Step 2)
// rather than reinvented - these cases mirror the same state machine the default engine has
// used since before this integration existed.
[HotSwappable]
[TestSuite]
internal static class ComputeNextLatchModeTests
{
    [Test]
    public static void BelowLowerBoundIsAlwaysReachedRegardlessOfPreviousState()
    {
        Assert.That(ComputeNextLatchMode(Unknown, count: 5, lower: 10, upper: 20)).Is.EqualTo(BelowLowerBound);
        Assert
            .That(ComputeNextLatchMode(BetweenBoundsEnabled, count: 5, lower: 10, upper: 20))
            .Is.EqualTo(BelowLowerBound);
        Assert
            .That(ComputeNextLatchMode(AboveUpperBound, count: 5, lower: 10, upper: 20))
            .Is.EqualTo(BelowLowerBound);
    }

    [Test]
    public static void AboveUpperBoundIsAlwaysReachedRegardlessOfPreviousState()
    {
        Assert.That(ComputeNextLatchMode(Unknown, count: 25, lower: 10, upper: 20)).Is.EqualTo(AboveUpperBound);
        Assert
            .That(ComputeNextLatchMode(BetweenBoundsDisabled, count: 25, lower: 10, upper: 20))
            .Is.EqualTo(AboveUpperBound);
        Assert
            .That(ComputeNextLatchMode(BelowLowerBound, count: 25, lower: 10, upper: 20))
            .Is.EqualTo(AboveUpperBound);
    }

    [Test]
    public static void RisingFromBelowLowerBoundEntersEnabledOnceStrictlyAboveLower()
    {
        // still at the lower bound exactly: stays in the "was below" state, doesn't yet flip
        Assert
            .That(ComputeNextLatchMode(BelowLowerBound, count: 10, lower: 10, upper: 20))
            .Is.EqualTo(BelowLowerBound);
        // strictly above lower: now enters the enabled latch
        Assert
            .That(ComputeNextLatchMode(BelowLowerBound, count: 11, lower: 10, upper: 20))
            .Is.EqualTo(BetweenBoundsEnabled);
    }

    [Test]
    public static void FallingFromAboveUpperBoundEntersDisabledOnceStrictlyBelowUpper()
    {
        Assert
            .That(ComputeNextLatchMode(AboveUpperBound, count: 20, lower: 10, upper: 20))
            .Is.EqualTo(AboveUpperBound);
        Assert
            .That(ComputeNextLatchMode(AboveUpperBound, count: 19, lower: 10, upper: 20))
            .Is.EqualTo(BetweenBoundsDisabled);
    }

    [Test]
    public static void BetweenBoundsStatesAreSticky()
    {
        Assert
            .That(ComputeNextLatchMode(BetweenBoundsEnabled, count: 15, lower: 10, upper: 20))
            .Is.EqualTo(BetweenBoundsEnabled);
        Assert
            .That(ComputeNextLatchMode(BetweenBoundsDisabled, count: 15, lower: 10, upper: 20))
            .Is.EqualTo(BetweenBoundsDisabled);
    }
}
