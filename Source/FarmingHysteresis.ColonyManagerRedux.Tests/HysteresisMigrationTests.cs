using DevTools.Testing;
using static FarmingHysteresis.ColonyManagerRedux.HysteresisMigration;

namespace FarmingHysteresis.ColonyManagerRedux.Tests;

// Covers the migration gate's "most common bounds win, tie broken randomly" mode-selection/
// tie-break logic in isolation from the live grower scan it's normally fed by.
[TestFixture(TestType.MainMenu)]
internal sealed class SelectMostCommonBoundsTests
{
    private static int FailIfCalled(int min, int max) =>
        throw new InvalidOperationException(
            "Tie-break picker should not be called for a clear winner."
        );

    private static int PickSecond(int min, int max) => 1;

    [Test]
    public static void SingleEntryIsItsOwnWinner()
    {
        var (lower, upper) = SelectMostCommonBounds([(10, 20)], FailIfCalled);

        Expect.AreEqual(10, lower);
        Expect.AreEqual(20, upper);
    }

    [Test]
    public static void ClearMajorityWinsOverMinority()
    {
        var (lower, upper) = SelectMostCommonBounds(
            [(10, 20), (10, 20), (10, 20), (0, 100)],
            FailIfCalled
        );

        Expect.AreEqual(10, lower);
        Expect.AreEqual(20, upper);
    }

    [Test]
    public static void TiedPairsAreResolvedViaTheProvidedPicker()
    {
        var (lower, upper) = SelectMostCommonBounds([(10, 20), (0, 100)], PickSecond);

        Expect.AreEqual(0, lower);
        Expect.AreEqual(100, upper);
    }
}

// Regression guard for the migration gate's suppression rule: takeover must stay suppressed
// while the one-time dialog is unanswered or was declined, and only stop being suppressed once
// the save has actually migrated (or was never gated in the first place).
[TestFixture(TestType.MainMenu)]
internal sealed class CmrMigrationGateStatusTests
{
    [Test]
    public static void AwaitingChoiceAndDeclinedSuppressTakeover()
    {
        Expect.IsTrue(
            CmrMigrationGameComponent.IsSuppressing(CmrMigrationGateStatus.AwaitingChoice)
        );
        Expect.IsTrue(CmrMigrationGameComponent.IsSuppressing(CmrMigrationGateStatus.Declined));
    }

    [Test]
    public static void NotGatedAndMigratedDoNotSuppressTakeover()
    {
        Expect.IsFalse(CmrMigrationGameComponent.IsSuppressing(CmrMigrationGateStatus.NotGated));
        Expect.IsFalse(CmrMigrationGameComponent.IsSuppressing(CmrMigrationGateStatus.Migrated));
    }
}

// Regression guard for a bug where FarmingHysteresisMod.HysteresisController could get stuck on
// CmrHysteresisController across a main-menu-and-reload cycle: CmrMigrationGate.HandleGameLoaded
// used to only resync the controller inside the two branches that actually needed to act (already
// gated, or beginning a fresh gate), so a save with nothing to migrate - the exact case where
// ShouldBeginMigrationGate is false - never resynced the controller to the current "take over
// Farming Hysteresis control" setting at all on load.
[TestFixture(TestType.MainMenu)]
internal sealed class ShouldBeginMigrationGateTests
{
    [Test]
    public static void BeginsGateOnlyWhenNotGatedAndTakeoverOnAndLegacyDataExists() =>
        Expect.IsTrue(
            CmrMigrationGate.ShouldBeginMigrationGate(
                CmrMigrationGateStatus.NotGated,
                takeoverHysteresisControl: true,
                hasLegacyHysteresisDataConfigured: true
            )
        );

    [Test]
    public static void DoesNotBeginGateWhenTakeoverIsOff() =>
        Expect.IsFalse(
            CmrMigrationGate.ShouldBeginMigrationGate(
                CmrMigrationGateStatus.NotGated,
                takeoverHysteresisControl: false,
                hasLegacyHysteresisDataConfigured: true
            )
        );

    [Test]
    public static void DoesNotBeginGateWhenThereIsNoLegacyData() =>
        Expect.IsFalse(
            CmrMigrationGate.ShouldBeginMigrationGate(
                CmrMigrationGateStatus.NotGated,
                takeoverHysteresisControl: true,
                hasLegacyHysteresisDataConfigured: false
            )
        );

    [Test]
    public static void DoesNotBeginGateWhenAlreadyGated()
    {
        foreach (
            var status in new[]
            {
                CmrMigrationGateStatus.AwaitingChoice,
                CmrMigrationGateStatus.Declined,
                CmrMigrationGateStatus.Migrated,
            }
        )
        {
            Expect.IsFalse(
                CmrMigrationGate.ShouldBeginMigrationGate(
                    status,
                    takeoverHysteresisControl: true,
                    hasLegacyHysteresisDataConfigured: true
                )
            );
        }
    }
}

