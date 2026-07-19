using RimTestRedux;
using static FarmingHysteresis.ColonyManagerRedux.CropRotationEntry;

namespace FarmingHysteresis.ColonyManagerRedux.Tests;

// Covers how ComputeTrackedDefs resolves a crop rotation entry's primary/secondary product(s)
// in isolation from any live PlantDef/mod-extension lookup.
[HotSwappable]
[TestSuite]
internal static class ComputeTrackedDefsTests
{
    [Test]
    public static void PrimaryOnlyReturnsJustThePrimaryDef()
    {
        var primary = new ThingDef();
        var secondary = new ThingDef();

        var result = ComputeTrackedDefs(DualCropTrackingMode.PrimaryOnly, primary, [secondary]);

        Assert.ThatCollection(result).Has.Count(1);
        Assert.ThatCollection(result).Does.Contain(primary);
    }

    [Test]
    public static void PrimaryOnlyWithNoPrimaryReturnsEmpty()
    {
        var result = ComputeTrackedDefs(DualCropTrackingMode.PrimaryOnly, null, []);

        Assert.ThatCollection(result).Is.Empty();
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
        );

        Assert.ThatCollection(result).Has.Count(2);
        Assert.ThatCollection(result).Does.Contain(secondaryOne);
        Assert.ThatCollection(result).Does.Contain(secondaryTwo);
        Assert.ThatCollection(result).Does.Not.Contain(primary);
    }

    [Test]
    public static void SecondaryOnlyWithNoSecondaryReturnsEmpty()
    {
        var primary = new ThingDef();

        var result = ComputeTrackedDefs(DualCropTrackingMode.SecondaryOnly, primary, []);

        Assert.ThatCollection(result).Is.Empty();
    }

    [Test]
    public static void BothReturnsPrimaryAndSecondaryDefsTogether()
    {
        var primary = new ThingDef();
        var secondary = new ThingDef();

        var result = ComputeTrackedDefs(DualCropTrackingMode.Both, primary, [secondary]);

        Assert.ThatCollection(result).Has.Count(2);
        Assert.ThatCollection(result).Does.Contain(primary);
        Assert.ThatCollection(result).Does.Contain(secondary);
    }

    [Test]
    public static void BothWithNoPrimaryReturnsJustSecondary()
    {
        var secondary = new ThingDef();

        var result = ComputeTrackedDefs(DualCropTrackingMode.Both, null, [secondary]);

        Assert.ThatCollection(result).Has.Count(1);
        Assert.ThatCollection(result).Does.Contain(secondary);
    }
}
