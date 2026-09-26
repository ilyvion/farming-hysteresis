using DevTools.Testing;
using static FarmingHysteresis.ColonyManagerRedux.ManagerJob_FarmingHysteresis;

namespace FarmingHysteresis.ColonyManagerRedux.Tests;

// Covers the "limit sowing to computed need" cell-count/cell-selection math in isolation from any
// live job/map - see ManagerJob_FarmingHysteresis.RecomputeSowBudget for the live glue.
[TestFixture(TestType.MainMenu)]
internal sealed class ComputeRequiredCellCountTests
{
    [Test]
    public static void NoRemainingNeedRequiresNoCells() =>
        Expect.AreEqual(
            0,
            ComputeRequiredCellCount(remainingNeed: 0, yieldPerCell: 5, safetyMultiplier: 1.2f)
        );

    [Test]
    public static void NegativeRemainingNeedRequiresNoCells() =>
        Expect.AreEqual(
            0,
            ComputeRequiredCellCount(remainingNeed: -10, yieldPerCell: 5, safetyMultiplier: 1.2f)
        );

    [Test]
    public static void RemainingNeedIsDividedByYieldAndRoundedUp() =>
        // 400 / 5 = 80, * 1.0 safety = 80
        Expect.AreEqual(
            80,
            ComputeRequiredCellCount(remainingNeed: 400, yieldPerCell: 5, safetyMultiplier: 1f)
        );

    [Test]
    public static void PartialCellIsRoundedUpRatherThanTruncated() =>
        // 401 / 5 = 80.2, rounded up to 81
        Expect.AreEqual(
            81,
            ComputeRequiredCellCount(remainingNeed: 401, yieldPerCell: 5, safetyMultiplier: 1f)
        );

    [Test]
    public static void SafetyMultiplierInflatesTheResult() =>
        // 400 / 5 = 80, * 1.2 safety = 96 mathematically, but 1.2f's float imprecision (stored as
        // ~1.2000000477) pushes the product just over 96, so the ceiling correctly rounds up to 97.
        Expect.AreEqual(
            97,
            ComputeRequiredCellCount(remainingNeed: 400, yieldPerCell: 5, safetyMultiplier: 1.2f)
        );

    [Test]
    public static void ZeroOrNegativeYieldPerCellIsTreatedAsOne() =>
        // A plant with no meaningful yield still costs one cell per unit of remaining need,
        // rather than dividing by zero/going negative.
        Expect.AreEqual(
            10,
            ComputeRequiredCellCount(remainingNeed: 10, yieldPerCell: 0, safetyMultiplier: 1f)
        );
}

[TestFixture(TestType.MainMenu)]
internal sealed class OrderEntryIdsStartingAtActiveTests
{
    private static void AssertSequence(IReadOnlyList<int> actual, params int[] expected)
    {
        Expect.AreEqual(expected.Length, actual.Count);
        for (var i = 0; i < expected.Length; i++)
        {
            Expect.AreEqual(expected[i], actual[i]);
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
        Expect.AreEqual(0, OrderEntryIdsStartingAtActive([], activeEntryId: 1).Count);
}

[TestFixture(TestType.MainMenu)]
internal sealed class ComputeCascadingSowPlanTests
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

        Expect.AreEqual(2, plan.Count);
        Expect.IsTrue(plan[new IntVec3(0, 0, 0)] == cropA);
        Expect.IsTrue(plan[new IntVec3(1, 0, 0)] == cropA);
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

        Expect.AreEqual(2, plan.Count);
        Expect.IsTrue(plan[new IntVec3(1, 0, 0)] == cropA);
        Expect.IsTrue(plan[new IntVec3(2, 0, 0)] == cropA);
        Expect.IsFalse(plan.ContainsKey(new IntVec3(0, 0, 0)));
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

        Expect.AreEqual(1, plan.Count);
        Expect.IsTrue(plan[new IntVec3(1, 0, 0)] == cropA);
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

        Expect.AreEqual(3, plan.Count);
        Expect.AreEqual(1, plan.Values.Count(def => def == cropA));
        Expect.AreEqual(2, plan.Values.Count(def => def == cropB));
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

        Expect.AreEqual(0, plan.Count);
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

        Expect.AreEqual(3, plan.Count);
        Expect.AreEqual(1, plan.Values.Count(def => def == cropA));
        Expect.AreEqual(2, plan.Values.Count(def => def == cropB));
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

        Expect.IsFalse(plan.ContainsKey(new IntVec3(0, 0, 0)));
        Expect.AreEqual(1, plan.Count);
    }
}

[TestFixture(TestType.MainMenu)]
internal sealed class ComputeNeedStatsTests
{
    private static ThingDef MakePlantDef(int harvestYield) =>
        new()
        {
            defName = "CropA",
            plant = new PlantProperties { harvestYield = harvestYield },
        };

    [Test]
    public static void NoPlantDefYetReturnsAllZeroes() =>
        Expect.IsTrue(
            ComputeNeedStats(
                plantDef: null,
                trackedThingCount: 0,
                upper: 100,
                safetyMultiplier: 1.2f,
                unbound: false,
                cellSowPlan: null
            )
                is (0, 0, 0)
        );

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

        Expect.AreEqual(5, yieldPerCell);
        // (1000 - 600) / 5 = 80
        Expect.AreEqual(80, cellsNeeded!.Value);
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

        Expect.IsTrue(cellsNeeded is null);
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

        Expect.AreEqual(2, cellsPlanned);
    }
}

[TestFixture(TestType.MainMenu)]
internal sealed class ComputeIsCellWithinSowBudgetTests
{
    [Test]
    public static void UnrestrictedWhenLimitIsOff() =>
        Expect.IsTrue(
            ComputeIsCellWithinSowBudget(
                limitSowingToComputedNeed: false,
                cellSowPlan: null,
                cell: new IntVec3(0, 0, 0)
            )
        );

    [Test]
    public static void DisallowedEverywhereWhenLimitIsOnButNoPlanHasBeenComputedYet() =>
        // e.g. right after a save load, or before this job's first cycle since the setting was
        // enabled - a missing plan means the manager hasn't decided yet, not "no limit".
        Expect.IsFalse(
            ComputeIsCellWithinSowBudget(
                limitSowingToComputedNeed: true,
                cellSowPlan: null,
                cell: new IntVec3(0, 0, 0)
            )
        );

    [Test]
    public static void AllowedOnlyForCellsThePlanAssigns()
    {
        var cropA = new ThingDef
        {
            defName = "CropA",
            plant = new PlantProperties { harvestYield = 5 },
        };
        Dictionary<IntVec3, ThingDef> plan = new() { [new IntVec3(0, 0, 0)] = cropA };

        Expect.IsTrue(
            ComputeIsCellWithinSowBudget(
                limitSowingToComputedNeed: true,
                cellSowPlan: plan,
                cell: new IntVec3(0, 0, 0)
            )
        );
        Expect.IsFalse(
            ComputeIsCellWithinSowBudget(
                limitSowingToComputedNeed: true,
                cellSowPlan: plan,
                cell: new IntVec3(1, 0, 0)
            )
        );
    }
}
