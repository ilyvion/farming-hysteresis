using RimTestRedux;
using static FarmingHysteresis.ColonyManagerRedux.ManagerJob_FarmingHysteresis;

namespace FarmingHysteresis.ColonyManagerRedux.Tests;

// Regression guard for the grower-exclusivity mechanism: a grower must never end up managed by
// more than one Farming Hysteresis manager job at once. See Docs/CMRIntegrationRework.md, Step 2.
[HotSwappable]
[TestSuite]
internal static class ManagerJobFarmingHysteresisScopeTests
{
    [Test]
    public static void AllModeIgnoresAreaAndSpecificSelection()
    {
        Assert
            .That(
                IsGrowerInScope(
                    GrowerAssignmentMode.All,
                    inArea: false,
                    isSpecificallySelected: false
                )
            )
            .Is.True();
        Assert
            .That(
                IsGrowerInScope(
                    GrowerAssignmentMode.All,
                    inArea: true,
                    isSpecificallySelected: true
                )
            )
            .Is.True();
    }

    [Test]
    public static void AreaModeFollowsAreaMembership()
    {
        Assert
            .That(
                IsGrowerInScope(
                    GrowerAssignmentMode.Area,
                    inArea: true,
                    isSpecificallySelected: false
                )
            )
            .Is.True();
        Assert
            .That(
                IsGrowerInScope(
                    GrowerAssignmentMode.Area,
                    inArea: false,
                    isSpecificallySelected: true
                )
            )
            .Is.False();
    }

    [Test]
    public static void SpecificModeFollowsExplicitSelection()
    {
        Assert
            .That(
                IsGrowerInScope(
                    GrowerAssignmentMode.Specific,
                    inArea: true,
                    isSpecificallySelected: false
                )
            )
            .Is.False();
        Assert
            .That(
                IsGrowerInScope(
                    GrowerAssignmentMode.Specific,
                    inArea: false,
                    isSpecificallySelected: true
                )
            )
            .Is.True();
    }
}

// Regression guard for the "don't even offer it" fix: plants with no harvestedThingDef (e.g.
// purely decorative plants like roses) must never appear in ValidTargetPlants, since
// Trigger_Hysteresis would have nothing to count for them. See Docs/CMRIntegrationRework.md.
[HotSwappable]
[TestSuite]
internal static class ValidTargetPlantCandidateTests
{
    [Test]
    public static void PlantWithNoHarvestedThingDefIsNeverValidRegardlessOfAvailability()
    {
        var plantDef = new ThingDef { plant = new PlantProperties { harvestedThingDef = null } };

        Assert.That(IsValidTargetPlantCandidate(plantDef, _ => true)).Is.False();
    }

    [Test]
    public static void PlantWithHarvestedThingDefFollowsAvailabilityPredicate()
    {
        var plantDef = new ThingDef
        {
            plant = new PlantProperties { harvestedThingDef = new ThingDef() },
        };

        Assert.That(IsValidTargetPlantCandidate(plantDef, _ => true)).Is.True();
        Assert.That(IsValidTargetPlantCandidate(plantDef, _ => false)).Is.False();
    }
}

// FindOwningJob/ManagedGrowers themselves need a live Manager/JobTracker to exercise end to end,
// but the "first claim wins" exclusion they're built on is plain set arithmetic - covered here
// with plain fakes standing in for growers and per-job scopes.
[HotSwappable]
[TestSuite]
internal static class GrowerClaimResolutionTests
{
    private static List<string> ManagedByFirstClaim(params IReadOnlyList<string>[] jobScopesInOrder)
    {
        var claimed = new HashSet<string>();
        List<string> managedByLastJob = [];
        foreach (var scope in jobScopesInOrder)
        {
            managedByLastJob = [.. scope.Where(g => !claimed.Contains(g))];
            foreach (var grower in scope)
            {
                _ = claimed.Add(grower);
            }
        }
        return managedByLastJob;
    }

