using DevTools.Testing;
using FarmingHysteresis.Defs;

namespace FarmingHysteresis.Tests;

// Regression guard: a subclass def (e.g. controlling Zone_Growing_Fancy) passes
// FindControlledClassCollisions cleanly since it doesn't exactly match a broader def's
// controlledClass (e.g. Zone_Growing), but the broader def's worker still enumerates the
// subclass's instances via inheritance-inclusive APIs (OfType<Zone_Growing>(),
// AllBuildingsColonistOfClass<Building_PlantGrower>()). Without this filter, such a grower would
// be yielded by both defs' workers in AllControlledPlantGrowers.
file class FakeBaseGrower;

file sealed class FakeDerivedGrower : FakeBaseGrower;

file sealed class FakeUnrelatedGrower;

[TestFixture(TestType.MainMenu)]
internal sealed class FarmingHysteresisControlDefIsMostSpecificControllerForTests
{
    [Test]
    public static void BroaderDefIsNotTheControllerForATypeAMoreSpecificDefExactlyMatches()
    {
        var broad = new FarmingHysteresisControlDef
        {
            defName = "Broad",
            controlledClass = typeof(FakeBaseGrower),
        };
        var specific = new FarmingHysteresisControlDef
        {
            defName = "Specific",
            controlledClass = typeof(FakeDerivedGrower),
        };

        var result = FarmingHysteresisControlDef.IsMostSpecificControllerFor(
            [broad, specific],
            typeof(FakeDerivedGrower),
            broad
        );

        Expect.IsFalse(result);
    }

    [Test]
    public static void MoreSpecificDefIsTheControllerForATypeItExactlyMatches()
    {
        var broad = new FarmingHysteresisControlDef
        {
            defName = "Broad",
            controlledClass = typeof(FakeBaseGrower),
        };
        var specific = new FarmingHysteresisControlDef
        {
            defName = "Specific",
            controlledClass = typeof(FakeDerivedGrower),
        };

        var result = FarmingHysteresisControlDef.IsMostSpecificControllerFor(
            [broad, specific],
            typeof(FakeDerivedGrower),
            specific
        );

        Expect.IsTrue(result);
    }

    [Test]
    public static void BroaderDefIsStillTheControllerForATypeOnlyItMatches()
    {
        var broad = new FarmingHysteresisControlDef
        {
            defName = "Broad",
            controlledClass = typeof(FakeBaseGrower),
        };
        var specific = new FarmingHysteresisControlDef
        {
            defName = "Specific",
            controlledClass = typeof(FakeDerivedGrower),
        };

        var result = FarmingHysteresisControlDef.IsMostSpecificControllerFor(
            [broad, specific],
            typeof(FakeBaseGrower),
            broad
        );

        Expect.IsTrue(result);
    }

    [Test]
    public static void NoDefIsTheControllerForAnUnrelatedType()
    {
        var broad = new FarmingHysteresisControlDef
        {
            defName = "Broad",
            controlledClass = typeof(FakeBaseGrower),
        };

        var result = FarmingHysteresisControlDef.IsMostSpecificControllerFor(
            [broad],
            typeof(FakeUnrelatedGrower),
            broad
        );

        Expect.IsFalse(result);
    }
}
