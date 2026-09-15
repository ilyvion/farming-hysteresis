using RimTestRedux;
using static FarmingHysteresis.ColonyManagerRedux.ManagerJob_FarmingHysteresis;

namespace FarmingHysteresis.ColonyManagerRedux.Tests;

// Covers the "limit sowing to computed need" cell-count/cell-selection math in isolation from any
// live job/map - see ManagerJob_FarmingHysteresis.RecomputeSowBudget for the live glue.
[HotSwappable]
[TestSuite]
internal static class ComputeRequiredCellCountTests
{
    [Test]
    public static void NoRemainingNeedRequiresNoCells() =>
        Assert
            .That(
                ComputeRequiredCellCount(remainingNeed: 0, yieldPerCell: 5, safetyMultiplier: 1.2f)
            )
            .Is.EqualTo(0);

    [Test]
    public static void NegativeRemainingNeedRequiresNoCells() =>
        Assert
            .That(
                ComputeRequiredCellCount(
                    remainingNeed: -10,
                    yieldPerCell: 5,
                    safetyMultiplier: 1.2f
                )
            )
            .Is.EqualTo(0);

    [Test]
    public static void RemainingNeedIsDividedByYieldAndRoundedUp() =>
        // 400 / 5 = 80, * 1.0 safety = 80
        Assert
            .That(
                ComputeRequiredCellCount(remainingNeed: 400, yieldPerCell: 5, safetyMultiplier: 1f)
            )
            .Is.EqualTo(80);

    [Test]
    public static void PartialCellIsRoundedUpRatherThanTruncated() =>
        // 401 / 5 = 80.2, rounded up to 81
        Assert
            .That(
                ComputeRequiredCellCount(remainingNeed: 401, yieldPerCell: 5, safetyMultiplier: 1f)
            )
            .Is.EqualTo(81);

    [Test]
    public static void SafetyMultiplierInflatesTheResult() =>
        // 400 / 5 = 80, * 1.2 safety = 96 mathematically, but 1.2f's float imprecision (stored as
        // ~1.2000000477) pushes the product just over 96, so the ceiling correctly rounds up to 97.
        Assert
            .That(
                ComputeRequiredCellCount(
                    remainingNeed: 400,
                    yieldPerCell: 5,
                    safetyMultiplier: 1.2f
                )
            )
            .Is.EqualTo(97);

    [Test]
    public static void ZeroOrNegativeYieldPerCellIsTreatedAsOne() =>
        // A plant with no meaningful yield still costs one cell per unit of remaining need,
        // rather than dividing by zero/going negative.
        Assert
            .That(
                ComputeRequiredCellCount(remainingNeed: 10, yieldPerCell: 0, safetyMultiplier: 1f)
            )
            .Is.EqualTo(10);
}

[HotSwappable]
[TestSuite]
internal static class OrderEntryIdsStartingAtActiveTests
{
    private static void AssertSequence(IReadOnlyList<int> actual, params int[] expected)
    {
        Assert.ThatCollection(actual).Has.Count(expected.Length);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.That(actual[i]).Is.EqualTo(expected[i]);
        }
    }

    [Test]
    public static void StartsAtTheActiveEntryAndWrapsAround() =>
        AssertSequence(OrderEntryIdsStartingAtActive([1, 2, 3, 4], activeEntryId: 3), 3, 4, 1, 2);

    [Test]
    public static void ActiveEntryAlreadyFirstIsUnchanged() =>
        AssertSequence(OrderEntryIdsStartingAtActive([1, 2, 3], activeEntryId: 1), 1, 2, 3);

    [Test]
    public static void NoActiveEntryFallsBackToListOrder() =>
        AssertSequence(OrderEntryIdsStartingAtActive([1, 2, 3], activeEntryId: null), 1, 2, 3);

    [Test]
    public static void UnrecognizedActiveEntryFallsBackToListOrder() =>
        AssertSequence(OrderEntryIdsStartingAtActive([1, 2, 3], activeEntryId: 99), 1, 2, 3);

    [Test]
    public static void EmptyListStaysEmpty() =>
        Assert.ThatCollection(OrderEntryIdsStartingAtActive([], activeEntryId: 1)).Has.Count(0);
}

