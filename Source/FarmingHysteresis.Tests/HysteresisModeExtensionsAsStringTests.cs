using RimTestRedux;

namespace FarmingHysteresis.Tests;

[HotSwappable]
[TestSuite]
internal static class HysteresisModeExtensionsAsStringTests
{
    [Test]
    public static void SowingReturnsNonEmptyString()
    {
        var result = HysteresisMode.Sowing.AsString();
        Assert.That(result).Is.Not.Null();
        Assert.That(result).Is.Not.EqualTo("");
    }

    [Test]
    public static void HarvestingReturnsNonEmptyString()
    {
        var result = HysteresisMode.Harvesting.AsString();
        Assert.That(result).Is.Not.Null();
        Assert.That(result).Is.Not.EqualTo("");
    }

    [Test]
    public static void SowingAndHarvestingReturnsNonEmptyString()
    {
        var result = HysteresisMode.SowingAndHarvesting.AsString();
        Assert.That(result).Is.Not.Null();
        Assert.That(result).Is.Not.EqualTo("");
    }

    [Test]
    [ShouldThrow(typeof(InvalidOperationException))]
    public static void UncoveredHysteresisModeThrows() => _ = ((HysteresisMode)99).AsString();
}
