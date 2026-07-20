using FarmingHysteresis.Patch;
using RimTestRedux;

namespace FarmingHysteresis.Tests;

[HotSwappable]
[TestSuite]
internal static class WorkGiverGrowerHarvestHasJobOnCellTests
{
    // Regression test: a grower whose hysteresis was disabled while its persisted
    // allowHarvest flag happened to be false used to stay permanently blocked from
    // harvesting, because the postfix applied that stale flag regardless of Enabled.
    [Test]
    public static void DisabledGrowerLeavesOriginalResultUntouched() =>
        Assert
            .That(
                WorkGiver_GrowerHarvest_HasJobOnCell.ComputeResult(
                    originalResult: true,
                    enabled: false,
                    allowHarvest: false
                )
            )
            .Is.True();

    [Test]
    public static void EnabledGrowerAppliesAllowHarvestFlag()
    {
        Assert
            .That(
                WorkGiver_GrowerHarvest_HasJobOnCell.ComputeResult(
                    originalResult: true,
                    enabled: true,
                    allowHarvest: false
                )
            )
            .Is.False();
        Assert
            .That(
                WorkGiver_GrowerHarvest_HasJobOnCell.ComputeResult(
                    originalResult: true,
                    enabled: true,
                    allowHarvest: true
                )
            )
            .Is.True();
    }
}
