using DevTools.Testing;
using static FarmingHysteresis.ColonyManagerRedux.CropRotationEntry;

namespace FarmingHysteresis.ColonyManagerRedux.Tests;

// Covers how ComputeTrackedDefs resolves a crop rotation entry's primary/secondary product(s)
// in isolation from any live PlantDef/mod-extension lookup.
[TestFixture(TestType.MainMenu)]
internal sealed class ComputeTrackedDefsTests
{
    [Test]
    public static void PrimaryOnlyReturnsJustThePrimaryDef()
    {
        var primary = new ThingDef();
        var secondary = new ThingDef();

        var result = ComputeTrackedDefs(DualCropTrackingMode.PrimaryOnly, primary, [secondary])
            .ToList();

        Expect.AreEqual(1, result.Count);
        Expect.IsTrue(result.Contains(primary));
    }

    [Test]
    public static void PrimaryOnlyWithNoPrimaryReturnsEmpty()
    {
        var result = ComputeTrackedDefs(DualCropTrackingMode.PrimaryOnly, null, []).ToList();

        Expect.IsEmpty(result);
    }

    [Test]
    public static void SecondaryOnlyReturnsJustTheSecondaryDefs()
    {
        var primary = new ThingDef();
        var secondaryOne = new ThingDef();
        var secondaryTwo = new ThingDef();

        var result = ComputeTrackedDefs(
                DualCropTrackingMode.SecondaryOnly,
                primary,
                [secondaryOne, secondaryTwo]
            )
            .ToList();

        Expect.AreEqual(2, result.Count);
        Expect.IsTrue(result.Contains(secondaryOne));
        Expect.IsTrue(result.Contains(secondaryTwo));
        Expect.IsFalse(result.Contains(primary));
    }

    [Test]
    public static void SecondaryOnlyWithNoSecondaryReturnsEmpty()
    {
        var primary = new ThingDef();

        var result = ComputeTrackedDefs(DualCropTrackingMode.SecondaryOnly, primary, []).ToList();

        Expect.IsEmpty(result);
    }

    [Test]
    public static void BothReturnsPrimaryAndSecondaryDefsTogether()
    {
        var primary = new ThingDef();
        var secondary = new ThingDef();

        var result = ComputeTrackedDefs(DualCropTrackingMode.Both, primary, [secondary]).ToList();

        Expect.AreEqual(2, result.Count);
        Expect.IsTrue(result.Contains(primary));
        Expect.IsTrue(result.Contains(secondary));
    }

    [Test]
    public static void BothWithNoPrimaryReturnsJustSecondary()
    {
        var secondary = new ThingDef();

        var result = ComputeTrackedDefs(DualCropTrackingMode.Both, null, [secondary]).ToList();

        Expect.AreEqual(1, result.Count);
        Expect.IsTrue(result.Contains(secondary));
    }
}
