using RimTestRedux;
using static FarmingHysteresis.ColonyManagerRedux.HysteresisMigration;

namespace FarmingHysteresis.ColonyManagerRedux.Tests;

// Regression guard for the migration gate's "most common bounds win, tie broken randomly" rule
// (see Docs/CMRIntegrationRework.md, Step 2's "default value and migration" section) - covers the
// mode-selection/tie-break logic in isolation from the live grower scan it's normally fed by.
[HotSwappable]
[TestSuite]
internal static class SelectMostCommonBoundsTests
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

        Assert.That(lower).Is.EqualTo(10);
        Assert.That(upper).Is.EqualTo(20);
    }

    [Test]
    public static void ClearMajorityWinsOverMinority()
    {
        var (lower, upper) = SelectMostCommonBounds(
            [(10, 20), (10, 20), (10, 20), (0, 100)],
            FailIfCalled
        );

        Assert.That(lower).Is.EqualTo(10);
        Assert.That(upper).Is.EqualTo(20);
    }

    [Test]
    public static void TiedPairsAreResolvedViaTheProvidedPicker()
    {
        var (lower, upper) = SelectMostCommonBounds([(10, 20), (0, 100)], PickSecond);

        Assert.That(lower).Is.EqualTo(0);
        Assert.That(upper).Is.EqualTo(100);
    }
}

// Regression guard for the migration gate's suppression rule: takeover must stay suppressed
// while the one-time dialog is unanswered or was declined, and only stop being suppressed once
// the save has actually migrated (or was never gated in the first place).
[HotSwappable]
[TestSuite]
internal static class CmrMigrationGateStatusTests
{
    [Test]
    public static void AwaitingChoiceAndDeclinedSuppressTakeover()
    {
        Assert
            .That(CmrMigrationGameComponent.IsSuppressing(CmrMigrationGateStatus.AwaitingChoice))
            .Is.True();
        Assert
            .That(CmrMigrationGameComponent.IsSuppressing(CmrMigrationGateStatus.Declined))
            .Is.True();
    }

    [Test]
    public static void NotGatedAndMigratedDoNotSuppressTakeover()
    {
        Assert
            .That(CmrMigrationGameComponent.IsSuppressing(CmrMigrationGateStatus.NotGated))
            .Is.False();
        Assert
            .That(CmrMigrationGameComponent.IsSuppressing(CmrMigrationGateStatus.Migrated))
            .Is.False();
    }
}

// Regression guard for a bug where FarmingHysteresisMod.HysteresisController could get stuck on
// CmrHysteresisController across a main-menu-and-reload cycle: CmrMigrationGate.HandleGameLoaded
// used to only resync the controller inside the two branches that actually needed to act (already
// gated, or beginning a fresh gate), so a save with nothing to migrate - the exact case where
// ShouldBeginMigrationGate is false - never resynced the controller to the current "take over
// Farming Hysteresis control" setting at all on load.
[HotSwappable]
[TestSuite]
internal static class ShouldBeginMigrationGateTests
{
    [Test]
    public static void BeginsGateOnlyWhenNotGatedAndTakeoverOnAndLegacyDataExists() =>
        Assert
            .That(
                CmrMigrationGate.ShouldBeginMigrationGate(
                    CmrMigrationGateStatus.NotGated,
                    takeoverHysteresisControl: true,
                    hasLegacyHysteresisDataConfigured: true
                )
            )
            .Is.True();

    [Test]
    public static void DoesNotBeginGateWhenTakeoverIsOff() =>
        Assert
            .That(
                CmrMigrationGate.ShouldBeginMigrationGate(
                    CmrMigrationGateStatus.NotGated,
                    takeoverHysteresisControl: false,
                    hasLegacyHysteresisDataConfigured: true
                )
            )
            .Is.False();

    [Test]
    public static void DoesNotBeginGateWhenThereIsNoLegacyData() =>
        Assert
            .That(
                CmrMigrationGate.ShouldBeginMigrationGate(
                    CmrMigrationGateStatus.NotGated,
                    takeoverHysteresisControl: true,
                    hasLegacyHysteresisDataConfigured: false
                )
            )
            .Is.False();

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
            Assert
                .That(
                    CmrMigrationGate.ShouldBeginMigrationGate(
                        status,
                        takeoverHysteresisControl: true,
                        hasLegacyHysteresisDataConfigured: true
                    )
                )
                .Is.False();
        }
    }
}

// Regression guard for the "never a mix" invariant (see Docs/CMRIntegrationRework.md's "Job
// execution respects takeover state" section): a Farming Hysteresis job must never be allowed to
// act on its growers while CMR isn't genuinely the active controller, even if the job itself
// would otherwise be managed.
[HotSwappable]
[TestSuite]
internal static class ManagerJobFarmingHysteresisIsManagedTests
{
    [Test]
    public static void ManagedOnlyWhenBaseManagedAndCmrIsActiveController()
    {
        Assert.That(ManagerJob_FarmingHysteresis.ComputeIsManaged(true, true)).Is.True();
        Assert.That(ManagerJob_FarmingHysteresis.ComputeIsManaged(true, false)).Is.False();
        Assert.That(ManagerJob_FarmingHysteresis.ComputeIsManaged(false, true)).Is.False();
        Assert.That(ManagerJob_FarmingHysteresis.ComputeIsManaged(false, false)).Is.False();
    }
}
