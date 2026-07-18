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