    [Test]
    public static void NonOverlappingScopesAreBothFullyManaged()
    {
        var managed = ManagedByFirstClaim(["zoneA"], ["zoneB"]);

        Assert.ThatCollection(managed).Has.Count(1);
        Assert.ThatCollection(managed).Does.Contain("zoneB");
    }

    [Test]
    public static void LaterJobLosesGrowerAlreadyClaimedByEarlierJob()
    {
        var laterJobManaged = ManagedByFirstClaim(["zoneA", "zoneB"], ["zoneB", "zoneC"]);

        Assert.ThatCollection(laterJobManaged).Does.Not.Contain("zoneB");
        Assert.ThatCollection(laterJobManaged).Does.Contain("zoneC");
    }
}

// Regression guard for ManagerJob_FarmingHysteresis.History's per-chapter selection logic (see
// Docs/CMRIntegrationRework.md, Step 3) - stock, lower bound, and upper bound are each their own
// flat/varying chapter, and none of them ever draw a target line.
[HotSwappable]
[TestSuite]
internal static class ManagerJobFarmingHysteresisHistoryTests
{
    [Test]
    public static void StockChapterCountIsTheTrackedThingCount() =>
        Assert
            .That(
                ManagerJob_FarmingHysteresis.History.SelectCount(
                    ManagerJobHistoryChapterDefOf.FH_HistoryStock,
                    trackedThingCount: 42,
                    lower: 10,
                    upper: 20
                )
            )
            .Is.EqualTo(42);

    [Test]
    public static void LowerChapterCountIsTheLowerBound() =>
        Assert
            .That(
                ManagerJob_FarmingHysteresis.History.SelectCount(
                    ManagerJobHistoryChapterDefOf.FH_HistoryLower,
                    trackedThingCount: 42,
                    lower: 10,
                    upper: 20
                )
            )
            .Is.EqualTo(10);

    [Test]
    public static void UpperChapterCountIsTheUpperBound() =>
        Assert
            .That(
                ManagerJob_FarmingHysteresis.History.SelectCount(
                    ManagerJobHistoryChapterDefOf.FH_HistoryUpper,
                    trackedThingCount: 42,
                    lower: 10,
                    upper: 20
                )
            )
            .Is.EqualTo(20);
}

// Regression guard for the "active crop follows identity, not position" fix (see
// Docs/CMRIntegrationRework.md's Step 5 follow-up): reordering/removing entries other than the
// active one must never silently change which crop is considered active - a plain index-based
// ActiveRotationIndex used to do exactly that whenever an earlier entry was removed.
[HotSwappable]
[TestSuite]
internal static class ComputeActiveEntryIdAfterRemovalTests
{
    [Test]
    public static void RemovingANonActiveEntryLeavesActiveIdUntouched()
    {
        // Entries [1, 2, 3] with 2 active; removing 1 (index 0) shifts 2 and 3 down a slot, but
        // the active id itself must still be 2, not whatever now sits at the old index.
        var result = ComputeActiveEntryIdAfterRemoval(
            activeEntryId: 2,
            removedEntryId: 1,
            removedIndex: 0,
            remainingEntryIds: [2, 3]
        );

        Assert.That(result).Is.EqualTo(2);
    }

    [Test]
    public static void RemovingTheActiveEntryFallsBackToWhateverNowOccupiesItsOldPosition()
    {
        var result = ComputeActiveEntryIdAfterRemoval(
            activeEntryId: 1,
            removedEntryId: 1,
            removedIndex: 0,
            remainingEntryIds: [2, 3]
        );

        Assert.That(result).Is.EqualTo(2);
    }

    [Test]
    public static void RemovingTheActiveEntryAtTheEndClampsToTheNewLastEntry()
    {
        var result = ComputeActiveEntryIdAfterRemoval(
            activeEntryId: 3,
            removedEntryId: 3,
            removedIndex: 2,
            remainingEntryIds: [1, 2]
        );

        Assert.That(result).Is.EqualTo(2);
    }

