using RimTestRedux;

namespace FarmingHysteresis.Tests;

[HotSwappable]
[TestSuite]
internal static class MainTabWindowHysteresisMatchesQuickSearchTests
{
    // Regression test: the pre-1.6 branch used a plain ordinal Contains, so typing "Rice"
    // matched nothing against a lowercase def label like "rice" while the 1.6 branch (which
    // used Contains(string, StringComparison.OrdinalIgnoreCase)) matched fine.
    [Test]
    public static void DifferentCaseMatches() =>
        Assert.That(MainTabWindow_Hysteresis.MatchesQuickSearch("rice", "Rice")).Is.True();

    [Test]
    public static void SameCaseMatches() =>
        Assert.That(MainTabWindow_Hysteresis.MatchesQuickSearch("rice", "rice")).Is.True();

    [Test]
    public static void SubstringMatches() =>
        Assert.That(MainTabWindow_Hysteresis.MatchesQuickSearch("Devilstrand", "strand")).Is.True();

    [Test]
    public static void NonMatchingTextDoesNotMatch() =>
        Assert.That(MainTabWindow_Hysteresis.MatchesQuickSearch("rice", "corn")).Is.False();

    [Test]
    public static void EmptyFilterMatchesEverything() =>
        Assert.That(MainTabWindow_Hysteresis.MatchesQuickSearch("rice", "")).Is.True();
}
