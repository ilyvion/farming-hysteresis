using DevTools.Testing;
using FarmingHysteresis.Extensions;

namespace FarmingHysteresis.Tests;

// Minimal IPlantToGrowSettable stand-in for PlantHarvestDef, which only ever calls
// GetPlantDefToGrow() - Map must never be touched, since that's the whole point of this helper
// existing separately from PlantHarvestInfo.
file sealed class FakeGrower(ThingDef? plantDef) : IPlantToGrowSettable
{
    public ThingDef? GetPlantDefToGrow() => plantDef;

    public void SetPlantDefToGrow(ThingDef plantDef) => throw new NotImplementedException();

    public bool CanAcceptSowNow() => throw new NotImplementedException();

    public IEnumerable<IntVec3> Cells => throw new NotImplementedException();

    public Map Map => throw new NotImplementedException();
}

[TestFixture(TestType.MainMenu)]
internal sealed class PlantToGrowSettableExtensionsPlantHarvestDefTests
{
    // Regression test for BUG-10: ITab_Hysteresis.RefreshFields used to call PlantHarvestInfo
    // (which computes the map-wide harvested count) only to discard the count. PlantHarvestDef
    // must resolve the def without ever touching Map, which would throw here if it did.
    [Test]
    public static void DoesNotTouchMap()
    {
        var harvestedThingDef = new ThingDef { defName = "Rice" };
        var grower = new FakeGrower(
            new ThingDef
            {
                defName = "RicePlant",
                plant = new PlantProperties { harvestedThingDef = harvestedThingDef },
            }
        );

        var result = grower.PlantHarvestDef();

        Expect.IsTrue(result == harvestedThingDef);
    }

    [Test]
    public static void NonPlantDefToGrowReturnsNullWithoutThrowing()
    {
        var grower = new FakeGrower(new ThingDef { defName = "NotAPlant", plant = null });

        var result = grower.PlantHarvestDef();

        Expect.IsTrue(result == null);
    }

    [Test]
    public static void NoPlantChosenReturnsNullWithoutThrowing()
    {
        var grower = new FakeGrower(null);

        var result = grower.PlantHarvestDef();

        Expect.IsTrue(result == null);
    }
}
