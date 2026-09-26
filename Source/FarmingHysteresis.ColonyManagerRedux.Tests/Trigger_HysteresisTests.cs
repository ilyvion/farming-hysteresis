using DevTools.Testing;
using static FarmingHysteresis.ColonyManagerRedux.Trigger_Hysteresis;
using static FarmingHysteresis.LatchMode;

namespace FarmingHysteresis.ColonyManagerRedux.Tests;

// Covers Trigger_Hysteresis's latch transition table, which mirrors the same state machine used
// by FarmingHysteresisData.UpdateLatchModeAndHandling in the default (non-CMR) engine.
[TestFixture(TestType.MainMenu)]
internal sealed class ComputeNextLatchModeTests
{
    [Test]
    public static void BelowLowerBoundIsAlwaysReachedRegardlessOfPreviousState()
    {
        Expect.AreEqual(
            BelowLowerBound,
            ComputeNextLatchMode(Unknown, count: 5, lower: 10, upper: 20)
        );
        Expect.AreEqual(
            BelowLowerBound,
            ComputeNextLatchMode(BetweenBoundsEnabled, count: 5, lower: 10, upper: 20)
        );
        Expect.AreEqual(
            BelowLowerBound,
            ComputeNextLatchMode(AboveUpperBound, count: 5, lower: 10, upper: 20)
        );
    }

    [Test]
    public static void AboveUpperBoundIsAlwaysReachedRegardlessOfPreviousState()
    {
        Expect.AreEqual(
            AboveUpperBound,
            ComputeNextLatchMode(Unknown, count: 25, lower: 10, upper: 20)
        );
        Expect.AreEqual(
            AboveUpperBound,
            ComputeNextLatchMode(BetweenBoundsDisabled, count: 25, lower: 10, upper: 20)
        );
        Expect.AreEqual(
            AboveUpperBound,
            ComputeNextLatchMode(BelowLowerBound, count: 25, lower: 10, upper: 20)
        );
    }

    [Test]
    public static void RisingFromBelowLowerBoundEntersEnabledOnceStrictlyAboveLower()
    {
        // still at the lower bound exactly: stays in the "was below" state, doesn't yet flip
        Expect.AreEqual(
            BelowLowerBound,
            ComputeNextLatchMode(BelowLowerBound, count: 10, lower: 10, upper: 20)
        );
        // strictly above lower: now enters the enabled latch
        Expect.AreEqual(
            BetweenBoundsEnabled,
            ComputeNextLatchMode(BelowLowerBound, count: 11, lower: 10, upper: 20)
        );
    }

    [Test]
    public static void FallingFromAboveUpperBoundEntersDisabledOnceStrictlyBelowUpper()
    {
        Expect.AreEqual(
            AboveUpperBound,
            ComputeNextLatchMode(AboveUpperBound, count: 20, lower: 10, upper: 20)
        );
        Expect.AreEqual(
            BetweenBoundsDisabled,
            ComputeNextLatchMode(AboveUpperBound, count: 19, lower: 10, upper: 20)
        );
    }

    [Test]
    public static void BetweenBoundsStatesAreSticky()
    {
        Expect.AreEqual(
            BetweenBoundsEnabled,
            ComputeNextLatchMode(BetweenBoundsEnabled, count: 15, lower: 10, upper: 20)
        );
        Expect.AreEqual(
            BetweenBoundsDisabled,
            ComputeNextLatchMode(BetweenBoundsDisabled, count: 15, lower: 10, upper: 20)
        );
    }

    // Regression guard for the "unbound" last-crop fallback: it must never latch into
    // AboveUpperBound/BetweenBoundsDisabled, since both would turn growing off - the whole point
    // is to keep growing forever once reached, rather than stopping once its stock passes upper.
    [Test]
    public static void UnboundNeverLatchesAboveUpperBoundRegardlessOfCount()
    {
        Expect.AreEqual(
            BetweenBoundsEnabled,
            ComputeNextLatchMode(
                BetweenBoundsEnabled,
                count: 25,
                lower: 10,
                upper: 20,
                unbound: true
            )
        );
        Expect.AreEqual(
            BetweenBoundsEnabled,
            ComputeNextLatchMode(Unknown, count: 25, lower: 10, upper: 20, unbound: true)
        );
    }

