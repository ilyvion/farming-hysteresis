using ColonyManagerRedux;
using DevTools.Testing;
using static FarmingHysteresis.ColonyManagerRedux.ManagerJob_FarmingHysteresis;

namespace FarmingHysteresis.ColonyManagerRedux.Tests;

// A grower must never end up managed by more than one Farming Hysteresis manager job at once.
[TestFixture(TestType.MainMenu)]
internal sealed class ManagerJobFarmingHysteresisScopeTests
{
    [Test]
    public static void AllModeIgnoresAreaAndSpecificSelection()
    {
        Expect.IsTrue(
            IsGrowerInScope(GrowerAssignmentMode.All, inArea: false, isSpecificallySelected: false)
        );
        Expect.IsTrue(
            IsGrowerInScope(GrowerAssignmentMode.All, inArea: true, isSpecificallySelected: true)
        );
    }

    [Test]
    public static void AreaModeFollowsAreaMembership()
    {
        Expect.IsTrue(
            IsGrowerInScope(GrowerAssignmentMode.Area, inArea: true, isSpecificallySelected: false)
        );
        Expect.IsFalse(
            IsGrowerInScope(GrowerAssignmentMode.Area, inArea: false, isSpecificallySelected: true)
        );
    }

    [Test]
    public static void SpecificModeFollowsExplicitSelection()
    {
        Expect.IsFalse(
            IsGrowerInScope(
                GrowerAssignmentMode.Specific,
                inArea: true,
                isSpecificallySelected: false
            )
        );
        Expect.IsTrue(
            IsGrowerInScope(
                GrowerAssignmentMode.Specific,
                inArea: false,
                isSpecificallySelected: true
            )
        );
    }
}

// Covers ClearUnboundOnCurrentLastEntry, AddRotationEntry's auto-clear step: a rotation entry's
// Unbound flag is only meaningful for the actual last entry, so whichever entry was previously
// last must give it up as soon as a new entry gets appended below it.
[TestFixture(TestType.MainMenu)]
internal sealed class ClearUnboundOnCurrentLastEntryTests
{
    [Test]
    public static void ClearsUnboundOnTheCurrentLastEntry()
    {
        List<CropRotationEntry> entries = [new(), new() { Unbound = true }];

        ClearUnboundOnCurrentLastEntry(entries);

        Expect.IsFalse(entries[^1].Unbound);
    }

    [Test]
    public static void LeavesEarlierEntriesUntouched()
    {
        var earlier = new CropRotationEntry { Unbound = true };
        List<CropRotationEntry> entries = [earlier, new() { Unbound = true }];

        ClearUnboundOnCurrentLastEntry(entries);

        // Not a realistic state (Unbound should never be set on a non-last entry to begin with),
        // but this confirms the clear is scoped to the last entry alone, not the whole list.
        Expect.IsTrue(earlier.Unbound);
    }

    [Test]
    public static void NoOpsOnAnEmptyList() => ClearUnboundOnCurrentLastEntry([]);
}

// Regression guard: plants with no harvestedThingDef (e.g. purely decorative plants like roses)
// must never appear in ValidTargetPlants, since Trigger_Hysteresis would have nothing to count
// for them.
[TestFixture(TestType.MainMenu)]
internal sealed class ValidTargetPlantCandidateTests
{
    [Test]
    public static void PlantWithNoHarvestedThingDefIsNeverValidRegardlessOfAvailability()
    {
        var plantDef = new ThingDef { plant = new PlantProperties { harvestedThingDef = null } };

        Expect.IsFalse(IsValidTargetPlantCandidate(plantDef, _ => true));
    }

    [Test]
    public static void PlantWithHarvestedThingDefFollowsAvailabilityPredicate()
    {
        var plantDef = new ThingDef
        {
            plant = new PlantProperties { harvestedThingDef = new ThingDef() },
        };

        Expect.IsTrue(IsValidTargetPlantCandidate(plantDef, _ => true));
        Expect.IsFalse(IsValidTargetPlantCandidate(plantDef, _ => false));
    }
}

