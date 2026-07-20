using FarmingHysteresis.VanillaPlantsExpandedMorePlants.Defs;
using RimTestRedux;

namespace FarmingHysteresis.VanillaPlantsExpandedMorePlants.Tests;

// Covers the null guards on GetAllowSow/SetAllowSow, added to match the ArgumentNullException
// behavior of the core FarmingHysteresisControlWorker_Zone_Growing worker (previously these threw
// an uninformative NullReferenceException instead).
[HotSwappable]
[TestSuite]
internal static class FarmingHysteresisControlWorkersTests
{
    private static readonly FarmingHysteresisControlWorker_Zone_GrowingAquatic AquaticWorker =
        new();
    private static readonly FarmingHysteresisControlWorker_Zone_GrowingSandy SandyWorker = new();

    [Test]
    [ShouldThrow(typeof(ArgumentNullException))]
    public static void AquaticGetAllowSowThrowsArgumentNullExceptionOnNullPlantGrower() =>
        AquaticWorker.GetAllowSow(null!);

    [Test]
    [ShouldThrow(typeof(ArgumentNullException))]
    public static void AquaticSetAllowSowThrowsArgumentNullExceptionOnNullPlantGrower() =>
        AquaticWorker.SetAllowSow(null!, true);

    [Test]
    [ShouldThrow(typeof(ArgumentNullException))]
    public static void SandyGetAllowSowThrowsArgumentNullExceptionOnNullPlantGrower() =>
        SandyWorker.GetAllowSow(null!);

    [Test]
    [ShouldThrow(typeof(ArgumentNullException))]
    public static void SandySetAllowSowThrowsArgumentNullExceptionOnNullPlantGrower() =>
        SandyWorker.SetAllowSow(null!, true);
}
