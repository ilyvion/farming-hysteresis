using DevTools.Testing;

namespace FarmingHysteresis.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class MainTabWindowHysteresisMatchesQuickSearchTests
{
    // Regression test: the pre-1.6 branch used a plain ordinal Contains, so typing "Rice"
    // matched nothing against a lowercase def label like "rice" while the 1.6 branch (which
    // used Contains(string, StringComparison.OrdinalIgnoreCase)) matched fine.
    [Test]
    public static void DifferentCaseMatches() =>
        Expect.IsTrue(MainTabWindow_Hysteresis.MatchesQuickSearch("rice", "Rice"));

    [Test]
    public static void SameCaseMatches() =>
        Expect.IsTrue(MainTabWindow_Hysteresis.MatchesQuickSearch("rice", "rice"));

    [Test]
    public static void SubstringMatches() =>
        Expect.IsTrue(MainTabWindow_Hysteresis.MatchesQuickSearch("Devilstrand", "strand"));

    [Test]
    public static void NonMatchingTextDoesNotMatch() =>
        Expect.IsFalse(MainTabWindow_Hysteresis.MatchesQuickSearch("rice", "corn"));

    [Test]
    public static void EmptyFilterMatchesEverything() =>
        Expect.IsTrue(MainTabWindow_Hysteresis.MatchesQuickSearch("rice", ""));
}
