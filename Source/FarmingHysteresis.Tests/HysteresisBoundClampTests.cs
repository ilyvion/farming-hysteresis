using DevTools.Testing;

namespace FarmingHysteresis.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class HysteresisBoundClampTests
{
    // Regression test: the main tab's bound widgets used to write player input straight into
    // the live BoundValues via an unclamped Widgets.IntEntry, letting a player set the lower
    // bound above the upper bound (or negative) directly from the main tab.
    [Test]
    public static void ClampLowerRejectsNegativeValues() =>
        Expect.AreEqual(0, HysteresisBoundClamp.ClampLower(-5, 10));

    [Test]
    public static void ClampLowerRejectsValuesAboveUpper() =>
        Expect.AreEqual(10, HysteresisBoundClamp.ClampLower(15, 10));

    [Test]
    public static void ClampLowerAllowsValuesWithinRange() =>
        Expect.AreEqual(5, HysteresisBoundClamp.ClampLower(5, 10));

    [Test]
    public static void ClampUpperRejectsValuesBelowLower() =>
        Expect.AreEqual(5, HysteresisBoundClamp.ClampUpper(2, 5));

    [Test]
    public static void ClampUpperAllowsValuesAtOrAboveLower()
    {
        Expect.AreEqual(5, HysteresisBoundClamp.ClampUpper(5, 5));
        Expect.AreEqual(10, HysteresisBoundClamp.ClampUpper(10, 5));
    }

    // Regression test: the mod settings window and ExposeData used to accept the default
    // lower/upper bounds unclamped, letting a player save inverted defaults (lower > upper)
    // that then seeded every newly created hysteresis control.
    [Test]
    public static void ClampRejectsNegativeLower()
    {
        var (lower, upper) = HysteresisBoundClamp.Clamp(-5, 10);
        Expect.AreEqual(0, lower);
        Expect.AreEqual(10, upper);
    }

    [Test]
    public static void ClampFixesInvertedBounds()
    {
        var (lower, upper) = HysteresisBoundClamp.Clamp(20, 10);
        Expect.AreEqual(10, lower);
        Expect.AreEqual(10, upper);
    }

    [Test]
    public static void ClampAllowsValidBounds()
    {
        var (lower, upper) = HysteresisBoundClamp.Clamp(5, 10);
        Expect.AreEqual(5, lower);
        Expect.AreEqual(10, upper);
    }
}
