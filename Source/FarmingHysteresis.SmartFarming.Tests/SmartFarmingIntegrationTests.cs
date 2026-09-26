using DevTools.Testing;

namespace FarmingHysteresis.SmartFarming.Tests;

// Minimal IPlantToGrowSettable stand-in - mirrors FarmingHysteresis.Tests' own FakeGrower - for
// asserting AllowSow's early-out on non-Zone_Growing growers without touching Map.
file sealed class FakeGrower : IPlantToGrowSettable
{
    public ThingDef? GetPlantDefToGrow() => throw new NotImplementedException();

    public void SetPlantDefToGrow(ThingDef plantDef) => throw new NotImplementedException();

    public bool CanAcceptSowNow() => throw new NotImplementedException();

    public IEnumerable<IntVec3> Cells => throw new NotImplementedException();

    public Map Map => throw new NotImplementedException();
}

// Regression guard: AllowSow must never veto a grower Smart Farming doesn't itself manage
// (anything that isn't a Zone_Growing), and must do so without touching Map/growZoneRegistry -
// this is what keeps SmartFarmingIntegration safe to register as
// FarmingHysteresisMod.AllowSowVeto for every controlled grower, not just Smart Farming's own
// growing zones.
[TestFixture(TestType.MainMenu)]
internal sealed class SmartFarmingIntegrationTests
{
    [Test]
    public static void NonZoneGrowingGrowerIsNeverVetoed() =>
        Expect.IsTrue(SmartFarmingIntegration.AllowSow(new FakeGrower()));
}
