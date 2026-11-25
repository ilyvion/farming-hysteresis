using DevTools.Testing;
using ReGrowthCore;

namespace FarmingHysteresis.ReGrowth.Tests;

// The allow-sow rule itself is covered by FarmingHysteresis.Tests' SmartFarmingCooperationTests;
// these check ReGrowth's sow-mode handling and how its sow modes map onto that rule.
[TestFixture(TestType.MainMenu)]
internal sealed class ReGrowthSmartFarmingIntegrationTests
{
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
}