[HotSwappable]
[TestSuite]
internal static class ComputeCascadingSowPlanTests
{
    [Test]
    public static void AlreadyGrowingCellsAreAlwaysIncludedAndCountAgainstTheRequirement()
    {
        var cropA = new ThingDef { defName = "CropA" };

        (IntVec3 Cell, float Fertility, ThingDef? StandingPlantDef)[] cells =
        [
            (new IntVec3(0, 0, 0), 1f, cropA),
            (new IntVec3(1, 0, 0), 1f, cropA),
            (new IntVec3(2, 0, 0), 1f, null),
        ];

        // 2 required overall, and 2 already growing - nothing new to sow.
        var plan = ComputeCascadingSowPlan(
            cells,
            [(cropA, RemainingNeed: 200, YieldPerCell: 100, Unbound: false)],
            safetyMultiplier: 1f
        );

        Assert.ThatCollection(plan.Keys).Has.Count(2);
        Assert.That(plan[new IntVec3(0, 0, 0)] == cropA).Is.True();
        Assert.That(plan[new IntVec3(1, 0, 0)] == cropA).Is.True();
    }

    [Test]
    public static void PicksHighestFertilityCellsFirstAmongThoseNotAlreadyGrowing()
    {
        var cropA = new ThingDef { defName = "CropA" };

        (IntVec3 Cell, float Fertility, ThingDef? StandingPlantDef)[] cells =
        [
            (new IntVec3(0, 0, 0), 0.4f, null),
            (new IntVec3(1, 0, 0), 0.9f, null),
            (new IntVec3(2, 0, 0), 0.6f, null),
        ];

        var plan = ComputeCascadingSowPlan(
            cells,
            [(cropA, RemainingNeed: 200, YieldPerCell: 100, Unbound: false)],
            safetyMultiplier: 1f
        );

        Assert.ThatCollection(plan.Keys).Has.Count(2);
        Assert.That(plan[new IntVec3(1, 0, 0)] == cropA).Is.True();
        Assert.That(plan[new IntVec3(2, 0, 0)] == cropA).Is.True();
        Assert.ThatCollection(plan.Keys).Does.Not.Contain(new IntVec3(0, 0, 0));
    }

    [Test]
    public static void NoRemainingNeedSowsNothingNew()
    {
        var cropA = new ThingDef { defName = "CropA" };

        (IntVec3 Cell, float Fertility, ThingDef? StandingPlantDef)[] cells =
        [
            (new IntVec3(0, 0, 0), 0.9f, null),
            (new IntVec3(1, 0, 0), 0.9f, cropA),
        ];

        var plan = ComputeCascadingSowPlan(
            cells,
            [(cropA, RemainingNeed: 0, YieldPerCell: 100, Unbound: false)],
            safetyMultiplier: 1f
        );

        Assert.ThatCollection(plan.Keys).Has.Count(1);
        Assert.That(plan[new IntVec3(1, 0, 0)] == cropA).Is.True();
    }

    [Test]
    public static void ExcessCellsBeyondTheActiveEntrysNeedCascadeToTheNextEntry()
    {
        var cropA = new ThingDef { defName = "CropA" };
        var cropB = new ThingDef { defName = "CropB" };

        (IntVec3 Cell, float Fertility, ThingDef? StandingPlantDef)[] cells =
        [
            (new IntVec3(0, 0, 0), 1f, null),
            (new IntVec3(1, 0, 0), 1f, null),
            (new IntVec3(2, 0, 0), 1f, null),
        ];

        // Entry A only needs 1 cell; the other 2 idle cells cascade to entry B rather than
        // staying fallow.
        var plan = ComputeCascadingSowPlan(
            cells,
            [
                (cropA, RemainingNeed: 100, YieldPerCell: 100, Unbound: false),
                (cropB, RemainingNeed: 200, YieldPerCell: 100, Unbound: false),
            ],
            safetyMultiplier: 1f
        );

        Assert.ThatCollection(plan.Keys).Has.Count(3);
        Assert.That(plan.Values.Count(def => def == cropA)).Is.EqualTo(1);
        Assert.That(plan.Values.Count(def => def == cropB)).Is.EqualTo(2);
    }

