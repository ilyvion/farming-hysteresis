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

[TestFixture(TestType.MainMenu)]
internal sealed class SmartFarmingCooperationTests
{
    // AllowSow must never veto a grower Smart Farming doesn't itself manage, and must do so
    // without asking about "No petty jobs", so it is safe to register as
    // FarmingHysteresisMod.AllowSowVeto for every controlled grower.
    [Test]
    public static void NonZoneGrowingGrowerIsNeverVetoed()
    {
        var cooperation = new SmartFarmingCooperation(_ =>
            throw new InvalidOperationException("must not be asked")
        );
        Expect.IsTrue(cooperation.AllowSow(new FakeGrower()));
    }

    [Test]
    public static void CachedDecisionIsIgnoredWithoutNoPettyJobs()
    {
        var cooperation = new SmartFarmingCooperation(_ => false);
        var zone = new Zone_Growing();
        cooperation.CacheAllowSow(zone, false);
        Expect.IsTrue(cooperation.AllowSow(zone));
    }

    [Test]
    public static void NoVetoBeforeAnyDecisionIsCached() =>
        Expect.IsTrue(new SmartFarmingCooperation(_ => true).AllowSow(new Zone_Growing()));

    [TestCase(true)]
    [TestCase(false)]
    public static void CachedDecisionAppliesWithNoPettyJobs(bool allowSow)
    {
        var cooperation = new SmartFarmingCooperation(_ => true);
        var zone = new Zone_Growing();
        cooperation.CacheAllowSow(zone, allowSow);
        Expect.AreEqual(allowSow, cooperation.AllowSow(zone));
    }

    [Test]
    public static void LatestCachedDecisionWins()
    {
        var cooperation = new SmartFarmingCooperation(_ => true);
        var zone = new Zone_Growing();
        cooperation.CacheAllowSow(zone, false);
        cooperation.CacheAllowSow(zone, true);
        Expect.IsTrue(cooperation.AllowSow(zone));
    }

    // Regression guard: Smart Farming sets allow-sow from its sow mode on every recalculation,
    // which turned sowing back on in a zone hysteresis had stopped until hysteresis next
    // reasserted it.
    [Test]
    public static void SmartFarmingDoesNotReenableSowingHysteresisDisallowed() =>
        Expect.IsFalse(
            SmartFarmingCooperation.ReconcileAllowSow(
                sowModeIsOff: false,
                allowSowBefore: false,
                allowSowAfter: true,
                noPettyJobs: false
            )
        );

    [Test]
    public static void HysteresisAllowedSowingStaysAllowed() =>
        Expect.IsTrue(
            SmartFarmingCooperation.ReconcileAllowSow(
                sowModeIsOff: false,
                allowSowBefore: true,
                allowSowAfter: true,
                noPettyJobs: false
            )
        );

    [Test]
    public static void SowModeOffKeepsSmartFarmingResult() =>
        Expect.IsFalse(
            SmartFarmingCooperation.ReconcileAllowSow(
                sowModeIsOff: true,
                allowSowBefore: true,
                allowSowAfter: false,
                noPettyJobs: false
            )
        );

    [Test]
    public static void NoPettyJobsCanStillDisallowSowing() =>
        Expect.IsFalse(
            SmartFarmingCooperation.ReconcileAllowSow(
                sowModeIsOff: false,
                allowSowBefore: true,
                allowSowAfter: false,
                noPettyJobs: true
            )
        );

    [Test]
    public static void NoPettyJobsCannotReenableSowingHysteresisDisallowed() =>
        Expect.IsFalse(
            SmartFarmingCooperation.ReconcileAllowSow(
                sowModeIsOff: false,
                allowSowBefore: false,
                allowSowAfter: true,
                noPettyJobs: true
            )
        );
}
