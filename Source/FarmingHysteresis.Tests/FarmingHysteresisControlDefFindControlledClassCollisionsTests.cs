using FarmingHysteresis.Defs;
using RimTestRedux;

namespace FarmingHysteresis.Tests;

// Regression guard for BUG-1: two FarmingHysteresisControlDefs claiming the same controlledClass
// must be surfaced as a ConfigErrors error (the modder-facing signal), since ResolveControlDef
// itself now silently picks one instead of throwing.
[HotSwappable]
[TestSuite]
internal static class FarmingHysteresisControlDefFindControlledClassCollisionsTests
{
    [Test]
    public static void NoErrorWhenAllControlledClassesAreDistinct()
    {
        var first = new FarmingHysteresisControlDef
        {
            defName = "First",
            controlledClass = typeof(Zone_Growing),
        };
        var second = new FarmingHysteresisControlDef
        {
            defName = "Second",
            controlledClass = typeof(Building_PlantGrower),
        };

        var result = FarmingHysteresisControlDef.FindControlledClassCollisions(
            [first, second],
            first
        );

        Assert.ThatCollection(result).Is.Empty();
    }

    [Test]
    public static void ReportsEachOtherDefClaimingTheSameControlledClass()
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
        var third = new FarmingHysteresisControlDef
        {
            defName = "Third",
            controlledClass = typeof(Zone_Growing),
        };

        var result = FarmingHysteresisControlDef.FindControlledClassCollisions(
            [first, second, third],
            first
        );

        Assert.ThatCollection(result).Has.Count(2);
    }

    [Test]
    public static void DoesNotReportTheDefAgainstItself()
    {
        var only = new FarmingHysteresisControlDef
        {
            defName = "Only",
            controlledClass = typeof(Zone_Growing),
        };

        var result = FarmingHysteresisControlDef.FindControlledClassCollisions([only], only);

        Assert.ThatCollection(result).Is.Empty();
    }
}
