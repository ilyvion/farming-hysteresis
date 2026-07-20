using RimTestRedux;

namespace FarmingHysteresis.Tests;

[HotSwappable]
[TestSuite]
internal static class MainButtonWorkerHysteresisComputeVisibleTests
{
    [Test]
    public static void VisibleOnlyWhenBothFlagsAreTrue()
    {
        Assert
            .That(
                MainButtonWorker_Hysteresis.ComputeVisible(
                    showHysteresisMainTab: true,
                    controllerShowMainTab: true
                )
            )
            .Is.True();
        Assert
            .That(
                MainButtonWorker_Hysteresis.ComputeVisible(
                    showHysteresisMainTab: true,
                    controllerShowMainTab: false
                )
            )
            .Is.False();
        Assert
            .That(
                MainButtonWorker_Hysteresis.ComputeVisible(
                    showHysteresisMainTab: false,
                    controllerShowMainTab: true
                )
            )
            .Is.False();
        Assert
            .That(
                MainButtonWorker_Hysteresis.ComputeVisible(
                    showHysteresisMainTab: false,
                    controllerShowMainTab: false
                )
            )
            .Is.False();
    }
}
