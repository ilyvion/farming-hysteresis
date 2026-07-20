using FarmingHysteresis.Defs;
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

// Regression guard for the GetControlDefForPlantGrower cache added to fix the uncached LINQ
// scan on the pawn job-search hot path: the resolution logic it caches by Type must keep
// picking an exact controlledClass match over a broader IsAssignableFrom fallback, regardless
// of def order, and must keep returning null (not throwing or matching wrongly) when nothing
// controls the given type.
[HotSwappable]
[TestSuite]
internal static class PlantToGrowSettableExtensionsResolveControlDefTests
{
    [Test]
    public static void ExactControlledClassMatchIsPreferredOverAssignableFromFallback()
    {
        var exact = new FarmingHysteresisControlDef
        {
            defName = "Exact",
            controlledClass = typeof(Zone_Growing),
        };
        var fallback = new FarmingHysteresisControlDef
        {
            defName = "Fallback",
            controlledClass = typeof(IPlantToGrowSettable),
        };

        var result = PlantToGrowSettableExtensions.ResolveControlDef(
            [fallback, exact],
            typeof(Zone_Growing)
        );

        Assert.That(result?.defName).Is.EqualTo("Exact");
    }

    [Test]
    public static void FallsBackToAssignableFromWhenNoExactMatchExists()
    {
        var fallback = new FarmingHysteresisControlDef
        {
            defName = "Fallback",
            controlledClass = typeof(IPlantToGrowSettable),
        };

        var result = PlantToGrowSettableExtensions.ResolveControlDef(
            [fallback],
            typeof(Zone_Growing)
        );

        Assert.That(result?.defName).Is.EqualTo("Fallback");
    }

    [Test]
    public static void ReturnsNullWhenNoDefControlsTheType()
    {
        var unrelated = new FarmingHysteresisControlDef
        {
            defName = "Unrelated",
            controlledClass = typeof(Building_PlantGrower),
        };

        var result = PlantToGrowSettableExtensions.ResolveControlDef(
            [unrelated],
            typeof(Zone_Growing)
        );

        Assert.That(result is null).Is.True();
    }
}
