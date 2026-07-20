using FarmingHysteresis.Patch;
using RimTestRedux;

namespace FarmingHysteresis.Tests;

[HotSwappable]
[TestSuite]
internal static class ZoneGetInspectTabsComputeInspectTabsTests
{
    private static readonly string[] ExtraTabs = ["extra"];

    [Test]
    public static void UncontrolledZoneTypeReturnsValuesUnchanged()
    {
        string[] values = ["existing"];
        Assert
            .ThatCollection(Zone_GetInspectTabs.ComputeInspectTabs(false, values, ExtraTabs))
            .Has.Count(1);
        Assert
            .ThatCollection(Zone_GetInspectTabs.ComputeInspectTabs(false, values, ExtraTabs))
            .Does.Contain("existing");
    }

    [Test]
    public static void UncontrolledZoneTypeWithNullValuesStaysNull() =>
        Assert
            .That(Zone_GetInspectTabs.ComputeInspectTabs(false, null, ExtraTabs) == null)
            .Is.True();

    [Test]
    public static void ControlledZoneTypeWithNullValuesReturnsExtraTabs() =>
        Assert
            .ThatCollection(Zone_GetInspectTabs.ComputeInspectTabs(true, null, ExtraTabs))
            .Does.Contain("extra");

    [Test]
    public static void ControlledZoneTypeConcatenatesExistingThenExtraTabs()
    {
        string[] values = ["existing"];
        var result = Zone_GetInspectTabs.ComputeInspectTabs(true, values, ExtraTabs).ToList();

        Assert.ThatCollection(result).Has.Count(2);
        Assert.That(result[0]).Is.EqualTo("existing");
        Assert.That(result[1]).Is.EqualTo("extra");
    }
}
