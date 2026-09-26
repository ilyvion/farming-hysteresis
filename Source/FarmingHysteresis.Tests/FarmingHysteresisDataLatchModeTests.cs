using DevTools.Testing;

namespace FarmingHysteresis.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class FarmingHysteresisDataLatchModeTests
{
    [Test]
    public static void UnknownLatchWithCountEqualToLowerStaysUnknownRatherThanEnabling()
    {
        // Regression test: on first evaluation, if the harvested count exactly equals
        // the lower bound, the latch must not spuriously flip to BetweenBoundsEnabled - it should
        // stay Unknown until a real above/below transition resolves it.
        var result = FarmingHysteresisData.ComputeNextLatchMode(
            LatchMode.Unknown,
            count: 5,
            lower: 5,
            upper: 10
        );
        Expect.AreEqual(LatchMode.Unknown, result);
    }

    // Regression test: an unresolved Unknown latch (e.g. count == lower on first
    // evaluation) must resolve to a definite control state (disabled) instead of the old
    // behavior of throwing InvalidOperationException every tick.
    [Test]
    public static void UnresolvedUnknownLatchResolvesToDisabledInsteadOfThrowing() =>
        Expect.IsFalse(FarmingHysteresisData.ResolveControlState(LatchMode.Unknown));

    [Test]
    public static void UnknownLatchWithCountAboveLowerBecomesEnabled()
    {
        var result = FarmingHysteresisData.ComputeNextLatchMode(
            LatchMode.Unknown,
            count: 6,
            lower: 5,
            upper: 10
        );
        Expect.AreEqual(LatchMode.BetweenBoundsEnabled, result);
    }

    [Test]
    public static void CountBelowLowerAlwaysBecomesBelowLowerBound()
    {
        var result = FarmingHysteresisData.ComputeNextLatchMode(
            LatchMode.BetweenBoundsDisabled,
            count: 3,
            lower: 5,
            upper: 10
        );
        Expect.AreEqual(LatchMode.BelowLowerBound, result);
    }

    [Test]
    public static void CountAboveUpperAlwaysBecomesAboveUpperBound()
    {
        var result = FarmingHysteresisData.ComputeNextLatchMode(
            LatchMode.BetweenBoundsEnabled,
            count: 11,
            lower: 5,
            upper: 10
        );
        Expect.AreEqual(LatchMode.AboveUpperBound, result);
    }

    [Test]
    public static void ResolveControlStateEnablesForBelowLowerAndBetweenBoundsEnabled()
    {
        Expect.IsTrue(FarmingHysteresisData.ResolveControlState(LatchMode.BelowLowerBound));
        Expect.IsTrue(FarmingHysteresisData.ResolveControlState(LatchMode.BetweenBoundsEnabled));
    }

    [Test]
    public static void ResolveControlStateDisablesForAboveUpperAndBetweenBoundsDisabled()
    {
        Expect.IsFalse(FarmingHysteresisData.ResolveControlState(LatchMode.AboveUpperBound));
        Expect.IsFalse(FarmingHysteresisData.ResolveControlState(LatchMode.BetweenBoundsDisabled));
    }
}
