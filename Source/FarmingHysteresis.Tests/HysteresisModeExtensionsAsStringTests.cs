using DevTools.Testing;

namespace FarmingHysteresis.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class HysteresisModeExtensionsAsStringTests
{
    [Test]
    public static void SowingReturnsNonEmptyString()
    {
        var result = HysteresisMode.Sowing.AsString();
        Expect.IsNotNull(result);
        Expect.AreNotEqual("", result);
    }

    [Test]
    public static void HarvestingReturnsNonEmptyString()
    {
        var result = HysteresisMode.Harvesting.AsString();
        Expect.IsNotNull(result);
        Expect.AreNotEqual("", result);
    }

    [Test]
    public static void SowingAndHarvestingReturnsNonEmptyString()
    {
        var result = HysteresisMode.SowingAndHarvesting.AsString();
        Expect.IsNotNull(result);
        Expect.AreNotEqual("", result);
    }

    [Test]
    public static void UncoveredHysteresisModeThrows() =>
        Expect.Throws<InvalidOperationException>(() => _ = ((HysteresisMode)99).AsString());
}

[TestFixture(TestType.MainMenu)]
internal sealed class HysteresisModeExtensionsControlsTests
{
    [Test]
    public static void SowingControlsSowingOnly()
    {
        Expect.IsTrue(HysteresisMode.Sowing.ControlsSowing());
        Expect.IsFalse(HysteresisMode.Sowing.ControlsHarvesting());
    }

    [Test]
    public static void HarvestingControlsHarvestingOnly()
    {
        Expect.IsFalse(HysteresisMode.Harvesting.ControlsSowing());
        Expect.IsTrue(HysteresisMode.Harvesting.ControlsHarvesting());
    }

    [Test]
    public static void SowingAndHarvestingControlsBoth()
    {
        Expect.IsTrue(HysteresisMode.SowingAndHarvesting.ControlsSowing());
        Expect.IsTrue(HysteresisMode.SowingAndHarvesting.ControlsHarvesting());
    }
}
