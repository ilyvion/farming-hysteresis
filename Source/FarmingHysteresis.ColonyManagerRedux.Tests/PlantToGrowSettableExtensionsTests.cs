using FarmingHysteresis.Extensions;
using RimTestRedux;

namespace FarmingHysteresis.ColonyManagerRedux.Tests;

// Regression guard: forceHarvestEnabled must keep harvest allowed regardless of
// ControlHarvesting/state, while sow gating is untouched by it - this prevents a crop the job
// has rotated away from getting stranded unharvested.
[HotSwappable]
[TestSuite]
internal static class PlantToGrowSettableExtensionsComputeAllowTests
{
    [Test]
    public static void SowFollowsStateWheneverSowingIsControlled()
    {
        Assert
            .That(PlantToGrowSettableExtensions.ComputeAllowSow(controlSowing: true, state: true))
            .Is.True();
        Assert
            .That(PlantToGrowSettableExtensions.ComputeAllowSow(controlSowing: true, state: false))
            .Is.False();
    }

    [Test]
    public static void SowIsAlwaysAllowedWhenSowingIsUncontrolled() =>
        Assert
            .That(PlantToGrowSettableExtensions.ComputeAllowSow(controlSowing: false, state: false))
            .Is.True();

    [Test]
    public static void HarvestFollowsStateWheneverHarvestingIsControlledAndNotForced()
    {
        Assert
            .That(
                PlantToGrowSettableExtensions.ComputeAllowHarvest(
                    controlHarvesting: true,
                    state: true,
                    forceHarvestEnabled: false
                )
            )
            .Is.True();
        Assert
            .That(
                PlantToGrowSettableExtensions.ComputeAllowHarvest(
                    controlHarvesting: true,
                    state: false,
                    forceHarvestEnabled: false
                )
            )
            .Is.False();
    }

    [Test]
    public static void HarvestIsAlwaysAllowedWhenHarvestingIsUncontrolled() =>
        Assert
            .That(
                PlantToGrowSettableExtensions.ComputeAllowHarvest(
                    controlHarvesting: false,
                    state: false,
                    forceHarvestEnabled: false
                )
            )
            .Is.True();

    // A crop the job has rotated away from must still get harvested even though the job's own
    // latch state now says "disabled" for it.
    [Test]
    public static void ForceHarvestEnabledOverridesAnOtherwiseDisabledState() =>
        Assert
            .That(
                PlantToGrowSettableExtensions.ComputeAllowHarvest(
                    controlHarvesting: true,
                    state: false,
                    forceHarvestEnabled: true
                )
            )
            .Is.True();
}
