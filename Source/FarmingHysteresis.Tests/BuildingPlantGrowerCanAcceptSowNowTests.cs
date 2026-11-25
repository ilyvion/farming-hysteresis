#if v1_7_OR_GREATER
using DevTools.Testing;
using FarmingHysteresis.Extensions;

namespace FarmingHysteresis.Tests;

file sealed class SowRequirementsProbe : WorkGiver_GrowerSow
{
    public bool Check(IPlantToGrowSettable settable) => ExtraRequirements(settable, null);
}

// Regression guard: CoreCLR's JIT can inline the small Building_PlantGrower.CanAcceptSowNow into
// its callers, so this goes through vanilla's caller, WorkGiver_GrowerSow.ExtraRequirements.
[TestFixture(TestType.Playing)]
internal sealed class BuildingPlantGrowerCanAcceptSowNowTests
{
    [Test]
    public static void HysteresisDisallowingSowFailsVanillaSowRequirements()
    {
        var settings = FarmingHysteresisMod.Settings;
        var enabledByDefault = settings.EnabledByDefault;
        var hysteresisMode = settings.HysteresisMode;
        try
        {
            settings.EnabledByDefault = true;
            settings.HysteresisMode = HysteresisMode.Sowing;

            var plantPotDef = DefDatabase<ThingDef>.GetNamed("PlantPot");
            var building = (Building_PlantGrower)
                ThingMaker.MakeThing(plantPotDef, GenStuff.DefaultStuffFor(plantPotDef));
            Expect.IsTrue(building.GetFarmingHysteresisData().Enabled);

            building.SetHysteresisControlState(HysteresisMode.Sowing, state: true);
            Expect.IsTrue(((IPlantToGrowSettable)building).CanAcceptSowNow());

            building.SetHysteresisControlState(HysteresisMode.Sowing, state: false);
            Expect.IsFalse(new SowRequirementsProbe().Check(building));
        }
        finally
        {
            settings.EnabledByDefault = enabledByDefault;
            settings.HysteresisMode = hysteresisMode;
        }
    }
}
#endif
