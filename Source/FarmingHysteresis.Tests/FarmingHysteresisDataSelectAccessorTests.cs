using DevTools.Testing;

namespace FarmingHysteresis.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class FarmingHysteresisDataSelectAccessorTests
{
    private sealed class FakeAccessor : IBoundedValueAccessor
    {
        public required BoundValues BoundValueRaw { get; init; }

        public BoundValues PeekBoundValue() => BoundValueRaw;

        public void CommitBoundValue(BoundValues value) { }
    }

    private static readonly FakeAccessor SelfAccessor = new()
    {
        BoundValueRaw = new BoundValues { Lower = 1, Upper = 2 },
    };
    private static readonly FakeAccessor MapAccessor = new()
    {
        BoundValueRaw = new BoundValues { Lower = 3, Upper = 4 },
    };
    private static readonly FakeAccessor GameAccessor = new()
    {
        BoundValueRaw = new BoundValues { Lower = 5, Upper = 6 },
    };

    [Test]
    public static void SelfPicksTheAlreadyResolvedSelfAccessorWithoutInvokingFactories()
    {
        // This locks in today's (pre-Game-tier) Self behavior: Self must never touch the
        // map/game factories, since those throw when called with a null grower context.
        var result = FarmingHysteresisData.SelectAccessor(
            BoundsSource.Self,
            SelfAccessor,
            () => throw new InvalidOperationException("map factory should not be invoked"),
            () => throw new InvalidOperationException("game factory should not be invoked")
        );
        Expect.AreEqual(1, result.BoundValueRaw.Lower);
    }

    [Test]
    public static void MapPicksTheMapAccessor()
    {
        var result = FarmingHysteresisData.SelectAccessor(
            BoundsSource.Map,
            SelfAccessor,
            () => MapAccessor,
            () => throw new InvalidOperationException("game factory should not be invoked")
        );
        Expect.AreEqual(3, result.BoundValueRaw.Lower);
    }

    [Test]
    public static void GamePicksTheGameAccessor()
    {
        var result = FarmingHysteresisData.SelectAccessor(
            BoundsSource.Game,
            SelfAccessor,
            () => throw new InvalidOperationException("map factory should not be invoked"),
            () => GameAccessor
        );
        Expect.AreEqual(5, result.BoundValueRaw.Lower);
    }

    [Test]
    public static void UncoveredBoundsSourceThrows() =>
        Expect.Throws<InvalidOperationException>(() =>
            FarmingHysteresisData.SelectAccessor(
                (BoundsSource)99,
                SelfAccessor,
                () => MapAccessor,
                () => GameAccessor
            )
        );
}
