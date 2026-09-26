using DevTools.Testing;
using static FarmingHysteresis.ColonyManagerRedux.ManagerTab_FarmingHysteresis;

namespace FarmingHysteresis.ColonyManagerRedux.Tests;

// Covers DescribeUntrackedProductHint's null-returning branches - the entry-detached, already-
// tracking-both, and no-resolvable-secondary-product cases - in isolation from any live VEF
// dual-crop extension.
[TestFixture(TestType.MainMenu)]
internal sealed class DescribeUntrackedProductHintTests
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

        Expect.IsNull(DescribeUntrackedProductHint(entry));
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

        Expect.IsNull(DescribeUntrackedProductHint(entry));
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

        Expect.IsNull(DescribeUntrackedProductHint(entry));
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

        Expect.IsNull(DescribeUntrackedProductHint(entry));
    }
}

// Covers AddCropButtonLabelKey's branch selection - a player with no rotation entries yet read
// the "additional crop" wording as implying one already existed, so the empty-rotation case must
// resolve to a distinct key.
[TestFixture(TestType.MainMenu)]
internal sealed class AddCropButtonLabelKeyTests
{
    [Test]
    public static void ReturnsFirstCropKeyWhenNoEntriesExist() =>
        Expect.AreEqual(
            "FarmingHysteresis.CMR.CropRotation.AddFirstCrop",
            AddCropButtonLabelKey(0)
        );

    [Test]
    public static void ReturnsAdditionalCropKeyWhenEntriesExist() =>
        Expect.AreEqual("FarmingHysteresis.CMR.CropRotation.AddCrop", AddCropButtonLabelKey(1));
}
