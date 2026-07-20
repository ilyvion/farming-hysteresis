using RimTestRedux;
using static FarmingHysteresis.ColonyManagerRedux.ManagerTab_FarmingHysteresis;

namespace FarmingHysteresis.ColonyManagerRedux.Tests;

// Covers DescribeUntrackedProductHint's null-returning branches - the entry-detached, already-
// tracking-both, and no-resolvable-secondary-product cases - in isolation from any live VEF
// dual-crop extension.
[HotSwappable]
[TestSuite]
internal static class DescribeUntrackedProductHintTests
{
    private static ThingDef PlantWithHarvestedProduct() =>
        new()
        {
            defName = "plant",
            plant = new PlantProperties { harvestedThingDef = new ThingDef { label = "product" } },
        };

    [Test]
    public static void DetachedFilterReturnsNullRegardlessOfMode()
    {
        var entry = new CropRotationEntry
        {
            TrackedFilterFollowsTargetPlant = false,
            PlantDef = PlantWithHarvestedProduct(),
            Mode = DualCropTrackingMode.PrimaryOnly,
        };

        Assert.That(DescribeUntrackedProductHint(entry)).Is.Null();
    }

    [Test]
    public static void ModeBothReturnsNull()
    {
        var entry = new CropRotationEntry
        {
            TrackedFilterFollowsTargetPlant = true,
            PlantDef = PlantWithHarvestedProduct(),
            Mode = DualCropTrackingMode.Both,
        };

        Assert.That(DescribeUntrackedProductHint(entry)).Is.Null();
    }

    [Test]
    public static void NoResolvableSecondaryProductReturnsNullForPrimaryOnly()
    {
        var entry = new CropRotationEntry
        {
            TrackedFilterFollowsTargetPlant = true,
            PlantDef = PlantWithHarvestedProduct(),
            Mode = DualCropTrackingMode.PrimaryOnly,
        };

        Assert.That(DescribeUntrackedProductHint(entry)).Is.Null();
    }

    [Test]
    public static void NoResolvableSecondaryProductReturnsNullForSecondaryOnly()
    {
        var entry = new CropRotationEntry
        {
            TrackedFilterFollowsTargetPlant = true,
            PlantDef = PlantWithHarvestedProduct(),
            Mode = DualCropTrackingMode.SecondaryOnly,
        };

        Assert.That(DescribeUntrackedProductHint(entry)).Is.Null();
    }
}