    [Test]
    public static void RemovingTheOnlyRemainingEntryLeavesNoActiveEntry()
    {
        var result = ComputeActiveEntryIdAfterRemoval(
            activeEntryId: 1,
            removedEntryId: 1,
            removedIndex: 0,
            remainingEntryIds: []
        );

        Assert.That(result).Is.Null();
    }
}

// Regression guard for the round-robin advance's position-based lookup: it must resolve the
// active entry's *current* list position (which reordering may have changed) rather than
// assuming it's stayed put, and degrade gracefully rather than throwing if it can't find a match.
[HotSwappable]
[TestSuite]
internal static class ComputeNextActiveEntryIdTests
{
    [Test]
    public static void AdvancesToTheNextEntryByCurrentPosition() =>
        Assert.That(ComputeNextActiveEntryId([1, 2, 3], activeEntryId: 1)).Is.EqualTo(2);

    [Test]
    public static void CyclesBackToTheFirstEntryAfterTheLast() =>
        Assert.That(ComputeNextActiveEntryId([1, 2, 3], activeEntryId: 3)).Is.EqualTo(1);

    // Entries reordered from [1, 2, 3] to [2, 1, 3] (1 and 2 swapped) - entry 1 is still active,
    // now at position 1 instead of 0, so the next entry should be 3, not 2.
    [Test]
    public static void FollowsTheActiveEntryToItsNewPositionAfterReordering() =>
        Assert.That(ComputeNextActiveEntryId([2, 1, 3], activeEntryId: 1)).Is.EqualTo(3);

    [Test]
    public static void FallsBackToTheFirstEntryWhenActiveIdIsMissingOrUnset()
    {
        Assert.That(ComputeNextActiveEntryId([1, 2, 3], activeEntryId: null)).Is.EqualTo(1);
        Assert.That(ComputeNextActiveEntryId([1, 2, 3], activeEntryId: 99)).Is.EqualTo(1);
    }
}

// Regression guard for RotationMode.Priority's active-entry selection (see
// Docs/CMRIntegrationRework.md's per-job rotation mode follow-up): list order is a priority
// order, so the winner is the first entry that isn't already satisfied (AboveUpperBound),
// skipping over any that are - not whichever entry was active before.
[HotSwappable]
[TestSuite]
internal static class ComputePriorityActiveEntryIdTests
{
    [Test]
    public static void FirstUnsatisfiedEntryByListOrderWins() =>
        Assert
            .That(
                ComputePriorityActiveEntryId(
                    [
                        (1, LatchMode.AboveUpperBound),
                        (2, LatchMode.BelowLowerBound),
                        (3, LatchMode.BelowLowerBound),
                    ],
                    previousActiveEntryId: 3
                )
            )
            .Is.EqualTo(2);

    [Test]
    public static void EarlierUnsatisfiedEntryTakesPriorityOverThePreviouslyActiveOne() =>
        Assert
            .That(
                ComputePriorityActiveEntryId(
                    [(1, LatchMode.BetweenBoundsEnabled), (2, LatchMode.BelowLowerBound)],
                    previousActiveEntryId: 2
                )
            )
            .Is.EqualTo(1);

    [Test]
    public static void FallsBackToThePreviouslyActiveEntryWhenEverythingIsSatisfied() =>
        Assert
            .That(
                ComputePriorityActiveEntryId(
                    [(1, LatchMode.AboveUpperBound), (2, LatchMode.AboveUpperBound)],
                    previousActiveEntryId: 2
                )
            )
            .Is.EqualTo(2);

    [Test]
    public static void FallsBackToTheFirstEntryWhenEverythingIsSatisfiedAndThereWasNoPreviousActiveEntry() =>
        Assert
            .That(
                ComputePriorityActiveEntryId(
                    [(1, LatchMode.AboveUpperBound), (2, LatchMode.AboveUpperBound)],
                    previousActiveEntryId: null
                )
            )
            .Is.EqualTo(1);
}