    // An entry marked unbound after it had already latched above its own upper bound must
    // immediately resume growing rather than staying stuck disabled - unbound is self-healing,
    // not just forward-looking.
    [Test]
    public static void UnboundSelfHealsAnAlreadyDisabledLatch() =>
        Expect.AreEqual(
            BetweenBoundsEnabled,
            ComputeNextLatchMode(AboveUpperBound, count: 25, lower: 10, upper: 20, unbound: true)
        );

    [Test]
    public static void UnboundStillFallsToBelowLowerBoundWhenCountDropsUnderLower() =>
        Expect.AreEqual(
            BelowLowerBound,
            ComputeNextLatchMode(
                BetweenBoundsEnabled,
                count: 5,
                lower: 10,
                upper: 20,
                unbound: true
            )
        );
}

// Covers Trigger_Hysteresis.IsEffectivelyUnbound - CropRotationEntry.Unbound only takes effect
// for the actual last rotation entry, so a stale flag left on an entry that used to be last (but
// isn't anymore) must never be acted on.
[TestFixture(TestType.MainMenu)]
internal sealed class IsEffectivelyUnboundTests
{
    [Test]
    public static void TrueForTheLastEntryMarkedUnbound()
    {
        var first = new CropRotationEntry();
        var last = new CropRotationEntry { Unbound = true };
        List<CropRotationEntry> entries = [first, last];

        Expect.IsTrue(IsEffectivelyUnbound(last, entries));
    }

    [Test]
    public static void FalseForTheLastEntryNotMarkedUnbound()
    {
        var last = new CropRotationEntry();
        List<CropRotationEntry> entries = [last];

        Expect.IsFalse(IsEffectivelyUnbound(last, entries));
    }

    [Test]
    public static void FalseForAnEarlierEntryEvenIfMarkedUnbound()
    {
        var earlier = new CropRotationEntry { Unbound = true };
        var last = new CropRotationEntry();
        List<CropRotationEntry> entries = [earlier, last];

        Expect.IsFalse(IsEffectivelyUnbound(earlier, entries));
    }
}

// Covers the pure scaling math shared by the lower/upper bound progress bar marks in
// Trigger_Hysteresis.DrawVerticalProgressBars/DrawHorizontalProgressBars and the base bar fill,
// in isolation from the live IMGUI draw calls it's normally fed by.
[TestFixture(TestType.MainMenu)]
internal sealed class ComputeMarkPositionTests
{
    [Test]
    public static void ValueAtZeroIsAtTheStartOfTheBar() =>
        Expect.AreEqual(
            0f,
            ComputeMarkPosition(value: 0, maxValue: 20, currentValue: 5, barLength: 100)
        );

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

        Expect.LessThan(markPosition, 100f);
        Expect.GreaterThan(markPosition, 0f);
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

        Expect.AreEqual(upperMark / 2, lowerMark);
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

        Expect.LessThan(markWithOvershoot, markWithoutOvershoot);
    }
}

// Covers Trigger_Hysteresis's "tracked filter follows target plant" seeding - the filter's own
// ThingFilter mutation logic, tested directly against a bare ThingFilter rather than a live
// job/trigger/map.
[TestFixture(TestType.MainMenu)]
internal sealed class SyncFilterToSingleDefTests
{
    [Test]
    public static void AllowsOnlyTheGivenDef()
    {
        var beer = new ThingDef();
        var filter = new ThingFilter();

        SyncFilterToSingleDef(filter, beer);

        Expect.AreEqual(1, filter.AllowedThingDefs.Count());
        Expect.IsTrue(filter.AllowedThingDefs.Contains(beer));
    }

    [Test]
    public static void ReplacesWhateverWasPreviouslyAllowed()
    {
        var hops = new ThingDef();
        var beer = new ThingDef();
        var filter = new ThingFilter();
        SyncFilterToSingleDef(filter, hops);

        SyncFilterToSingleDef(filter, beer);

        Expect.AreEqual(1, filter.AllowedThingDefs.Count());
        Expect.IsTrue(filter.AllowedThingDefs.Contains(beer));
        Expect.IsFalse(filter.AllowedThingDefs.Contains(hops));
    }