// FindOwningJob/ManagedGrowers themselves need a live Manager/JobTracker to exercise end to end,
// but the "first claim wins" exclusion they're built on is plain set arithmetic - covered here
// with plain fakes standing in for growers and per-job scopes.
[TestFixture(TestType.MainMenu)]
internal sealed class GrowerClaimResolutionTests
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

        Expect.AreEqual(1, managed.Count);
        Expect.IsTrue(managed.Contains("zoneB"));
    }

    [Test]
    public static void LaterJobLosesGrowerAlreadyClaimedByEarlierJob()
    {
        var laterJobManaged = ManagedByFirstClaim(["zoneA", "zoneB"], ["zoneB", "zoneC"]);

        Expect.IsFalse(laterJobManaged.Contains("zoneB"));
        Expect.IsTrue(laterJobManaged.Contains("zoneC"));
    }
}

// Covers ManagerJob_FarmingHysteresis.History's per-chapter selection logic: stock, lower bound,
// and upper bound are each their own flat/varying chapter, and none of them ever draw a target
// line.
[TestFixture(TestType.MainMenu)]
internal sealed class ManagerJobFarmingHysteresisHistoryTests
{
    [Test]
    public static void StockChapterCountIsTheTrackedThingCount() =>
        Expect.AreEqual(
            42,
            ManagerJob_FarmingHysteresis.History.SelectCount(
                ManagerJobHistoryChapterDefOf.FH_HistoryStock,
                trackedThingCount: 42,
                lower: 10,
                upper: 20
            )
        );

    [Test]
    public static void LowerChapterCountIsTheLowerBound() =>
        Expect.AreEqual(
            10,
            ManagerJob_FarmingHysteresis.History.SelectCount(
                ManagerJobHistoryChapterDefOf.FH_HistoryLower,
                trackedThingCount: 42,
                lower: 10,
                upper: 20
            )
        );

    [Test]
    public static void UpperChapterCountIsTheUpperBound() =>
        Expect.AreEqual(
            20,
            ManagerJob_FarmingHysteresis.History.SelectCount(
                ManagerJobHistoryChapterDefOf.FH_HistoryUpper,
                trackedThingCount: 42,
                lower: 10,
                upper: 20
            )
        );
}

// Regression guard: reordering/removing entries other than the active one must never silently
// change which crop is considered active - the active entry must be tracked by identity, not by
// list position.
[TestFixture(TestType.MainMenu)]
internal sealed class ComputeActiveEntryIdAfterRemovalTests
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

        Expect.AreEqual(2, result);
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

        Expect.AreEqual(2, result);
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

        Expect.AreEqual(2, result);
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

        Expect.IsTrue(result == null);
    }
}

// Regression guard for the round-robin advance's position-based lookup: it must resolve the
// active entry's *current* list position (which reordering may have changed) rather than
// assuming it's stayed put, and degrade gracefully rather than throwing if it can't find a match.
[TestFixture(TestType.MainMenu)]
internal sealed class ComputeNextActiveEntryIdTests
{
    [Test]
    public static void AdvancesToTheNextEntryByCurrentPosition() =>
        Expect.AreEqual(2, ComputeNextActiveEntryId([1, 2, 3], activeEntryId: 1));

    [Test]
    public static void CyclesBackToTheFirstEntryAfterTheLast() =>
        Expect.AreEqual(1, ComputeNextActiveEntryId([1, 2, 3], activeEntryId: 3));

    // Entries reordered from [1, 2, 3] to [2, 1, 3] (1 and 2 swapped) - entry 1 is still active,
    // now at position 1 instead of 0, so the next entry should be 3, not 2.
    [Test]
    public static void FollowsTheActiveEntryToItsNewPositionAfterReordering() =>
        Expect.AreEqual(3, ComputeNextActiveEntryId([2, 1, 3], activeEntryId: 1));

    [Test]
    public static void FallsBackToTheFirstEntryWhenActiveIdIsMissingOrUnset()
    {
        Expect.AreEqual(1, ComputeNextActiveEntryId([1, 2, 3], activeEntryId: null));
        Expect.AreEqual(1, ComputeNextActiveEntryId([1, 2, 3], activeEntryId: 99));
    }
}

// Covers RotationMode.Priority's active-entry selection: list order is a priority order, so the
// winner is the first entry that isn't already satisfied (AboveUpperBound), skipping over any
// that are - not whichever entry was active before.
[TestFixture(TestType.MainMenu)]
internal sealed class ComputePriorityActiveEntryIdTests
{
    [Test]
    public static void FirstUnsatisfiedEntryByListOrderWins() =>
        Expect.AreEqual(
            2,
            ComputePriorityActiveEntryId(
                [
                    (1, LatchMode.AboveUpperBound),
                    (2, LatchMode.BelowLowerBound),
                    (3, LatchMode.BelowLowerBound),
                ],
                previousActiveEntryId: 3
            )
        );

