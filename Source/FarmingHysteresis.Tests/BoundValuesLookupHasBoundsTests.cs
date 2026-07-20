using RimTestRedux;

namespace FarmingHysteresis.Tests;

[HotSwappable]
[TestSuite]
internal static class BoundValuesLookupHasBoundsTests
{
    [Test]
    public static void NullDictionaryHasNoBounds()
    {
        var hops = new ThingDef();

        Assert.That(BoundValuesLookup.HasBounds(null, hops)).Is.False();
    }

    [Test]
    public static void EmptyDictionaryHasNoBounds()
    {
        var hops = new ThingDef();
        var values = new Dictionary<ThingDef, BoundValues>();

        Assert.That(BoundValuesLookup.HasBounds(values, hops)).Is.False();
    }

    [Test]
    public static void DictionaryContainingTheKeyHasBounds()
    {
        var hops = new ThingDef();
        var values = new Dictionary<ThingDef, BoundValues> { [hops] = new() };

        Assert.That(BoundValuesLookup.HasBounds(values, hops)).Is.True();
    }

    [Test]
    public static void DictionaryContainingOnlyOtherKeysHasNoBounds()
    {
        var hops = new ThingDef();
        var beer = new ThingDef();
        var values = new Dictionary<ThingDef, BoundValues> { [beer] = new() };

        Assert.That(BoundValuesLookup.HasBounds(values, hops)).Is.False();
    }
}
