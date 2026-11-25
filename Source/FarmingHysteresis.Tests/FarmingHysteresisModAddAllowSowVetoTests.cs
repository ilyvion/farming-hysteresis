using DevTools.Testing;

namespace FarmingHysteresis.Tests;

file sealed class FakeGrower : IPlantToGrowSettable
{
    public ThingDef? GetPlantDefToGrow() => throw new NotImplementedException();

    public void SetPlantDefToGrow(ThingDef plantDef) => throw new NotImplementedException();

    public bool CanAcceptSowNow() => throw new NotImplementedException();

    public IEnumerable<IntVec3> Cells => throw new NotImplementedException();

    public Map Map => throw new NotImplementedException();
}

// Regression guard: an integration registering its veto must not replace another integration's
// (Smart Farming and ReGrowth 2's bundled copy can both be active).
[TestFixture(TestType.MainMenu)]
internal sealed class FarmingHysteresisModAddAllowSowVetoTests
{
    private Func<IPlantToGrowSettable, bool>? savedVeto;

    [SetUp]
    public void SaveVeto()
    {
        savedVeto = FarmingHysteresisMod.AllowSowVeto;
        FarmingHysteresisMod.AllowSowVeto = null;
    }

    [TearDown]
    public void RestoreVeto() => FarmingHysteresisMod.AllowSowVeto = savedVeto;

    [Test]
    public static void SingleVetoIsApplied()
    {
        FarmingHysteresisMod.AddAllowSowVeto(_ => false);
        Expect.IsFalse(FarmingHysteresisMod.AllowSowVeto!(new FakeGrower()));
    }

    [Test]
    public static void EarlierVetoStillAppliesAfterAnotherIsAdded()
    {
        FarmingHysteresisMod.AddAllowSowVeto(_ => false);
        FarmingHysteresisMod.AddAllowSowVeto(_ => true);
        Expect.IsFalse(FarmingHysteresisMod.AllowSowVeto!(new FakeGrower()));
    }

    [Test]
    public static void LaterVetoAppliesAfterAnotherWasAdded()
    {
        FarmingHysteresisMod.AddAllowSowVeto(_ => true);
        FarmingHysteresisMod.AddAllowSowVeto(_ => false);
        Expect.IsFalse(FarmingHysteresisMod.AllowSowVeto!(new FakeGrower()));
    }

    [Test]
    public static void SowingAllowedWhenEveryVetoAllowsIt()
    {
        FarmingHysteresisMod.AddAllowSowVeto(_ => true);
        FarmingHysteresisMod.AddAllowSowVeto(_ => true);
        Expect.IsTrue(FarmingHysteresisMod.AllowSowVeto!(new FakeGrower()));
    }
}