    [Test]
    public static void EveryEntrysNeedAlreadyCoveredLeavesExcessCellsFallow()
    {
        var cropA = new ThingDef { defName = "CropA" };
        var cropB = new ThingDef { defName = "CropB" };

        (IntVec3 Cell, float Fertility, ThingDef? StandingPlantDef)[] cells =
        [
            (new IntVec3(0, 0, 0), 1f, null),
            (new IntVec3(1, 0, 0), 1f, null),
        ];

        var plan = ComputeCascadingSowPlan(
            cells,
            [
                (cropA, RemainingNeed: 0, YieldPerCell: 100, Unbound: false),
                (cropB, RemainingNeed: 0, YieldPerCell: 100, Unbound: false),
            ],
            safetyMultiplier: 1f
        );

        Assert.ThatCollection(plan.Keys).Has.Count(0);
    }

    [Test]
    public static void UnboundLastEntryConsumesEveryRemainingCell()
    {
        var cropA = new ThingDef { defName = "CropA" };
        var cropB = new ThingDef { defName = "CropB" };

        (IntVec3 Cell, float Fertility, ThingDef? StandingPlantDef)[] cells =
        [
            (new IntVec3(0, 0, 0), 1f, null),
            (new IntVec3(1, 0, 0), 1f, null),
            (new IntVec3(2, 0, 0), 1f, null),
        ];

        var plan = ComputeCascadingSowPlan(
            cells,
            [
                (cropA, RemainingNeed: 100, YieldPerCell: 100, Unbound: false),
                (cropB, RemainingNeed: 0, YieldPerCell: 100, Unbound: true),
            ],
            safetyMultiplier: 1f
        );

        Assert.ThatCollection(plan.Keys).Has.Count(3);
        Assert.That(plan.Values.Count(def => def == cropA)).Is.EqualTo(1);
        Assert.That(plan.Values.Count(def => def == cropB)).Is.EqualTo(2);
    }

    [Test]
    public static void CellOccupiedByAPlantOutsideEveryListedEntryIsNeverReassigned()
    {
        var cropA = new ThingDef { defName = "CropA" };
        var cropB = new ThingDef { defName = "CropB" };
        var wildFiller = new ThingDef { defName = "WildFiller" };

        (IntVec3 Cell, float Fertility, ThingDef? StandingPlantDef)[] cells =
        [
            (new IntVec3(0, 0, 0), 1f, wildFiller),
            (new IntVec3(1, 0, 0), 1f, null),
        ];

        var plan = ComputeCascadingSowPlan(
            cells,
            [
                (cropA, RemainingNeed: 100, YieldPerCell: 100, Unbound: false),
                (cropB, RemainingNeed: 100, YieldPerCell: 100, Unbound: false),
            ],
            safetyMultiplier: 1f
        );

        Assert.ThatCollection(plan.Keys).Does.Not.Contain(new IntVec3(0, 0, 0));
        Assert.ThatCollection(plan.Keys).Has.Count(1);
    }
}

[HotSwappable]
[TestSuite]
internal static class ComputeNeedStatsTests
{
    private static ThingDef MakePlantDef(int harvestYield) =>
        new()
        {
            defName = "CropA",
            plant = new PlantProperties { harvestYield = harvestYield },
        };

