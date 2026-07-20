using RimTestRedux;

namespace FarmingHysteresis.Tests;

[HotSwappable]
[TestSuite]
internal static class FarmingHysteresisDataLatchModeTests
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
        Assert.That(result).Is.EqualTo(LatchMode.Unknown);
    }

    // Regression test: an unresolved Unknown latch (e.g. count == lower on first
    // evaluation) must resolve to a definite control state (disabled) instead of the old
    // behavior of throwing InvalidOperationException every tick.
    [Test]
    public static void UnresolvedUnknownLatchResolvesToDisabledInsteadOfThrowing() =>
        Assert.That(FarmingHysteresisData.ResolveControlState(LatchMode.Unknown)).Is.False();

    [Test]
    public static void UnknownLatchWithCountAboveLowerBecomesEnabled()
    {
        var result = FarmingHysteresisData.ComputeNextLatchMode(
            LatchMode.Unknown,
            count: 6,
            lower: 5,
            upper: 10
        );
        Assert.That(result).Is.EqualTo(LatchMode.BetweenBoundsEnabled);
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
        Assert.That(result).Is.EqualTo(LatchMode.BelowLowerBound);
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
        Assert.That(result).Is.EqualTo(LatchMode.AboveUpperBound);
    }

    [Test]
    public static void ResolveControlStateEnablesForBelowLowerAndBetweenBoundsEnabled()
    {
        Assert.That(FarmingHysteresisData.ResolveControlState(LatchMode.BelowLowerBound)).Is.True();
        Assert
            .That(FarmingHysteresisData.ResolveControlState(LatchMode.BetweenBoundsEnabled))
            .Is.True();
    }

    [Test]
    public static void ResolveControlStateDisablesForAboveUpperAndBetweenBoundsDisabled()
    {
        Assert
            .That(FarmingHysteresisData.ResolveControlState(LatchMode.AboveUpperBound))
            .Is.False();
        Assert
            .That(FarmingHysteresisData.ResolveControlState(LatchMode.BetweenBoundsDisabled))
            .Is.False();
    }
}
