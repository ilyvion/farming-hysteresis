using FarmingHysteresis.VanillaExpandedFramework.Defs;
using RimTestRedux;
#if v1_6
using VEF.Plants;
#else
using VanillaPlantsExpanded;
#endif

namespace FarmingHysteresis.VanillaExpandedFramework.Tests;

// Covers DualCropSecondaryProductResolverWorker.ResolveSecondaryProducts' handling of the three
// DualCropExtension shapes it can find on a plant, plus the no-extension-at-all case.
[HotSwappable]
[TestSuite]
internal static class DualCropSecondaryProductResolverWorkerTests
{
    private static readonly DualCropSecondaryProductResolverWorker Worker = new();

    private static ThingDef PlantWithExtension(DualCropExtension? extension)
    {
        var plantDef = new ThingDef { defName = "plant" };
        if (extension != null)
        {
            plantDef.modExtensions = [extension];
        }
        return plantDef;
    }

    [Test]
    public static void NoExtensionYieldsNothing()
    {
        var plantDef = PlantWithExtension(null);

        var result = Worker.ResolveSecondaryProducts(plantDef).ToList();

        Assert.ThatCollection(result).Is.Empty();
    }

    [Test]
    public static void SecondaryOutputOnlyYieldsThatOneDef()
    {
        var secondary = new ThingDef { defName = "secondary" };
        var plantDef = PlantWithExtension(new DualCropExtension { secondaryOutput = secondary });

        var result = Worker.ResolveSecondaryProducts(plantDef).ToList();

        Assert.ThatCollection(result).Has.Count(1);
        Assert.ThatCollection(result).Does.Contain(secondary);
    }

    [Test]
    public static void RandomSecondaryOutputOnlyYieldsEveryListedCandidate()
    {
        var randomOne = new ThingDef { defName = "randomOne" };
        var randomTwo = new ThingDef { defName = "randomTwo" };
        var plantDef = PlantWithExtension(
            new DualCropExtension { randomSecondaryOutput = [randomOne, randomTwo] }
        );

        var result = Worker.ResolveSecondaryProducts(plantDef).ToList();

        Assert.ThatCollection(result).Has.Count(2);
        Assert.ThatCollection(result).Does.Contain(randomOne);
        Assert.ThatCollection(result).Does.Contain(randomTwo);
    }

    [Test]
    public static void BothSetYieldsSecondaryOutputFirstThenEveryRandomCandidate()
    {
        var secondary = new ThingDef { defName = "secondary" };
        var randomOne = new ThingDef { defName = "randomOne" };
        var randomTwo = new ThingDef { defName = "randomTwo" };
        var plantDef = PlantWithExtension(
            new DualCropExtension
            {
                secondaryOutput = secondary,
                randomSecondaryOutput = [randomOne, randomTwo],
            }
        );

        var result = Worker.ResolveSecondaryProducts(plantDef).ToList();

        Assert.ThatCollection(result).Has.Count(3);
        Assert.That(ReferenceEquals(result[0], secondary)).Is.True();
        Assert.That(ReferenceEquals(result[1], randomOne)).Is.True();
        Assert.That(ReferenceEquals(result[2], randomTwo)).Is.True();
    }
}
