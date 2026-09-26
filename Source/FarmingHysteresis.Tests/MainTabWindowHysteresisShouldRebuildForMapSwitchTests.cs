using DevTools.Testing;

namespace FarmingHysteresis.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class MainTabWindowHysteresisShouldRebuildForMapSwitchTests
{
    // Regression test: RebuildBoundsList() used to resolve Find.CurrentMap only from PreOpen
    // and the source-selector float menu, so switching maps with the tab open (e.g. via dev
    // tools or gravship transit) kept showing and editing the previous map's bounds.
    [Test]
    public static void MapSourceWithDifferentCurrentMapRebuilds()
    {
        var boundMap = new object();
        var currentMap = new object();

        Expect.IsTrue(
            MainTabWindow_Hysteresis.ShouldRebuildForMapSwitch(
                BoundsSource.Map,
                boundMap,
                currentMap
            )
        );
    }

    [Test]
    public static void MapSourceWithSameCurrentMapDoesNotRebuild()
    {
        var boundMap = new object();

        Expect.IsFalse(
            MainTabWindow_Hysteresis.ShouldRebuildForMapSwitch(BoundsSource.Map, boundMap, boundMap)
        );
    }

    [Test]
    public static void GameSourceNeverRebuildsRegardlessOfMap()
    {
        var boundMap = new object();
        var currentMap = new object();

        Expect.IsFalse(
            MainTabWindow_Hysteresis.ShouldRebuildForMapSwitch(
                BoundsSource.Game,
                boundMap,
                currentMap
            )
        );
        Expect.IsFalse(
            MainTabWindow_Hysteresis.ShouldRebuildForMapSwitch(BoundsSource.Game, null, null)
        );
    }

    [Test]
    public static void SelfSourceNeverRebuildsRegardlessOfMap()
    {
        var boundMap = new object();
        var currentMap = new object();

        Expect.IsFalse(
            MainTabWindow_Hysteresis.ShouldRebuildForMapSwitch(
                BoundsSource.Self,
                boundMap,
                currentMap
            )
        );
    }
}
