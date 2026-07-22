using RimTestRedux;

namespace FarmingHysteresis.Tests;

[HotSwappable]
[TestSuite]
internal static class HysteresisBoundClampTests
{
    // Regression test: the main tab's bound widgets used to write player input straight into
    // the live BoundValues via an unclamped Widgets.IntEntry, letting a player set the lower
    // bound above the upper bound (or negative) directly from the main tab.
    [Test]
    public static void ClampLowerRejectsNegativeValues() =>
        Assert.That(HysteresisBoundClamp.ClampLower(-5, 10)).Is.EqualTo(0);

    [Test]
    public static void ClampLowerRejectsValuesAboveUpper() =>
        Assert.That(HysteresisBoundClamp.ClampLower(15, 10)).Is.EqualTo(10);

    [Test]
    public static void ClampLowerAllowsValuesWithinRange() =>
        Assert.That(HysteresisBoundClamp.ClampLower(5, 10)).Is.EqualTo(5);

    [Test]
    public static void ClampUpperRejectsValuesBelowLower() =>
        Assert.That(HysteresisBoundClamp.ClampUpper(2, 5)).Is.EqualTo(5);

    [Test]
    public static void ClampUpperAllowsValuesAtOrAboveLower()
    {
        Assert.That(HysteresisBoundClamp.ClampUpper(5, 5)).Is.EqualTo(5);
        Assert.That(HysteresisBoundClamp.ClampUpper(10, 5)).Is.EqualTo(10);
    }

    // Regression test: the mod settings window and ExposeData used to accept the default
    // lower/upper bounds unclamped, letting a player save inverted defaults (lower > upper)
    // that then seeded every newly created hysteresis control.
    [Test]
    public static void ClampRejectsNegativeLower()
    {
        var (lower, upper) = HysteresisBoundClamp.Clamp(-5, 10);
        Assert.That(lower).Is.EqualTo(0);
        Assert.That(upper).Is.EqualTo(10);
    }

    [Test]
    public static void ClampFixesInvertedBounds()
    {
        var (lower, upper) = HysteresisBoundClamp.Clamp(20, 10);
        Assert.That(lower).Is.EqualTo(10);
        Assert.That(upper).Is.EqualTo(10);
    }

    [Test]
    public static void ClampAllowsValidBounds()
    {
        var (lower, upper) = HysteresisBoundClamp.Clamp(5, 10);
        Assert.That(lower).Is.EqualTo(5);
        Assert.That(upper).Is.EqualTo(10);
    }
}
