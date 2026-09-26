using DevTools.Testing;

namespace FarmingHysteresis.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class FarmingHysteresisMapComponentResolveLoadedIdTests
{
    [Test]
    public static void UnresolvedIdFallsBackToMapUniqueId()
    {
        // Regression guard: a component added to an existing save loads with id == -1
        // (no "id" node to read); FinalizeInit must fix it up to the map's own id.
        var resolved = FarmingHysteresisMapComponent.ResolveLoadedId(-1, 7);

        Expect.AreEqual(7, resolved);
    }

    [Test]
    public static void AlreadyResolvedIdIsLeftUntouched()
    {
        var resolved = FarmingHysteresisMapComponent.ResolveLoadedId(3, 7);

        Expect.AreEqual(3, resolved);
    }
}