    [Test]
    public static void EarlierUnsatisfiedEntryTakesPriorityOverThePreviouslyActiveOne() =>
        Expect.AreEqual(
            1,
            ComputePriorityActiveEntryId(
                [(1, LatchMode.BetweenBoundsEnabled), (2, LatchMode.BelowLowerBound)],
                previousActiveEntryId: 2
            )
        );

    [Test]
    public static void FallsBackToThePreviouslyActiveEntryWhenEverythingIsSatisfied() =>
        Expect.AreEqual(
            2,
            ComputePriorityActiveEntryId(
                [(1, LatchMode.AboveUpperBound), (2, LatchMode.AboveUpperBound)],
                previousActiveEntryId: 2
            )
        );

    [Test]
    public static void FallsBackToTheFirstEntryWhenEverythingIsSatisfiedAndThereWasNoPreviousActiveEntry() =>
        Expect.AreEqual(
            1,
            ComputePriorityActiveEntryId(
                [(1, LatchMode.AboveUpperBound), (2, LatchMode.AboveUpperBound)],
                previousActiveEntryId: null
            )
        );

    // Regression test: an entry that dropped back under its upper bound without reaching its
    // lower bound (BetweenBoundsDisabled) must still be treated as disallowed, same as
    // AboveUpperBound - previously this method only checked for AboveUpperBound, so it
    // incorrectly switched back to entry 1 as soon as its count dipped under the upper bound,
    // instead of waiting for it to drop below the lower bound like the hysteresis rule requires.
    [Test]
    public static void StaysOnTheLowerPriorityEntryWhileTheHigherPriorityOneIsBetweenBoundsDisabled() =>
        Expect.AreEqual(
            2,
            ComputePriorityActiveEntryId(
                [(1, LatchMode.BetweenBoundsDisabled), (2, LatchMode.BelowLowerBound)],
                previousActiveEntryId: 2
            )
        );
}

// Covers RotationMode.RoundRobin's active-entry selection: stays on the active entry unless *its
// own* latch just made a fresh transition into AboveUpperBound this cycle - a latch that was
// already AboveUpperBound going into the cycle must not re-trigger an advance every cycle
// thereafter.
[TestFixture(TestType.MainMenu)]
internal sealed class ComputeRoundRobinActiveEntryIdTests
{
    [Test]
    public static void StaysOnTheActiveEntryWhenItsLatchHasNotJustTransitioned() =>
        Expect.AreEqual(
            1,
            ComputeRoundRobinActiveEntryId(
                [(1, LatchMode.BetweenBoundsEnabled), (2, LatchMode.Unknown)],
                activeEntryId: 1,
                previousActiveLatch: LatchMode.BetweenBoundsEnabled
            )
        );

    [Test]
    public static void AdvancesToTheNextEntryWhenTheActiveEntryJustBecameSatisfied() =>
        Expect.AreEqual(
            2,
            ComputeRoundRobinActiveEntryId(
                [(1, LatchMode.AboveUpperBound), (2, LatchMode.Unknown)],
                activeEntryId: 1,
                previousActiveLatch: LatchMode.BetweenBoundsEnabled
            )
        );

    [Test]
    public static void DoesNotReAdvanceWhenTheActiveEntryWasAlreadySatisfiedLastCycle() =>
        Expect.AreEqual(
            1,
            ComputeRoundRobinActiveEntryId(
                [(1, LatchMode.AboveUpperBound), (2, LatchMode.Unknown)],
                activeEntryId: 1,
                previousActiveLatch: LatchMode.AboveUpperBound
            )
        );

    [Test]
    public static void DoesNothingWithoutAPreviousLatchOrActiveEntry()
    {
        Expect.IsTrue(
            ComputeRoundRobinActiveEntryId(
                [(1, LatchMode.AboveUpperBound)],
                activeEntryId: null,
                previousActiveLatch: LatchMode.BetweenBoundsEnabled
            ) == null
        );
        Expect.AreEqual(
            1,
            ComputeRoundRobinActiveEntryId(
                [(1, LatchMode.AboveUpperBound)],
                activeEntryId: 1,
                previousActiveLatch: null
            )
        );
    }
}

