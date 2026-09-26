using DevTools.Testing;
using FarmingHysteresis.Defs;

namespace FarmingHysteresis.Tests;

// Regression guard for BUG-1: two FarmingHysteresisControlDefs claiming the same controlledClass
// must be surfaced as a ConfigErrors error (the modder-facing signal), since ResolveControlDef
// itself now silently picks one instead of throwing.
[TestFixture(TestType.MainMenu)]
internal sealed class FarmingHysteresisControlDefFindControlledClassCollisionsTests
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

        Expect.IsEmpty(result);
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

        Expect.AreEqual(2, result.Count());
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

        Expect.IsEmpty(result);
    }
}
