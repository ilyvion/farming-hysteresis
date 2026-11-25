using DevTools.Testing;
using ReGrowthCore;

namespace FarmingHysteresis.ReGrowth.Tests;

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

[TestFixture(TestType.MainMenu)]
internal sealed class ReGrowthSmartFarmingIntegrationTests
{
    // AllowSow must never veto a grower Smart Farming doesn't itself manage, and must do so
    // without touching Map, so it is safe to register as FarmingHysteresisMod.AllowSowVeto for
    // every controlled grower.
    [Test]
    public static void NonZoneGrowingGrowerIsNeverVetoed() =>
        Expect.IsTrue(ReGrowthSmartFarmingIntegration.AllowSow(new FakeGrower()));

    // Regression guard: ReGrowth's Smart Farming switches an 'On' zone to 'Off' when it finds
    // sowing disallowed, and then refuses to sow it at all, so once hysteresis turned sowing off
    // it could never turn it back on.
    [Test]
    public static void SwitchToOffCausedByHysteresisIsUndone()
    {
        var (sowMode, allowSow) = ReGrowthSmartFarmingIntegration.Reconcile(
            sowModeBefore: ZoneData.SowMode.On,
            allowSowBefore: false,
            sowModeAfter: ZoneData.SowMode.Off,
            allowSowAfter: false,
            noPettyJobs: false
        );
        Expect.AreEqual(ZoneData.SowMode.On, sowMode);
        Expect.IsFalse(allowSow, "hysteresis's disallowed sowing must stay disallowed");
    }

    [TestCase(ZoneData.SowMode.On)]
    [TestCase(ZoneData.SowMode.Smart)]
    [TestCase(ZoneData.SowMode.Force)]
    public static void SmartFarmingDoesNotReenableSowingHysteresisDisallowed(
        ZoneData.SowMode sowMode
    )
    {
        var (resultSowMode, allowSow) = ReGrowthSmartFarmingIntegration.Reconcile(
            sowModeBefore: sowMode,
            allowSowBefore: false,
            sowModeAfter: sowMode,
            allowSowAfter: true,
            noPettyJobs: false
        );
        Expect.AreEqual(sowMode, resultSowMode);
        Expect.IsFalse(allowSow);
    }

    [Test]
    public static void HysteresisAllowedSowingStaysAllowed()
    {
        var (sowMode, allowSow) = ReGrowthSmartFarmingIntegration.Reconcile(
            sowModeBefore: ZoneData.SowMode.On,
            allowSowBefore: true,
            sowModeAfter: ZoneData.SowMode.On,
            allowSowAfter: true,
            noPettyJobs: false
        );
        Expect.AreEqual(ZoneData.SowMode.On, sowMode);
        Expect.IsTrue(allowSow);
    }

    [Test]
    public static void PlayerChosenOffIsKept()
    {
        var (sowMode, allowSow) = ReGrowthSmartFarmingIntegration.Reconcile(
            sowModeBefore: ZoneData.SowMode.Off,
            allowSowBefore: true,
            sowModeAfter: ZoneData.SowMode.Off,
            allowSowAfter: false,
            noPettyJobs: false
        );
        Expect.AreEqual(ZoneData.SowMode.Off, sowMode);
        Expect.IsFalse(allowSow);
    }

    [Test]
    public static void NoPettyJobsCanStillDisallowSowing()
    {
        var (_, allowSow) = ReGrowthSmartFarmingIntegration.Reconcile(
            sowModeBefore: ZoneData.SowMode.On,
            allowSowBefore: true,
            sowModeAfter: ZoneData.SowMode.On,
            allowSowAfter: false,
            noPettyJobs: true
        );
        Expect.IsFalse(allowSow);
    }

    [Test]
    public static void NoPettyJobsCannotReenableSowingHysteresisDisallowed()
    {
        var (_, allowSow) = ReGrowthSmartFarmingIntegration.Reconcile(
            sowModeBefore: ZoneData.SowMode.On,
            allowSowBefore: false,
            sowModeAfter: ZoneData.SowMode.On,
            allowSowAfter: true,
            noPettyJobs: true
        );
        Expect.IsFalse(allowSow);
    }
}
