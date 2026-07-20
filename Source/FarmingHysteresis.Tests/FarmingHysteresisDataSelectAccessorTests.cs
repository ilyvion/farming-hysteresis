using RimTestRedux;

namespace FarmingHysteresis.Tests;

[HotSwappable]
[TestSuite]
internal static class FarmingHysteresisDataSelectAccessorTests
{
    private sealed class FakeAccessor : IBoundedValueAccessor
    {
        public required BoundValues BoundValueRaw { get; init; }
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
        Assert.That(result.BoundValueRaw.Lower).Is.EqualTo(1);
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
        Assert.That(result.BoundValueRaw.Lower).Is.EqualTo(3);
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
        Assert.That(result.BoundValueRaw.Lower).Is.EqualTo(5);
    }

    [Test]
    [ShouldThrow(typeof(InvalidOperationException))]
    public static void UncoveredBoundsSourceThrows() =>
        FarmingHysteresisData.SelectAccessor(
            (BoundsSource)99,
            SelfAccessor,
            () => MapAccessor,
            () => GameAccessor
        );
}