// Regression guard for the "nothing to migrate" case: a save with no legacy setup shouldn't ever
// see the dialog, but it still needs to end up Migrated rather than staying NotGated forever
// (which would keep re-running the legacy-data scan on every single load).
[TestFixture(TestType.MainMenu)]
internal sealed class ShouldSilentlyMarkMigratedTests
{
    [Test]
    public static void MarksMigratedWhenNotGatedAndTakeoverOnAndNoLegacyData() =>
        Expect.IsTrue(
            CmrMigrationGate.ShouldSilentlyMarkMigrated(
                CmrMigrationGateStatus.NotGated,
                takeoverHysteresisControl: true,
                hasLegacyHysteresisDataConfigured: false
            )
        );

    [Test]
    public static void DoesNotMarkMigratedWhenLegacyDataExists() =>
        Expect.IsFalse(
            CmrMigrationGate.ShouldSilentlyMarkMigrated(
                CmrMigrationGateStatus.NotGated,
                takeoverHysteresisControl: true,
                hasLegacyHysteresisDataConfigured: true
            )
        );

    [Test]
    public static void DoesNotMarkMigratedWhenTakeoverIsOff() =>
        Expect.IsFalse(
            CmrMigrationGate.ShouldSilentlyMarkMigrated(
                CmrMigrationGateStatus.NotGated,
                takeoverHysteresisControl: false,
                hasLegacyHysteresisDataConfigured: false
            )
        );

    [Test]
    public static void DoesNotMarkMigratedWhenAlreadyGated()
    {
        foreach (
            var status in new[]
            {
                CmrMigrationGateStatus.AwaitingChoice,
                CmrMigrationGateStatus.Declined,
                CmrMigrationGateStatus.Migrated,
            }
        )
        {
            Expect.IsFalse(
                CmrMigrationGate.ShouldSilentlyMarkMigrated(
                    status,
                    takeoverHysteresisControl: true,
                    hasLegacyHysteresisDataConfigured: false
                )
            );
        }
    }
}

// Regression guard for the rule ApplyControllerState uses to pick between CmrHysteresisController
// and DefaultHysteresisController - the CMR controller should only ever be active when takeover is
// on and the current save isn't still being suppressed by the migration gate.
[TestFixture(TestType.MainMenu)]
internal sealed class ComputeShouldUseCmrControllerTests
{
    [Test]
    public static void UsesCmrOnlyWhenTakeoverIsOnAndNotSuppressed() =>
        Expect.IsTrue(
            ManagerSettings_FarmingHysteresis.ComputeShouldUseCmrController(
                takeOverHysteresisControl: true,
                isCurrentSaveSuppressingTakeover: false
            )
        );

    [Test]
    public static void DoesNotUseCmrWhenTakeoverIsOff() =>
        Expect.IsFalse(
            ManagerSettings_FarmingHysteresis.ComputeShouldUseCmrController(
                takeOverHysteresisControl: false,
                isCurrentSaveSuppressingTakeover: false
            )
        );

    [Test]
    public static void DoesNotUseCmrWhenSuppressed() =>
        Expect.IsFalse(
            ManagerSettings_FarmingHysteresis.ComputeShouldUseCmrController(
                takeOverHysteresisControl: true,
                isCurrentSaveSuppressingTakeover: true
            )
        );

    [Test]
    public static void DoesNotUseCmrWhenTakeoverIsOffAndSuppressed() =>
        Expect.IsFalse(
            ManagerSettings_FarmingHysteresis.ComputeShouldUseCmrController(
                takeOverHysteresisControl: false,
                isCurrentSaveSuppressingTakeover: true
            )
        );
}

// Regression guard for a bug where a save that had used CMR from the very start still got the
// migration dialog on every load: CmrMigrationGameComponent.Status used to default to NotGated
// both in memory (fresh component) and as the ExposeData fallback for a missing save node, so
// "genuinely never needed a gate" and "predates status tracking entirely" were indistinguishable,
// and the "nothing to migrate" outcome was never actually persisted (Scribe skips writing a value
// that matches its own default), so the decision had to be re-derived on every single load.
// Splitting the two defaults means any status other than NotGated now always gets scribed
// explicitly, so a save only ever sees NotGated again if it truly never resolved this gate before.
[TestFixture(TestType.MainMenu)]
internal sealed class CmrMigrationGameComponentStatusDefaultTests
{
    [Test]
    public static void FreshComponentDefaultsToMigratedNotNotGated() =>
        Expect.AreEqual(
            CmrMigrationGateStatus.Migrated,
            new CmrMigrationGameComponent(null!).Status
        );

