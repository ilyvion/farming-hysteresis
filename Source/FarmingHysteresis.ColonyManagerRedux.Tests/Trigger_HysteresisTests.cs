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

// Regression guard for Trigger_Hysteresis's "tracked filter follows target plant" seeding (see
// Docs/CMRIntegrationRework.md, Step 4 - resolves #16): the filter's own ThingFilter mutation
// logic, tested directly against a bare ThingFilter rather than a live job/trigger/map.
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

// Regression guard for the generalization behind Step 6's dual-crop tracking modes (see
// Docs/CMRIntegrationRework.md) - SyncFilterToSingleDef is now a one-def special case of this.
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

// Regression guard for Trigger_Hysteresis's crop-rotation advance decision (see
// Docs/CMRIntegrationRework.md, Step 5 - resolves #6): only a fresh transition into
// AboveUpperBound should advance the rotation, and only when there's more than one crop to
// rotate through - otherwise this integration's original single-crop behavior (sit disabled
// once over Upper, indefinitely) must stay unreachable/unaffected.
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