    [Test]
    public static void ClearsToDisallowAllWhenDefIsNull()
    {
        var hops = new ThingDef();
        var filter = new ThingFilter();
        SyncFilterToSingleDef(filter, hops);

        SyncFilterToSingleDef(filter, null);

        Expect.IsEmpty(filter.AllowedThingDefs);
    }
}

// Covers SyncFilterToDefs, the multi-def generalization that SyncFilterToSingleDef is a one-def
// special case of.
[TestFixture(TestType.MainMenu)]
internal sealed class SyncFilterToDefsTests
{
    [Test]
    public static void AllowsEveryGivenDef()
    {
        var hops = new ThingDef();
        var beer = new ThingDef();
        var filter = new ThingFilter();

        SyncFilterToDefs(filter, [hops, beer]);

        Expect.AreEqual(2, filter.AllowedThingDefs.Count());
        Expect.IsTrue(filter.AllowedThingDefs.Contains(hops));
        Expect.IsTrue(filter.AllowedThingDefs.Contains(beer));
    }

    [Test]
    public static void ReplacesWhateverWasPreviouslyAllowed()
    {
        var hops = new ThingDef();
        var beer = new ThingDef();
        var filter = new ThingFilter();
        SyncFilterToDefs(filter, [hops]);

        SyncFilterToDefs(filter, [beer]);

        Expect.AreEqual(1, filter.AllowedThingDefs.Count());
        Expect.IsTrue(filter.AllowedThingDefs.Contains(beer));
        Expect.IsFalse(filter.AllowedThingDefs.Contains(hops));
    }

    [Test]
    public static void ClearsToDisallowAllWhenDefsIsEmpty()
    {
        var hops = new ThingDef();
        var filter = new ThingFilter();
        SyncFilterToDefs(filter, [hops]);

        SyncFilterToDefs(filter, []);

        Expect.IsEmpty(filter.AllowedThingDefs);
    }
}

// Covers Trigger_Hysteresis's crop-rotation advance decision: only a fresh transition into
// AboveUpperBound should advance the rotation, and only when there's more than one crop to
// rotate through - otherwise a single-crop job must sit disabled once over Upper, indefinitely,
// rather than advancing.
[TestFixture(TestType.MainMenu)]
internal sealed class ShouldAdvanceRotationTests
{
    [Test]
    public static void DoesNotAdvanceWithoutMultipleRotationEntries()
    {
        Expect.IsFalse(
            ShouldAdvanceRotation(BetweenBoundsEnabled, AboveUpperBound, rotationEntryCount: 0)
        );
        Expect.IsFalse(
            ShouldAdvanceRotation(BetweenBoundsEnabled, AboveUpperBound, rotationEntryCount: 1)
        );
    }

    [Test]
    public static void AdvancesOnFreshTransitionIntoAboveUpperBoundWithMultipleEntries()
    {
        Expect.IsTrue(
            ShouldAdvanceRotation(BetweenBoundsEnabled, AboveUpperBound, rotationEntryCount: 2)
        );
        Expect.IsTrue(
            ShouldAdvanceRotation(BelowLowerBound, AboveUpperBound, rotationEntryCount: 2)
        );
    }

    // Sticky - already over the bound last cycle too, so this isn't a fresh transition.
    [Test]
    public static void DoesNotAdvanceWhenAlreadyAboveUpperBound() =>
        Expect.IsFalse(
            ShouldAdvanceRotation(AboveUpperBound, AboveUpperBound, rotationEntryCount: 2)
        );

    [Test]
    public static void DoesNotAdvanceWhenNotAboveUpperBound() =>
        Expect.IsFalse(
            ShouldAdvanceRotation(BelowLowerBound, BetweenBoundsEnabled, rotationEntryCount: 2)
        );
}

// Covers DescribeTrackedCountNoun's pluralizable-noun logic, tested directly against a bare
// ThingFilter rather than a live job/trigger.
[TestFixture(TestType.MainMenu)]
internal sealed class DescribeTrackedCountNounTests
{
    [Test]
    public static void SingleAllowedDefReturnsItsLabel()
    {
        var beer = new ThingDef { label = "beer" };
        var filter = new ThingFilter();
        SyncFilterToSingleDef(filter, beer);

        var result = DescribeTrackedCountNoun(filter);

        Expect.AreEqual("beer", result);
    }

