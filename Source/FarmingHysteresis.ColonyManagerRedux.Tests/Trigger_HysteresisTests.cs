using RimTestRedux;
using static FarmingHysteresis.ColonyManagerRedux.Trigger_Hysteresis;
using static FarmingHysteresis.LatchMode;

namespace FarmingHysteresis.ColonyManagerRedux.Tests;

// Covers Trigger_Hysteresis's latch transition table, which mirrors the same state machine used
// by FarmingHysteresisData.UpdateLatchModeAndHandling in the default (non-CMR) engine.
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

// Covers the pure scaling math shared by the lower/upper bound progress bar marks in
// Trigger_Hysteresis.DrawVerticalProgressBars/DrawHorizontalProgressBars and the base bar fill,
// in isolation from the live IMGUI draw calls it's normally fed by.
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

// Covers Trigger_Hysteresis's "tracked filter follows target plant" seeding - the filter's own
// ThingFilter mutation logic, tested directly against a bare ThingFilter rather than a live
// job/trigger/map.
[HotSwappable]
[TestSuite]
internal static class SyncFilterToSingleDefTests
{
    [Test]
    public static void AllowsOnlyTheGivenDef()
    {
        var beer = new ThingDef();
        var filter = new ThingFilter();

        SyncFilterToSingleDef(filter, beer);

        Assert.ThatCollection(filter.AllowedThingDefs).Has.Count(1);
        Assert.ThatCollection(filter.AllowedThingDefs).Does.Contain(beer);
    }

    [Test]
    public static void ReplacesWhateverWasPreviouslyAllowed()
    {
        var hops = new ThingDef();
        var beer = new ThingDef();
        var filter = new ThingFilter();
        SyncFilterToSingleDef(filter, hops);

        SyncFilterToSingleDef(filter, beer);

        Assert.ThatCollection(filter.AllowedThingDefs).Has.Count(1);
        Assert.ThatCollection(filter.AllowedThingDefs).Does.Contain(beer);
        Assert.ThatCollection(filter.AllowedThingDefs).Does.Not.Contain(hops);
    }

    [Test]
    public static void ClearsToDisallowAllWhenDefIsNull()
    {
        var hops = new ThingDef();
        var filter = new ThingFilter();
        SyncFilterToSingleDef(filter, hops);

        SyncFilterToSingleDef(filter, null);

        Assert.ThatCollection(filter.AllowedThingDefs).Is.Empty();
    }
}

// Covers SyncFilterToDefs, the multi-def generalization that SyncFilterToSingleDef is a one-def
// special case of.
[HotSwappable]
[TestSuite]
internal static class SyncFilterToDefsTests
{
    [Test]
    public static void AllowsEveryGivenDef()
    {
        var hops = new ThingDef();
        var beer = new ThingDef();
        var filter = new ThingFilter();

        SyncFilterToDefs(filter, [hops, beer]);

        Assert.ThatCollection(filter.AllowedThingDefs).Has.Count(2);
        Assert.ThatCollection(filter.AllowedThingDefs).Does.Contain(hops);
        Assert.ThatCollection(filter.AllowedThingDefs).Does.Contain(beer);
    }

    [Test]
    public static void ReplacesWhateverWasPreviouslyAllowed()
    {
        var hops = new ThingDef();
        var beer = new ThingDef();
        var filter = new ThingFilter();
        SyncFilterToDefs(filter, [hops]);

        SyncFilterToDefs(filter, [beer]);

        Assert.ThatCollection(filter.AllowedThingDefs).Has.Count(1);
        Assert.ThatCollection(filter.AllowedThingDefs).Does.Contain(beer);
        Assert.ThatCollection(filter.AllowedThingDefs).Does.Not.Contain(hops);
    }

    [Test]
    public static void ClearsToDisallowAllWhenDefsIsEmpty()
    {
        var hops = new ThingDef();
        var filter = new ThingFilter();
        SyncFilterToDefs(filter, [hops]);

        SyncFilterToDefs(filter, []);

        Assert.ThatCollection(filter.AllowedThingDefs).Is.Empty();
    }
}

