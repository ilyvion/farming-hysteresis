using RimTestRedux;

namespace FarmingHysteresis.Tests;

[HotSwappable]
[TestSuite]
internal static class MainTabWindowHysteresisShouldRebuildForMapSwitchTests
{
    // Regression test: RebuildBoundsList() used to resolve Find.CurrentMap only from PreOpen
    // and the source-selector float menu, so switching maps with the tab open (e.g. via dev
    // tools or gravship transit) kept showing and editing the previous map's bounds.
    [Test]
    public static void MapSourceWithDifferentCurrentMapRebuilds()
    {
        var boundMap = new object();
        var currentMap = new object();

        Assert
            .That(
                MainTabWindow_Hysteresis.ShouldRebuildForMapSwitch(
                    BoundsSource.Map,
                    boundMap,
                    currentMap
                )
            )
            .Is.True();
    }

    [Test]
    public static void MapSourceWithSameCurrentMapDoesNotRebuild()
    {
        var boundMap = new object();

        Assert
            .That(
                MainTabWindow_Hysteresis.ShouldRebuildForMapSwitch(
                    BoundsSource.Map,
                    boundMap,
                    boundMap
                )
            )
            .Is.False();
    }

    [Test]
    public static void GameSourceNeverRebuildsRegardlessOfMap()
    {
        var boundMap = new object();
        var currentMap = new object();

        Assert
            .That(
                MainTabWindow_Hysteresis.ShouldRebuildForMapSwitch(
                    BoundsSource.Game,
                    boundMap,
                    currentMap
                )
            )
            .Is.False();
        Assert
            .That(MainTabWindow_Hysteresis.ShouldRebuildForMapSwitch(BoundsSource.Game, null, null))
            .Is.False();
    }

    [Test]
    public static void SelfSourceNeverRebuildsRegardlessOfMap()
    {
        var boundMap = new object();
        var currentMap = new object();

        Assert
            .That(
                MainTabWindow_Hysteresis.ShouldRebuildForMapSwitch(
                    BoundsSource.Self,
                    boundMap,
                    currentMap
                )
            )
            .Is.False();
    }
}
