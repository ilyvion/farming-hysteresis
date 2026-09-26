using DevTools.Testing;
using FarmingHysteresis.Patch;

namespace FarmingHysteresis.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class ZoneGetInspectTabsComputeInspectTabsTests
{
    private static readonly string[] ExtraTabs = ["extra"];

    [Test]
    public static void UncontrolledZoneTypeReturnsValuesUnchanged()
    {
        string[] values = ["existing"];
        Expect.AreEqual(
            1,
            Zone_GetInspectTabs.ComputeInspectTabs(false, values, ExtraTabs).Count()
        );
        Expect.IsTrue(
            Zone_GetInspectTabs.ComputeInspectTabs(false, values, ExtraTabs).Contains("existing")
        );
    }

    [Test]
    public static void UncontrolledZoneTypeWithNullValuesStaysNull() =>
        Expect.IsTrue(Zone_GetInspectTabs.ComputeInspectTabs(false, null, ExtraTabs) == null);

    [Test]
    public static void ControlledZoneTypeWithNullValuesReturnsExtraTabs() =>
        Expect.IsTrue(
            Zone_GetInspectTabs.ComputeInspectTabs(true, null, ExtraTabs).Contains("extra")
        );

    [Test]
    public static void ControlledZoneTypeConcatenatesExistingThenExtraTabs()
    {
        string[] values = ["existing"];
        var result = Zone_GetInspectTabs.ComputeInspectTabs(true, values, ExtraTabs).ToList();

        Expect.AreEqual(2, result.Count);
        Expect.AreEqual("existing", result[0]);
        Expect.AreEqual("extra", result[1]);
    }
}