// Covers Trigger_Hysteresis's crop-rotation advance decision: only a fresh transition into
// AboveUpperBound should advance the rotation, and only when there's more than one crop to
// rotate through - otherwise a single-crop job must sit disabled once over Upper, indefinitely,
// rather than advancing.
[HotSwappable]
[TestSuite]
internal static class ShouldAdvanceRotationTests
{
    [Test]
    public static void DoesNotAdvanceWithoutMultipleRotationEntries()
    {
        Assert
            .That(
                ShouldAdvanceRotation(BetweenBoundsEnabled, AboveUpperBound, rotationEntryCount: 0)
            )
            .Is.False();
        Assert
            .That(
                ShouldAdvanceRotation(BetweenBoundsEnabled, AboveUpperBound, rotationEntryCount: 1)
            )
            .Is.False();
    }

    [Test]
    public static void AdvancesOnFreshTransitionIntoAboveUpperBoundWithMultipleEntries()
    {
        Assert
            .That(
                ShouldAdvanceRotation(BetweenBoundsEnabled, AboveUpperBound, rotationEntryCount: 2)
            )
            .Is.True();
        Assert
            .That(ShouldAdvanceRotation(BelowLowerBound, AboveUpperBound, rotationEntryCount: 2))
            .Is.True();
    }

    // Sticky - already over the bound last cycle too, so this isn't a fresh transition.
    [Test]
    public static void DoesNotAdvanceWhenAlreadyAboveUpperBound() =>
        Assert
            .That(ShouldAdvanceRotation(AboveUpperBound, AboveUpperBound, rotationEntryCount: 2))
            .Is.False();

    [Test]
    public static void DoesNotAdvanceWhenNotAboveUpperBound() =>
        Assert
            .That(
                ShouldAdvanceRotation(BelowLowerBound, BetweenBoundsEnabled, rotationEntryCount: 2)
            )
            .Is.False();
}

// Covers DescribeTrackedCountNoun's pluralizable-noun logic, tested directly against a bare
// ThingFilter rather than a live job/trigger.
[HotSwappable]
[TestSuite]
internal static class DescribeTrackedCountNounTests
{
    [Test]
    public static void SingleAllowedDefReturnsItsLabel()
    {
        var beer = new ThingDef { label = "beer" };
        var filter = new ThingFilter();
        SyncFilterToSingleDef(filter, beer);

        var result = DescribeTrackedCountNoun(filter);

        Assert.That(result).Is.EqualTo("beer");
    }

    [Test]
    public static void NoAllowedDefsReturnsTheGenericNoun()
    {
        var filter = new ThingFilter();

        var result = DescribeTrackedCountNoun(filter);

        Assert.That(result).Is.Not.Null();
        Assert.That(result).Is.Not.EqualTo("");
    }

    [Test]
    public static void MultipleAllowedDefsReturnsTheGenericNounNotAnyIndividualLabel()
    {
        var hops = new ThingDef { label = "hops" };
        var beer = new ThingDef { label = "beer" };
        var filter = new ThingFilter();
        SyncFilterToDefs(filter, [hops, beer]);

        var result = DescribeTrackedCountNoun(filter);

        Assert.That(result).Is.Not.EqualTo("hops");
        Assert.That(result).Is.Not.EqualTo("beer");
    }
}

// Covers DescribeTrackedFilter's 3-way None/single-label/Multiple summary, tested directly
// against a bare ThingFilter rather than a live job/trigger.
[HotSwappable]
[TestSuite]
internal static class DescribeTrackedFilterTests
{
    [Test]
    public static void NoAllowedDefsReturnsTheNoneSummary()
    {
        var filter = new ThingFilter();

        var result = DescribeTrackedFilter(filter);

        Assert.That(result).Is.Not.Null();
        Assert.That(result).Is.Not.EqualTo("");
    }

    [Test]
    public static void SingleAllowedDefReturnsItsLabel()
    {
        var beer = new ThingDef { label = "beer" };
        var filter = new ThingFilter();
        SyncFilterToSingleDef(filter, beer);

        var result = DescribeTrackedFilter(filter);

        Assert.That(result).Is.EqualTo("beer");
    }

    [Test]
    public static void MultipleAllowedDefsReturnsAMultipleSummaryMentioningTheCount()
    {
        var hops = new ThingDef { label = "hops" };
        var beer = new ThingDef { label = "beer" };
        var rice = new ThingDef { label = "rice" };
        var filter = new ThingFilter();
        SyncFilterToDefs(filter, [hops, beer, rice]);

        var result = DescribeTrackedFilter(filter);

        Assert.That(result).Is.Not.EqualTo("hops");
        Assert.That(result).Is.Not.EqualTo("beer");
        Assert.That(result).Is.Not.EqualTo("rice");
        Assert.ThatCollection(result.Split(' ')).Does.Contain("3");
    }
}
