using RimTestRedux;

namespace FarmingHysteresis.Tests;

[HotSwappable]
[TestSuite]
internal static class FarmingHysteresisMapComponentResolveLoadedIdTests
{
    [Test]
    public static void UnresolvedIdFallsBackToMapUniqueId()
    {
        // Regression guard: a component added to an existing save loads with id == -1
        // (no "id" node to read); FinalizeInit must fix it up to the map's own id.
        var resolved = FarmingHysteresisMapComponent.ResolveLoadedId(-1, 7);

        Assert.That(resolved).Is.EqualTo(7);
    }

    [Test]
    public static void AlreadyResolvedIdIsLeftUntouched()
    {
        var resolved = FarmingHysteresisMapComponent.ResolveLoadedId(3, 7);

        Assert.That(resolved).Is.EqualTo(3);
    }
}
