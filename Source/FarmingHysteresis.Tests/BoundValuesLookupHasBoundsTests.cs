using DevTools.Testing;

namespace FarmingHysteresis.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class BoundValuesLookupHasBoundsTests
{
    [Test]
    public static void NullDictionaryHasNoBounds()
    {
        var hops = new ThingDef();

        Expect.IsFalse(BoundValuesLookup.HasBounds(null, hops));
    }

    [Test]
    public static void EmptyDictionaryHasNoBounds()
    {
        var hops = new ThingDef();
        var values = new Dictionary<ThingDef, BoundValues>();

        Expect.IsFalse(BoundValuesLookup.HasBounds(values, hops));
    }

    [Test]
    public static void DictionaryContainingTheKeyHasBounds()
    {
        var hops = new ThingDef();
        var values = new Dictionary<ThingDef, BoundValues> { [hops] = new() };

        Expect.IsTrue(BoundValuesLookup.HasBounds(values, hops));
    }

    [Test]
    public static void DictionaryContainingOnlyOtherKeysHasNoBounds()
    {
        var hops = new ThingDef();
        var beer = new ThingDef();
        var values = new Dictionary<ThingDef, BoundValues> { [beer] = new() };

        Expect.IsFalse(BoundValuesLookup.HasBounds(values, hops));
    }

    [Test]
    public static void PeekOnNullDictionaryReturnsDetachedDefault()
    {
        var hops = new ThingDef();

        var peeked = BoundValuesLookup.Peek(null, hops, 10, 20);

        Expect.AreEqual(10, peeked.Lower);
        Expect.AreEqual(20, peeked.Upper);
    }

    [Test]
    public static void PeekOnMissingKeyDoesNotAddToDictionary()
    {
        var hops = new ThingDef();
        var values = new Dictionary<ThingDef, BoundValues>();

        _ = BoundValuesLookup.Peek(values, hops, 10, 20);

        Expect.IsFalse(BoundValuesLookup.HasBounds(values, hops));
    }

    [Test]
    public static void PeekOnExistingKeyReturnsExistingEntry()
    {
        var hops = new ThingDef();
        var existing = new BoundValues { Lower = 5, Upper = 15 };
        var values = new Dictionary<ThingDef, BoundValues> { [hops] = existing };

        var peeked = BoundValuesLookup.Peek(values, hops, 10, 20);

        Expect.IsTrue(ReferenceEquals(peeked, existing));
    }

    [Test]
    public static void CommitAddsDetachedValueOnFirstCall()
    {
        var hops = new ThingDef();
        var values = new Dictionary<ThingDef, BoundValues>();
        var peeked = BoundValuesLookup.Peek(values, hops, 10, 20);

        BoundValuesLookup.Commit(values, hops, peeked);

        Expect.IsTrue(BoundValuesLookup.HasBounds(values, hops));
        Expect.IsTrue(ReferenceEquals(values[hops], peeked));
    }

    [Test]
    public static void CommitOnAlreadyPresentKeyDoesNotOverwrite()
    {
        var hops = new ThingDef();
        var existing = new BoundValues { Lower = 5, Upper = 15 };
        var values = new Dictionary<ThingDef, BoundValues> { [hops] = existing };
        var other = new BoundValues { Lower = 99, Upper = 100 };

        BoundValuesLookup.Commit(values, hops, other);

        Expect.IsTrue(ReferenceEquals(values[hops], existing));
    }
}
