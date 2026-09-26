using DevTools.Testing;
using FarmingHysteresis.Patch;

namespace FarmingHysteresis.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class WorkGiverGrowerHarvestHasJobOnCellTests
{
    // Regression test: a grower whose hysteresis was disabled while its persisted
    // allowHarvest flag happened to be false used to stay permanently blocked from
    // harvesting, because the postfix applied that stale flag regardless of Enabled.
    // FarmingHysteresis.VanillaPlantsExpandedMorePlants's aquatic/sandy postfixes now
    // route through this same ComputeResult, so this suite guards those too.
    [Test]
    public static void DisabledGrowerLeavesOriginalResultUntouched() =>
        Expect.IsTrue(
            WorkGiver_GrowerHarvest_HasJobOnCell.ComputeResult(
                originalResult: true,
                enabled: false,
                allowHarvest: false
            )
        );

    [Test]
    public static void EnabledGrowerAppliesAllowHarvestFlag()
    {
        Expect.IsFalse(
            WorkGiver_GrowerHarvest_HasJobOnCell.ComputeResult(
                originalResult: true,
                enabled: true,
                allowHarvest: false
            )
        );
        Expect.IsTrue(
            WorkGiver_GrowerHarvest_HasJobOnCell.ComputeResult(
                originalResult: true,
                enabled: true,
                allowHarvest: true
            )
        );
    }
}