// Regression guard for RotationMode.RoundRobin's active-entry selection, now that it's a pure
// function (Trigger_Hysteresis.ComputeCycleUpdate/ApplyCycleUpdate split - see the Gather/Execute
// contract follow-up in Docs/CMRIntegrationRework.md): stays on the active entry unless *its own*
// latch just made a fresh transition into AboveUpperBound this cycle - a latch that was already
// AboveUpperBound going into the cycle must not re-trigger an advance every cycle thereafter.
[HotSwappable]
[TestSuite]
internal static class ComputeRoundRobinActiveEntryIdTests
{
    [Test]
    public static void StaysOnTheActiveEntryWhenItsLatchHasNotJustTransitioned() =>
        Assert
            .That(
                ComputeRoundRobinActiveEntryId(
                    [(1, LatchMode.BetweenBoundsEnabled), (2, LatchMode.Unknown)],
                    activeEntryId: 1,
                    previousActiveLatch: LatchMode.BetweenBoundsEnabled
                )
            )
            .Is.EqualTo(1);

    [Test]
    public static void AdvancesToTheNextEntryWhenTheActiveEntryJustBecameSatisfied() =>
        Assert
            .That(
                ComputeRoundRobinActiveEntryId(
                    [(1, LatchMode.AboveUpperBound), (2, LatchMode.Unknown)],
                    activeEntryId: 1,
                    previousActiveLatch: LatchMode.BetweenBoundsEnabled
                )
            )
            .Is.EqualTo(2);

    [Test]
    public static void DoesNotReAdvanceWhenTheActiveEntryWasAlreadySatisfiedLastCycle() =>
        Assert
            .That(
                ComputeRoundRobinActiveEntryId(
                    [(1, LatchMode.AboveUpperBound), (2, LatchMode.Unknown)],
                    activeEntryId: 1,
                    previousActiveLatch: LatchMode.AboveUpperBound
                )
            )
            .Is.EqualTo(1);

    [Test]
    public static void DoesNothingWithoutAPreviousLatchOrActiveEntry()
    {
        Assert
            .That(
                ComputeRoundRobinActiveEntryId(
                    [(1, LatchMode.AboveUpperBound)],
                    activeEntryId: null,
                    previousActiveLatch: LatchMode.BetweenBoundsEnabled
                )
            )
            .Is.Null();
        Assert
            .That(
                ComputeRoundRobinActiveEntryId(
                    [(1, LatchMode.AboveUpperBound)],
                    activeEntryId: 1,
                    previousActiveLatch: null
                )
            )
            .Is.EqualTo(1);
    }
}

// Regression guard for the crop-rotation "don't strand the outgoing crop" fix (see
// Docs/CMRIntegrationRework.md, Step 5 - resolves #6): a grower still holding a plant that isn't
// the active rotation entry's crop must be detected regardless of what else is standing in it.
[HotSwappable]
[TestSuite]
internal static class GrowerHasLeftoverPlantsTests
{
    [Test]
    public static void NoLeftoversWhenEveryCellMatchesTheTarget()
    {
        var target = new ThingDef();

        Assert.That(GrowerHasLeftoverPlants([target, target], target)).Is.False();
    }

    [Test]
    public static void NoLeftoversWhenCellsAreBareOrMatchTheTarget()
    {
        var target = new ThingDef();

        Assert.That(GrowerHasLeftoverPlants([target, null, null], target)).Is.False();
    }

    [Test]
    public static void HasLeftoversWhenAnyCellHoldsADifferentPlant()
    {
        var target = new ThingDef();
        var outgoing = new ThingDef();

        Assert.That(GrowerHasLeftoverPlants([target, outgoing, null], target)).Is.True();
    }
}
