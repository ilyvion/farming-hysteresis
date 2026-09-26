using DevTools.Testing;

namespace FarmingHysteresis.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class MainButtonWorkerHysteresisComputeVisibleTests
{
    [Test]
    public static void VisibleOnlyWhenBothFlagsAreTrue()
    {
        Expect.IsTrue(
            MainButtonWorker_Hysteresis.ComputeVisible(
                showHysteresisMainTab: true,
                controllerShowMainTab: true
            )
        );
        Expect.IsFalse(
            MainButtonWorker_Hysteresis.ComputeVisible(
                showHysteresisMainTab: true,
                controllerShowMainTab: false
            )
        );
        Expect.IsFalse(
            MainButtonWorker_Hysteresis.ComputeVisible(
                showHysteresisMainTab: false,
                controllerShowMainTab: true
            )
        );
        Expect.IsFalse(
            MainButtonWorker_Hysteresis.ComputeVisible(
                showHysteresisMainTab: false,
                controllerShowMainTab: false
            )
        );
    }
}
