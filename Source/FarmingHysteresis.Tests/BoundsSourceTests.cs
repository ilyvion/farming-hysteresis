using RimTestRedux;

namespace FarmingHysteresis.Tests;

[HotSwappable]
[TestSuite]
internal static class BoundsSourceMigrationTests
{
    [Test]
    public static void OldTrueMigratesToMap() =>
        Assert
            .That(BoundsSourceMigration.FromOldUseGlobalValues(true))
            .Is.EqualTo(BoundsSource.Map);

    [Test]
    public static void OldFalseMigratesToSelf() =>
        Assert
            .That(BoundsSourceMigration.FromOldUseGlobalValues(false))
            .Is.EqualTo(BoundsSource.Self);
}

[HotSwappable]
[TestSuite]
internal static class BoundsSourceUiShouldSeedOnSwitchTests
{
    [Test]
    public static void SelfIsNeverSeeded()
    {
        Assert.That(BoundsSourceUi.ShouldSeedOnSwitch(BoundsSource.Self, false)).Is.False();
        Assert.That(BoundsSourceUi.ShouldSeedOnSwitch(BoundsSource.Self, true)).Is.False();
    }

    [Test]
    public static void MapIsSeededOnlyWhenDestinationHasNoExistingBounds()
    {
        Assert.That(BoundsSourceUi.ShouldSeedOnSwitch(BoundsSource.Map, false)).Is.True();
        Assert.That(BoundsSourceUi.ShouldSeedOnSwitch(BoundsSource.Map, true)).Is.False();
    }

    [Test]
    public static void GameIsSeededOnlyWhenDestinationHasNoExistingBounds()
    {
        Assert.That(BoundsSourceUi.ShouldSeedOnSwitch(BoundsSource.Game, false)).Is.True();
        Assert.That(BoundsSourceUi.ShouldSeedOnSwitch(BoundsSource.Game, true)).Is.False();
    }
}

[HotSwappable]
[TestSuite]
internal static class BoundsSourceUiLabelTests
{
    [Test]
    public static void SelfReturnsNonEmptyString()
    {
        var result = BoundsSourceUi.Label(BoundsSource.Self);
        Assert.That(result).Is.Not.Null();
        Assert.That(result).Is.Not.EqualTo("");
    }

    [Test]
    public static void MapReturnsNonEmptyString()
    {
        var result = BoundsSourceUi.Label(BoundsSource.Map);
        Assert.That(result).Is.Not.Null();
        Assert.That(result).Is.Not.EqualTo("");
    }

    [Test]
    public static void GameReturnsNonEmptyString()
    {
        var result = BoundsSourceUi.Label(BoundsSource.Game);
        Assert.That(result).Is.Not.Null();
        Assert.That(result).Is.Not.EqualTo("");
    }

    [Test]
    public static void UncoveredBoundsSourceThrows() =>
        Assert.ThatFunc(() => BoundsSourceUi.Label((BoundsSource)99)).Does.Throw();
}

[HotSwappable]
[TestSuite]
internal static class FarmingHysteresisDataSeedBoundsTests
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

        Assert.That(data.LowerBound).Is.EqualTo(seededLower);
        Assert.That(data.UpperBound).Is.EqualTo(seededUpper);
    }
}
