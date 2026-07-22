using FarmingHysteresis.Extensions;
using RimTestRedux;

namespace FarmingHysteresis.Tests;

// Minimal IPlantToGrowSettable stand-in for PlantHarvestInfo, which only ever calls
// GetPlantDefToGrow() (and Map, only once a non-null harvestedThingDef is found) - the other
// interface members just throw if something starts relying on them.
file sealed class FakeGrower(ThingDef? plantDef) : IPlantToGrowSettable
{
    public ThingDef? GetPlantDefToGrow() => plantDef;

    public void SetPlantDefToGrow(ThingDef plantDef) => throw new NotImplementedException();

    public bool CanAcceptSowNow() => throw new NotImplementedException();

    public IEnumerable<IntVec3> Cells => throw new NotImplementedException();

    public Map Map => throw new NotImplementedException();
}

[HotSwappable]
[TestSuite]
internal static class PlantToGrowSettableExtensionsPlantHarvestInfoTests
{
    // Regression test: a misbehaving mod that sets a non-plant ThingDef as a grower's
    // plant-to-grow used to NRE here, since only the outer GetPlantDefToGrow() null was guarded,
    // not ThingDef.plant itself.
    [Test]
    public static void NonPlantDefToGrowReturnsNullWithoutThrowing()
    {
        var grower = new FakeGrower(new ThingDef { defName = "NotAPlant", plant = null });

        var (harvestedThingDef, count) = grower.PlantHarvestInfo();

        Assert.That(harvestedThingDef == null).Is.True();
        Assert.That(count).Is.EqualTo(0);
    }

    [Test]
    public static void NoPlantChosenReturnsNullWithoutThrowing()
    {
        var grower = new FakeGrower(null);

        var (harvestedThingDef, count) = grower.PlantHarvestInfo();

        Assert.That(harvestedThingDef == null).Is.True();
        Assert.That(count).Is.EqualTo(0);
    }
}
