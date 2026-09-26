using DevTools.Testing;
using FarmingHysteresis.Defs;
using FarmingHysteresis.Extensions;

namespace FarmingHysteresis.ColonyManagerRedux.Tests;

// Regression guard: forceHarvestEnabled must keep harvest allowed regardless of
// ControlHarvesting/state, while sow gating is untouched by it - this prevents a crop the job
// has rotated away from getting stranded unharvested.
[TestFixture(TestType.MainMenu)]
internal sealed class PlantToGrowSettableExtensionsComputeAllowTests
{
    [Test]
    public static void SowFollowsStateWheneverSowingIsControlled()
    {
        Expect.IsTrue(
            PlantToGrowSettableExtensions.ComputeAllowSow(controlSowing: true, state: true)
        );
        Expect.IsFalse(
            PlantToGrowSettableExtensions.ComputeAllowSow(controlSowing: true, state: false)
        );
    }

    [Test]
    public static void SowIsAlwaysAllowedWhenSowingIsUncontrolled() =>
        Expect.IsTrue(
            PlantToGrowSettableExtensions.ComputeAllowSow(controlSowing: false, state: false)
        );

    // A registered soft-dependency veto (e.g. Smart Farming's "No petty jobs") can only ever turn
    // sowing off, never force it on over hysteresis's own decision.
    [Test]
    public static void VetoOverridesAnOtherwiseAllowedSow() =>
        Expect.IsFalse(
            PlantToGrowSettableExtensions.ComputeAllowSow(
                controlSowing: true,
                state: true,
                vetoAllowsSow: false
            )
        );

    [Test]
    public static void VetoCannotForceSowOnWhenHysteresisWouldDisallowIt() =>
        Expect.IsFalse(
            PlantToGrowSettableExtensions.ComputeAllowSow(
                controlSowing: true,
                state: false,
                vetoAllowsSow: true
            )
        );

    [Test]
    public static void NoVetoRegisteredDefaultsToNoVeto() =>
        Expect.IsTrue(
            PlantToGrowSettableExtensions.ComputeAllowSow(controlSowing: true, state: true)
        );

    [Test]
    public static void HarvestFollowsStateWheneverHarvestingIsControlledAndNotForced()
    {
        Expect.IsTrue(
            PlantToGrowSettableExtensions.ComputeAllowHarvest(
                controlHarvesting: true,
                state: true,
                forceHarvestEnabled: false
            )
        );
        Expect.IsFalse(
            PlantToGrowSettableExtensions.ComputeAllowHarvest(
                controlHarvesting: true,
                state: false,
                forceHarvestEnabled: false
            )
        );
    }

    [Test]
    public static void HarvestIsAlwaysAllowedWhenHarvestingIsUncontrolled() =>
        Expect.IsTrue(
            PlantToGrowSettableExtensions.ComputeAllowHarvest(
                controlHarvesting: false,
                state: false,
                forceHarvestEnabled: false
            )
        );

    // A crop the job has rotated away from must still get harvested even though the job's own
    // latch state now says "disabled" for it.
    [Test]
    public static void ForceHarvestEnabledOverridesAnOtherwiseDisabledState() =>
        Expect.IsTrue(
            PlantToGrowSettableExtensions.ComputeAllowHarvest(
                controlHarvesting: true,
                state: false,
                forceHarvestEnabled: true
            )
        );
}

// Regression guard for the GetControlDefForPlantGrower cache added to fix the uncached LINQ
// scan on the pawn job-search hot path: the resolution logic it caches by Type must keep
// picking an exact controlledClass match over a broader IsAssignableFrom fallback, regardless
// of def order, and must keep returning null (not throwing or matching wrongly) when nothing
// controls the given type.
[TestFixture(TestType.MainMenu)]
internal sealed class PlantToGrowSettableExtensionsResolveControlDefTests
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

        Expect.AreEqual("Exact", result?.defName);
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

        Expect.AreEqual("Fallback", result?.defName);
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

        Expect.IsTrue(result is null);
    }

    // Regression guard: two defs colliding on the same controlledClass must not crash
    // the job-search hot path via SingleOrDefault - resolution degrades to picking one of them,
    // while FarmingHysteresisControlDefTests.ConfigErrors is what surfaces the collision to the modder.
    [Test]
    public static void DuplicateExactControlledClassDoesNotThrowAndPicksOne()
    {
        var first = new FarmingHysteresisControlDef
        {
            defName = "First",
            controlledClass = typeof(Zone_Growing),
        };
        var second = new FarmingHysteresisControlDef
        {
            defName = "Second",
            controlledClass = typeof(Zone_Growing),
        };

        var result = PlantToGrowSettableExtensions.ResolveControlDef(
            [first, second],
            typeof(Zone_Growing)
        );

        Expect.AreEqual("First", result?.defName);
    }
}