// Minimal IPlantToGrowSettable stand-in for GrowerLabel's fallback branch - neither a Zone nor a
// Thing, so it should fall through to the type-name case.
file sealed class FakeGrower : IPlantToGrowSettable
{
    public ThingDef? GetPlantDefToGrow() => throw new NotImplementedException();

    public void SetPlantDefToGrow(ThingDef plantDef) => throw new NotImplementedException();

    public bool CanAcceptSowNow() => throw new NotImplementedException();

    public IEnumerable<IntVec3> Cells => throw new NotImplementedException();

    public Map Map => throw new NotImplementedException();
}

// Covers GrowerLabel's fallback branch for a grower that's neither a Zone nor a Thing.
[TestFixture(TestType.MainMenu)]
internal sealed class GrowerLabelTests
{
    [Test]
    public static void FallsBackToTheGrowerTypeNameForAnUnrecognizedGrowerType()
    {
        var grower = new FakeGrower();

        Expect.AreEqual(grower.GetType().Name, GrowerLabel(grower));
    }
}

// Regression guard: a grower still holding a plant that isn't the active rotation entry's crop
// must be detected regardless of what else is standing in it, so the outgoing crop doesn't get
// stranded during a rotation switch - but only when that other plant is itself one of this job's
// rotation crops, not any incidental non-rotation plant (e.g. a wild filler plant sprouting in an
// unsown cell), which must never count as a leftover.
[TestFixture(TestType.MainMenu)]
internal sealed class GrowerHasLeftoverPlantsTests
{
    [Test]
    public static void NoLeftoversWhenEveryCellMatchesTheTarget()
    {
        var target = new ThingDef();

        Expect.IsFalse(GrowerHasLeftoverPlants([target, target], target, [target]));
    }

    [Test]
    public static void NoLeftoversWhenCellsAreBareOrMatchTheTarget()
    {
        var target = new ThingDef();

        Expect.IsFalse(GrowerHasLeftoverPlants([target, null, null], target, [target]));
    }

    [Test]
    public static void HasLeftoversWhenAnyCellHoldsAnotherRotationCrop()
    {
        var target = new ThingDef();
        var outgoing = new ThingDef();

        Expect.IsTrue(
            GrowerHasLeftoverPlants([target, outgoing, null], target, [target, outgoing])
        );
    }

    [Test]
    public static void NoLeftoversWhenTheOtherPlantIsntPartOfTheRotation()
    {
        var target = new ThingDef();
        var wildPlant = new ThingDef();

        Expect.IsFalse(GrowerHasLeftoverPlants([target, wildPlant, null], target, [target]));
    }
}

// Regression guard: a grower having a leftover rotation crop somewhere must not make every other
// plant on that grower look like a leftover too - CmrHysteresisController.ShouldProtectLeftoverFromCut
// checks the specific plant being targeted for cutting via this method, not GrowerHasLeftoverPlants,
// so a non-rotation plant elsewhere on the same grower is never protected from being cut.
[TestFixture(TestType.MainMenu)]
internal sealed class IsLeftoverPlantTests
{
    [Test]
    public static void TargetPlantIsNeverALeftover()
    {
        var target = new ThingDef();

        Expect.IsFalse(IsLeftoverPlant(target, target, [target]));
    }

    [Test]
    public static void BarePlantIsNeverALeftover()
    {
        var target = new ThingDef();

        Expect.IsFalse(IsLeftoverPlant(null, target, [target]));
    }

    [Test]
    public static void OtherRotationCropIsALeftover()
    {
        var target = new ThingDef();
        var outgoing = new ThingDef();

        Expect.IsTrue(IsLeftoverPlant(outgoing, target, [target, outgoing]));
    }

    [Test]
    public static void PlantOutsideTheRotationIsNeverALeftover()
    {
        var target = new ThingDef();
        var wildPlant = new ThingDef();

        Expect.IsFalse(IsLeftoverPlant(wildPlant, target, [target]));
    }
}

// Regression guard: PostLoadInit must prune unresolved (null) scribed references instead of
// dereferencing them, since an unresolved LookMode.Reference is ordinary (the referenced
// building/zone was already gone at save time), not corruption.
[TestFixture(TestType.MainMenu)]
internal sealed class UnresolvedSpecificGrowerReferenceCleanupTests
{
    [Test]
    public static void UnresolvedGrowingZoneIsRemoved() =>
        Expect.IsTrue(ShouldRemoveUnresolvedGrowingZone(isNull: true, cellCount: 0));

