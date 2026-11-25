using DevTools.Testing;
using SmartFarming;

namespace FarmingHysteresis.SmartFarming.Tests;

// The reconcile rule itself is covered by FarmingHysteresis.Tests' SmartFarmingCooperationTests;
// these check how Smart Farming's sow modes map onto it.
[TestFixture(TestType.MainMenu)]
internal sealed class SmartFarmingIntegrationTests
{
    // Regression guard: Smart Farming sets allow-sow from its sow mode on every recalculation,
    // which turned sowing back on in a zone hysteresis had stopped until hysteresis next
    // reasserted it.
    [TestCase(ZoneData.SowMode.On)]
    [TestCase(ZoneData.SowMode.Smart)]
    [TestCase(ZoneData.SowMode.Force)]
    public static void SmartFarmingDoesNotReenableSowingHysteresisDisallowed(
        ZoneData.SowMode sowMode
    ) =>
        Expect.IsFalse(
            SmartFarmingIntegration.ReconcileAllowSow(
                sowMode,
                allowSowBefore: false,
                allowSowAfter: true,
                noPettyJobs: false
            )
        );

    [Test]
    public static void PlayerChosenOffIsKept() =>
        Expect.IsFalse(
            SmartFarmingIntegration.ReconcileAllowSow(
                ZoneData.SowMode.Off,
                allowSowBefore: true,
                allowSowAfter: false,
                noPettyJobs: false
            )
        );
}