    [Test]
    public static void NoAllowedDefsReturnsTheGenericNoun()
    {
        var filter = new ThingFilter();

        var result = DescribeTrackedCountNoun(filter);

        Expect.IsNotNull(result);
        Expect.AreNotEqual("", result);
    }

    [Test]
    public static void MultipleAllowedDefsReturnsTheGenericNounNotAnyIndividualLabel()
    {
        var hops = new ThingDef { label = "hops" };
        var beer = new ThingDef { label = "beer" };
        var filter = new ThingFilter();
        SyncFilterToDefs(filter, [hops, beer]);

        var result = DescribeTrackedCountNoun(filter);

        Expect.AreNotEqual("hops", result);
        Expect.AreNotEqual("beer", result);
    }
}

// Covers ResolveActiveEntryIdAfterCycleUpdate, the fallback ApplyCycleUpdate uses to pick the
// committed active entry id when a rotation entry was removed during the real multi-tick
// gather->execute window, so the cycle's computed NewActiveEntryId can point at an id that no
// longer exists (regression guard: this used to be a bare First(e => e.Id == id) that threw).
[TestFixture(TestType.MainMenu)]
internal sealed class ResolveActiveEntryIdAfterCycleUpdateTests
{
    [Test]
    public static void KeepsTheComputedIdWhenItStillExists()
    {
        var result = ResolveActiveEntryIdAfterCycleUpdate(
            newActiveEntryId: 2,
            currentActiveEntryId: 1,
            remainingEntryIds: [1, 2, 3]
        );

        Expect.AreEqual(2, result);
    }

    [Test]
    public static void FallsBackToTheCurrentActiveIdWhenTheComputedOneWasRemoved()
    {
        var result = ResolveActiveEntryIdAfterCycleUpdate(
            newActiveEntryId: 2,
            currentActiveEntryId: 1,
            remainingEntryIds: [1, 3]
        );

        Expect.AreEqual(1, result);
    }

    [Test]
    public static void FallsBackToWhateverEntryRemainsWhenBothComputedAndCurrentWereRemoved()
    {
        var result = ResolveActiveEntryIdAfterCycleUpdate(
            newActiveEntryId: 2,
            currentActiveEntryId: 1,
            remainingEntryIds: [3, 4]
        );

        Expect.AreEqual(3, result);
    }

    [Test]
    public static void ReturnsNullWhenNoEntriesRemain()
    {
        var result = ResolveActiveEntryIdAfterCycleUpdate(
            newActiveEntryId: 2,
            currentActiveEntryId: 1,
            remainingEntryIds: []
        );

        Expect.IsTrue(result == null);
    }

    [Test]
    public static void KeepsTheCurrentIdWhenNoNewIdWasComputed()
    {
        var result = ResolveActiveEntryIdAfterCycleUpdate(
            newActiveEntryId: null,
            currentActiveEntryId: 1,
            remainingEntryIds: [1, 2]
        );

        Expect.AreEqual(1, result);
    }
}

// Covers DescribeTrackedFilter's 3-way None/single-label/Multiple summary, tested directly
// against a bare ThingFilter rather than a live job/trigger.
[TestFixture(TestType.MainMenu)]
internal sealed class DescribeTrackedFilterTests
{
    [Test]
    public static void NoAllowedDefsReturnsTheNoneSummary()
    {
        var filter = new ThingFilter();

        var result = DescribeTrackedFilter(filter);

        Expect.IsNotNull(result);
        Expect.AreNotEqual("", result);
    }

    [Test]
    public static void SingleAllowedDefReturnsItsLabel()
    {
        var beer = new ThingDef { label = "beer" };
        var filter = new ThingFilter();
        SyncFilterToSingleDef(filter, beer);

        var result = DescribeTrackedFilter(filter);

        Expect.AreEqual("beer", result);
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

        Expect.AreNotEqual("hops", result);
        Expect.AreNotEqual("beer", result);
        Expect.AreNotEqual("rice", result);
        Expect.IsTrue(result.Split(' ').Contains("3"));
    }
}
