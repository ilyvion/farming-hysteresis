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

    [Test]
    public static void PeekOnNullDictionaryReturnsDetachedDefault()
    {
        var hops = new ThingDef();

        var peeked = BoundValuesLookup.Peek(null, hops, 10, 20);

        Assert.That(peeked.Lower).Is.EqualTo(10);
        Assert.That(peeked.Upper).Is.EqualTo(20);
    }

    [Test]
    public static void PeekOnMissingKeyDoesNotAddToDictionary()
    {
        var hops = new ThingDef();
        var values = new Dictionary<ThingDef, BoundValues>();

        _ = BoundValuesLookup.Peek(values, hops, 10, 20);

        Assert.That(BoundValuesLookup.HasBounds(values, hops)).Is.False();
    }

    [Test]
    public static void PeekOnExistingKeyReturnsExistingEntry()
    {
        var hops = new ThingDef();
        var existing = new BoundValues { Lower = 5, Upper = 15 };
        var values = new Dictionary<ThingDef, BoundValues> { [hops] = existing };

        var peeked = BoundValuesLookup.Peek(values, hops, 10, 20);

        Assert.That(ReferenceEquals(peeked, existing)).Is.True();
    }

    [Test]
    public static void CommitAddsDetachedValueOnFirstCall()
    {
        var hops = new ThingDef();
        var values = new Dictionary<ThingDef, BoundValues>();
        var peeked = BoundValuesLookup.Peek(values, hops, 10, 20);

        BoundValuesLookup.Commit(values, hops, peeked);

        Assert.That(BoundValuesLookup.HasBounds(values, hops)).Is.True();
        Assert.That(ReferenceEquals(values[hops], peeked)).Is.True();
    }

    [Test]
    public static void CommitOnAlreadyPresentKeyDoesNotOverwrite()
    {
        var hops = new ThingDef();
        var existing = new BoundValues { Lower = 5, Upper = 15 };
        var values = new Dictionary<ThingDef, BoundValues> { [hops] = existing };
        var other = new BoundValues { Lower = 99, Upper = 100 };

        BoundValuesLookup.Commit(values, hops, other);

        Assert.That(ReferenceEquals(values[hops], existing)).Is.True();
    }
}
