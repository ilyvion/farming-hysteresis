using RimTestRedux;

namespace FarmingHysteresis.Tests;

[HotSwappable]
[TestSuite]
internal static class MainTabWindowHysteresisBoundClampTests
{
    // Regression test: the main tab's bound widgets used to write player input straight into
    // the live BoundValues via an unclamped Widgets.IntEntry, letting a player set the lower
    // bound above the upper bound (or negative) directly from the main tab.
    [Test]
    public static void ClampLowerBoundRejectsNegativeValues() =>
        Assert.That(MainTabWindow_Hysteresis.ClampLowerBound(-5, 10)).Is.EqualTo(0);

    [Test]
    public static void ClampLowerBoundRejectsValuesAboveUpper() =>
        Assert.That(MainTabWindow_Hysteresis.ClampLowerBound(15, 10)).Is.EqualTo(10);

    [Test]
    public static void ClampLowerBoundAllowsValuesWithinRange() =>
        Assert.That(MainTabWindow_Hysteresis.ClampLowerBound(5, 10)).Is.EqualTo(5);

    [Test]
    public static void ClampUpperBoundRejectsValuesBelowLower() =>
        Assert.That(MainTabWindow_Hysteresis.ClampUpperBound(2, 5)).Is.EqualTo(5);

    [Test]
    public static void ClampUpperBoundAllowsValuesAtOrAboveLower()
    {
        Assert.That(MainTabWindow_Hysteresis.ClampUpperBound(5, 5)).Is.EqualTo(5);
        Assert.That(MainTabWindow_Hysteresis.ClampUpperBound(10, 5)).Is.EqualTo(10);
    }
}