    [Test]
    public static void ComputeDefaultStatusIsMigratedWhenTakeoverStartsOn() =>
        Expect.AreEqual(
            CmrMigrationGateStatus.Migrated,
            CmrMigrationGameComponent.ComputeDefaultStatus(takeoverHysteresisControl: true)
        );

    [Test]
    public static void ComputeDefaultStatusIsDeclinedWhenTakeoverStartsOff() =>
        Expect.AreEqual(
            CmrMigrationGateStatus.Declined,
            CmrMigrationGameComponent.ComputeDefaultStatus(takeoverHysteresisControl: false)
        );
}

// A Farming Hysteresis job must never be allowed to act on its growers while CMR isn't genuinely
// the active controller, even if the job itself would otherwise be managed.
[TestFixture(TestType.MainMenu)]
internal sealed class ManagerJobFarmingHysteresisIsManagedTests
{
    [Test]
    public static void ManagedOnlyWhenBaseManagedAndCmrIsActiveController()
    {
        Expect.IsTrue(ManagerJob_FarmingHysteresis.ComputeIsManaged(true, true));
        Expect.IsFalse(ManagerJob_FarmingHysteresis.ComputeIsManaged(true, false));
        Expect.IsFalse(ManagerJob_FarmingHysteresis.ComputeIsManaged(false, true));
        Expect.IsFalse(ManagerJob_FarmingHysteresis.ComputeIsManaged(false, false));
    }
}

// Minimal IPlantToGrowSettable stand-in for GroupByTargetPlant, whose own logic only ever calls
// GetPlantDefToGrow() - the other interface members are never touched by it and just throw if
// something starts relying on them.
file sealed class FakeGrower(ThingDef? plantDef) : IPlantToGrowSettable
{
    public ThingDef? GetPlantDefToGrow() => plantDef;

    public void SetPlantDefToGrow(ThingDef plantDef) => throw new NotImplementedException();

    public bool CanAcceptSowNow() => throw new NotImplementedException();

    public IEnumerable<IntVec3> Cells => throw new NotImplementedException();

    public Map Map => throw new NotImplementedException();
}

// Covers HysteresisMigration.GroupByTargetPlant's dropping/grouping rules in isolation from the
// live map scan it's normally fed by.
[TestFixture(TestType.MainMenu)]
internal sealed class GroupByTargetPlantTests
{
    private static ThingDef HarvestablePlant(string defName) =>
        new()
        {
            defName = defName,
            plant = new PlantProperties { harvestedThingDef = new ThingDef { label = defName } },
        };

    private static ThingDef DecorativePlant() => new() { plant = new PlantProperties() };

    private static ThingDef NonPlantDef() => new() { defName = "NotAPlant", plant = null };

    [Test]
    public static void GrowersOfANonPlantDefAreDroppedWithoutThrowing()
    {
        var grower = new FakeGrower(NonPlantDef());

        var groups = GroupByTargetPlant([grower]).ToList();

        Expect.IsEmpty(groups);
    }

    [Test]
    public static void GrowersWithNoPlantChosenAreDropped()
    {
        var grower = new FakeGrower(null);

        var groups = GroupByTargetPlant([grower]).ToList();

        Expect.IsEmpty(groups);
    }

    [Test]
    public static void GrowersOfADecorativeOnlyPlantAreDropped()
    {
        var grower = new FakeGrower(DecorativePlant());

        var groups = GroupByTargetPlant([grower]).ToList();

        Expect.IsEmpty(groups);
    }

    [Test]
    public static void GrowersOfTheSamePlantLandInOneGroupTogetherInOriginalOrder()
    {
        var hops = HarvestablePlant("hops");
        var growerA = new FakeGrower(hops);
        var growerB = new FakeGrower(hops);

        var groups = GroupByTargetPlant([growerA, growerB]).ToList();

        Expect.AreEqual(1, groups.Count);
        Expect.AreEqual(hops.defName, groups[0].PlantDef.defName);
        Expect.AreEqual(2, groups[0].Growers.Count);
        Expect.IsTrue(ReferenceEquals(groups[0].Growers[0], growerA));
        Expect.IsTrue(ReferenceEquals(groups[0].Growers[1], growerB));
    }

    [Test]
    public static void GrowersOfDifferentPlantsProduceSeparateGroups()
    {
        var hops = HarvestablePlant("hops");
        var rice = HarvestablePlant("rice");
        var hopsGrower = new FakeGrower(hops);
        var riceGrower = new FakeGrower(rice);

        var groups = GroupByTargetPlant([hopsGrower, riceGrower]).ToList();

        Expect.AreEqual(2, groups.Count);
        Expect.IsTrue(groups.Select(g => g.PlantDef).Contains(hops));
        Expect.IsTrue(groups.Select(g => g.PlantDef).Contains(rice));
    }
}