    [Test]
    public static void EmptyGrowingZoneIsRemoved() =>
        Expect.IsTrue(ShouldRemoveUnresolvedGrowingZone(isNull: false, cellCount: 0));

    [Test]
    public static void NonEmptyResolvedGrowingZoneIsKept() =>
        Expect.IsFalse(ShouldRemoveUnresolvedGrowingZone(isNull: false, cellCount: 1));

    [Test]
    public static void UnresolvedPlantGrowerBuildingIsRemoved() =>
        Expect.IsTrue(ShouldRemoveUnresolvedPlantGrowerBuilding(isNull: true, destroyed: false));

    [Test]
    public static void DestroyedPlantGrowerBuildingIsRemoved() =>
        Expect.IsTrue(ShouldRemoveUnresolvedPlantGrowerBuilding(isNull: false, destroyed: true));

    [Test]
    public static void ResolvedPlantGrowerBuildingIsKept() =>
        Expect.IsFalse(ShouldRemoveUnresolvedPlantGrowerBuilding(isNull: false, destroyed: false));
}

// Regression guard: specifically selected plant grower buildings must survive a save/load
// round-trip. PostLoadInit runs before loaded things are spawned, so the cleanup must not treat
// a not-yet-spawned building as gone.
[TestFixture(TestType.Playing)]
internal sealed class SpecificPlantGrowerBuildingSaveLoadTests
{
    [Test]
    public static void SpecificPlantGrowerBuildingSurvivesPostLoadInit()
    {
        var map = Find.CurrentMap;
        var manager = Manager.For(map);
        var job = new ManagerJob_FarmingHysteresis(manager)
        {
            AssignmentMode = GrowerAssignmentMode.Specific,
        };
        var building = (Building_PlantGrower)ThingMaker.MakeThing(ThingDefOf.HydroponicsBasin);
        _ = job.SpecificPlantGrowerBuildings.Add(building);

        Expect.IsFalse(building.Spawned);

        job.PruneUnresolvedSpecificGrowers();

        Expect.IsTrue(job.SpecificPlantGrowerBuildings.Contains(building));
    }
}

// Regression guard: a deleted Area used as GrowerArea must be cleared out via
// Notify_AreaRemoved, not left dangling until the next save/load cycle re-resolves the reference
// as null.
[TestFixture(TestType.MainMenu)]
internal sealed class IsRemovedAreaTests
{
    [Test]
    public static void MatchingGrowerAreaIsRecognizedAsRemoved()
    {
        var area = new Area_Home();
        Expect.IsTrue(IsRemovedArea(area, area));
    }

    [Test]
    public static void DifferentAreaIsNotRecognizedAsRemoved()
    {
        var growerArea = new Area_Home();
        var removedArea = new Area_Home();
        Expect.IsFalse(IsRemovedArea(growerArea, removedArea));
    }

    [Test]
    public static void NoGrowerAreaIsNotRecognizedAsRemoved() =>
        Expect.IsFalse(IsRemovedArea(null, new Area_Home()));
}

// A job saved before per-job HysteresisMode existed has no scribed node for
// HasMigratedHysteresisMode, so it loads that flag as false (ExposeData's scribed default) -
// which must pick up the old global HysteresisMode setting instead of whatever
// HysteresisMode.Sowing default the (missing) node would otherwise have resolved to. A job saved
// after the field existed must keep exactly what it loaded, regardless of the legacy value.
[TestFixture(TestType.MainMenu)]
internal sealed class ResolveHysteresisModeAfterLoadTests
{
    [Test]
    public static void AlreadyMigratedJobKeepsItsLoadedMode() =>
        Expect.AreEqual(
            HysteresisMode.Harvesting,
            ResolveHysteresisModeAfterLoad(
                hasMigratedHysteresisMode: true,
                loadedHysteresisMode: HysteresisMode.Harvesting,
                legacyHysteresisMode: HysteresisMode.SowingAndHarvesting
            )
        );

    [Test]
    public static void UnmigratedJobAdoptsTheLegacyGlobalMode() =>
        Expect.AreEqual(
            HysteresisMode.SowingAndHarvesting,
            ResolveHysteresisModeAfterLoad(
                hasMigratedHysteresisMode: false,
                loadedHysteresisMode: HysteresisMode.Sowing,
                legacyHysteresisMode: HysteresisMode.SowingAndHarvesting
            )
        );
}
