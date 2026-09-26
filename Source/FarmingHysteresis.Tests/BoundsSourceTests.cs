using DevTools.Testing;

namespace FarmingHysteresis.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class BoundsSourceMigrationTests
{
    [Test]
    public static void OldTrueMigratesToMap() =>
        Expect.AreEqual(BoundsSource.Map, BoundsSourceMigration.FromOldUseGlobalValues(true));

    [Test]
    public static void OldFalseMigratesToSelf() =>
        Expect.AreEqual(BoundsSource.Self, BoundsSourceMigration.FromOldUseGlobalValues(false));
}

[TestFixture(TestType.MainMenu)]
internal sealed class BoundsSourceUiShouldSeedOnSwitchTests
{
    [Test]
    public static void SelfIsNeverSeeded()
    {
        Expect.IsFalse(BoundsSourceUi.ShouldSeedOnSwitch(BoundsSource.Self, false));
        Expect.IsFalse(BoundsSourceUi.ShouldSeedOnSwitch(BoundsSource.Self, true));
    }

    [Test]
    public static void MapIsSeededOnlyWhenDestinationHasNoExistingBounds()
    {
        Expect.IsTrue(BoundsSourceUi.ShouldSeedOnSwitch(BoundsSource.Map, false));
        Expect.IsFalse(BoundsSourceUi.ShouldSeedOnSwitch(BoundsSource.Map, true));
    }

    [Test]
    public static void GameIsSeededOnlyWhenDestinationHasNoExistingBounds()
    {
        Expect.IsTrue(BoundsSourceUi.ShouldSeedOnSwitch(BoundsSource.Game, false));
        Expect.IsFalse(BoundsSourceUi.ShouldSeedOnSwitch(BoundsSource.Game, true));
    }
}

[TestFixture(TestType.MainMenu)]
internal sealed class BoundsSourceUiLabelTests
{
    [Test]
    public static void SelfReturnsNonEmptyString()
    {
        var result = BoundsSourceUi.Label(BoundsSource.Self);
        Expect.IsNotNull(result);
        Expect.AreNotEqual("", result);
    }

    [Test]
    public static void MapReturnsNonEmptyString()
    {
        var result = BoundsSourceUi.Label(BoundsSource.Map);
        Expect.IsNotNull(result);
        Expect.AreNotEqual("", result);
    }

    [Test]
    public static void GameReturnsNonEmptyString()
    {
        var result = BoundsSourceUi.Label(BoundsSource.Game);
        Expect.IsNotNull(result);
        Expect.AreNotEqual("", result);
    }

    [Test]
    public static void UncoveredBoundsSourceThrows() =>
        Expect.Throws<InvalidOperationException>(() => BoundsSourceUi.Label((BoundsSource)99));
}

[TestFixture(TestType.MainMenu)]
internal sealed class FarmingHysteresisDataSeedBoundsTests
{
    // Regression test: switching to a fresh Map/Game tier used to seed it by
    // assigning LowerBound then UpperBound through the clamped property setters. Since a
    // fresh tier still holds its own default bounds at that point, seeding a lower value
    // above the still-default upper bound got silently clamped down instead of applied.
    [Test]
    public static void SeedBoundsAppliesBothValuesEvenWhenSeededLowerExceedsThePriorUpper()
    {
        var data = new FarmingHysteresisData(null!) { boundsSource = BoundsSource.Self };
        var priorUpper = data.UpperBound;
        var seededLower = priorUpper + 1000;
        var seededUpper = priorUpper + 2000;

        data.SeedBounds(seededLower, seededUpper);

        Expect.AreEqual(seededLower, data.LowerBound);
        Expect.AreEqual(seededUpper, data.UpperBound);
    }
}