    [Test]
    public static void NoPlantDefYetReturnsAllZeroes() =>
        Assert
            .That(
                ComputeNeedStats(
                    plantDef: null,
                    trackedThingCount: 0,
                    upper: 100,
                    safetyMultiplier: 1.2f,
                    unbound: false,
                    cellSowPlan: null
                )
                    is (0, 0, 0)
            )
            .Is.True();

    [Test]
    public static void BoundEntryReportsRequiredCellCount()
    {
        var (yieldPerCell, cellsNeeded, _) = ComputeNeedStats(
            plantDef: MakePlantDef(harvestYield: 5),
            trackedThingCount: 600,
            upper: 1000,
            safetyMultiplier: 1f,
            unbound: false,
            cellSowPlan: null
        );

        Assert.That(yieldPerCell).Is.EqualTo(5);
        // (1000 - 600) / 5 = 80
        Assert.That(cellsNeeded!.Value).Is.EqualTo(80);
    }

    [Test]
    public static void UnboundEntryReportsNoFixedCellCount()
    {
        var (_, cellsNeeded, _) = ComputeNeedStats(
            plantDef: MakePlantDef(harvestYield: 5),
            trackedThingCount: 0,
            upper: 1000,
            safetyMultiplier: 1f,
            unbound: true,
            cellSowPlan: null
        );

        Assert.That(cellsNeeded is null).Is.True();
    }

    [Test]
    public static void CellsPlannedCountsOnlyThisEntrysOwnPlan()
    {
        var cropA = MakePlantDef(harvestYield: 5);
        var cropB = new ThingDef
        {
            defName = "CropB",
            plant = new PlantProperties { harvestYield = 5 },
        };
        Dictionary<IntVec3, ThingDef> cellSowPlan = new()
        {
            [new IntVec3(0, 0, 0)] = cropA,
            [new IntVec3(1, 0, 0)] = cropA,
            [new IntVec3(2, 0, 0)] = cropB,
        };

        var (_, _, cellsPlanned) = ComputeNeedStats(
            plantDef: cropA,
            trackedThingCount: 0,
            upper: 1000,
            safetyMultiplier: 1f,
            unbound: false,
            cellSowPlan: cellSowPlan
        );

        Assert.That(cellsPlanned).Is.EqualTo(2);
    }
}

[HotSwappable]
[TestSuite]
internal static class ComputeIsCellWithinSowBudgetTests
{
    [Test]
    public static void UnrestrictedWhenLimitIsOff() =>
        Assert
            .That(
                ComputeIsCellWithinSowBudget(
                    limitSowingToComputedNeed: false,
                    cellSowPlan: null,
                    cell: new IntVec3(0, 0, 0)
                )
            )
            .Is.True();

    [Test]
    public static void DisallowedEverywhereWhenLimitIsOnButNoPlanHasBeenComputedYet() =>
        // e.g. right after a save load, or before this job's first cycle since the setting was
        // enabled - a missing plan means the manager hasn't decided yet, not "no limit".
        Assert
            .That(
                ComputeIsCellWithinSowBudget(
                    limitSowingToComputedNeed: true,
                    cellSowPlan: null,
                    cell: new IntVec3(0, 0, 0)
                )
            )
            .Is.False();

    [Test]
    public static void AllowedOnlyForCellsThePlanAssigns()
    {
        var cropA = new ThingDef
        {
            defName = "CropA",
            plant = new PlantProperties { harvestYield = 5 },
        };
        Dictionary<IntVec3, ThingDef> plan = new() { [new IntVec3(0, 0, 0)] = cropA };

        Assert
            .That(
                ComputeIsCellWithinSowBudget(
                    limitSowingToComputedNeed: true,
                    cellSowPlan: plan,
                    cell: new IntVec3(0, 0, 0)
                )
            )
            .Is.True();
        Assert
            .That(
                ComputeIsCellWithinSowBudget(
                    limitSowingToComputedNeed: true,
                    cellSowPlan: plan,
                    cell: new IntVec3(1, 0, 0)
                )
            )
            .Is.False();
    }
}
