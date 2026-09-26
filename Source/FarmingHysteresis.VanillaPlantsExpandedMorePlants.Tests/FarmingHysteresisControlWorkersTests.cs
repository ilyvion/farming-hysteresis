using DevTools.Testing;
using FarmingHysteresis.VanillaPlantsExpandedMorePlants.Defs;

namespace FarmingHysteresis.VanillaPlantsExpandedMorePlants.Tests;

// Covers the null guards on GetAllowSow/SetAllowSow, added to match the ArgumentNullException
// behavior of the core FarmingHysteresisControlWorker_Zone_Growing worker (previously these threw
// an uninformative NullReferenceException instead).
[TestFixture(TestType.MainMenu)]
internal sealed class FarmingHysteresisControlWorkersTests
{
    private static readonly FarmingHysteresisControlWorker_Zone_GrowingAquatic AquaticWorker =
        new();
    private static readonly FarmingHysteresisControlWorker_Zone_GrowingSandy SandyWorker = new();

    [Test]
    public static void AquaticGetAllowSowThrowsArgumentNullExceptionOnNullPlantGrower() =>
        Expect.Throws<ArgumentNullException>(() => AquaticWorker.GetAllowSow(null!));

    [Test]
    public static void AquaticSetAllowSowThrowsArgumentNullExceptionOnNullPlantGrower() =>
        Expect.Throws<ArgumentNullException>(() => AquaticWorker.SetAllowSow(null!, true));

    [Test]
    public static void SandyGetAllowSowThrowsArgumentNullExceptionOnNullPlantGrower() =>
        Expect.Throws<ArgumentNullException>(() => SandyWorker.GetAllowSow(null!));

    [Test]
    public static void SandySetAllowSowThrowsArgumentNullExceptionOnNullPlantGrower() =>
        Expect.Throws<ArgumentNullException>(() => SandyWorker.SetAllowSow(null!, true));
}
