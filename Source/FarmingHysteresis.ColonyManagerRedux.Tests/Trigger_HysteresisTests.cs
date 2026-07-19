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
        Assert
            .That(ComputeNextLatchMode(Unknown, count: 5, lower: 10, upper: 20))
            .Is.EqualTo(BelowLowerBound);
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
        Assert
            .That(ComputeNextLatchMode(Unknown, count: 25, lower: 10, upper: 20))
            .Is.EqualTo(AboveUpperBound);
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

// Regression guard for the lower/upper bound progress bar marks added to
// Trigger_Hysteresis.DrawVerticalProgressBars/DrawHorizontalProgressBars (see
// Docs/CMRIntegrationRework.md's "No progress bar drawn yet" follow-up note) - covers the pure
// scaling math the two mark lines share with the base bar fill, in isolation from the live
// IMGUI draw calls it's normally fed by.
[HotSwappable]
[TestSuite]
internal static class ComputeMarkPositionTests
{
    [Test]
    public static void ValueAtZeroIsAtTheStartOfTheBar() =>
        Assert
            .That(ComputeMarkPosition(value: 0, maxValue: 20, currentValue: 5, barLength: 100))
            .Is.EqualTo(0f);

    [Test]
    public static void MaxValueLandsBeforeTheEndOfTheBarToLeaveHeadroom()
    {
        // bar scale always goes a little past maxValue, so maxValue's own mark never touches
        // the very end of the bar (barLength) - matches Trigger.ComputeProgressBarMetrics's own
        // headroom behavior, which this reimplements.
        var markPosition = ComputeMarkPosition(
            value: 20,
            maxValue: 20,
            currentValue: 5,
            barLength: 100
        );

        Assert.That(markPosition).Is.LessThan(100f);
        Assert.That(markPosition).Is.GreaterThan(0f);
    }

    [Test]
    public static void LowerAndUpperMarksShareTheSameScale()
    {
        // Both marks are computed against the same maxValue/currentValue/barLength, so a lower
        // bound half of the upper bound should land at roughly half its mark position.
        var upperMark = ComputeMarkPosition(
            value: 20,
            maxValue: 20,
            currentValue: 5,
            barLength: 100
        );
        var lowerMark = ComputeMarkPosition(
            value: 10,
            maxValue: 20,
            currentValue: 5,
            barLength: 100
        );

        Assert.That(lowerMark).Is.EqualTo(upperMark / 2);
    }

    [Test]
    public static void CurrentValueBeyondMaxValueStillExpandsTheScale()
    {
        // when the tracked count overshoots maxValue, the bar's own scale grows to fit it (see
        // Trigger.ComputeProgressBarMetrics), so the same value's mark position shrinks rather
        // than staying fixed.
        var markWithoutOvershoot = ComputeMarkPosition(
            value: 20,
            maxValue: 20,
            currentValue: 5,
            barLength: 100
        );
        var markWithOvershoot = ComputeMarkPosition(
            value: 20,
            maxValue: 20,
            currentValue: 500,
            barLength: 100
        );

        Assert.That(markWithOvershoot).Is.LessThan(markWithoutOvershoot);
    }
}
