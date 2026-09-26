using DevTools.Testing;
using FarmingHysteresis.Patch;

namespace FarmingHysteresis.Tests;

// Regression guard: a protected leftover plant's cell must only ever have its cut job
// suppressed, never any other job JobOnCell might return (e.g. the eventual Sow job once the
// cell is actually clear) - see ManagerJob_FarmingHysteresis.ExecuteJobDataCoroutine for why
// this is a per-plant cut suppression rather than a whole-grower "allow sow" override.
[TestFixture(TestType.MainMenu)]
internal sealed class WorkGiverGrowerSowJobOnCellTests
{
    [Test]
    public static void CutJobOnProtectedLeftoverIsSuppressed() =>
        Expect.IsTrue(
            WorkGiver_GrowerSow_JobOnCell.ShouldSuppressCut(
                jobDef: JobDefOf.CutPlant,
                protectLeftoverFromCut: true
            )
        );

    [Test]
    public static void CutJobIsUntouchedWhenNothingIsProtected() =>
        Expect.IsFalse(
            WorkGiver_GrowerSow_JobOnCell.ShouldSuppressCut(
                jobDef: JobDefOf.CutPlant,
                protectLeftoverFromCut: false
            )
        );

    [Test]
    public static void NonCutJobIsNeverSuppressedEvenIfProtected() =>
        Expect.IsFalse(
            WorkGiver_GrowerSow_JobOnCell.ShouldSuppressCut(
                jobDef: JobDefOf.Sow,
                protectLeftoverFromCut: true
            )
        );

    [Test]
    public static void SowJobOutsideTheSowBudgetIsSuppressed() =>
        Expect.IsTrue(
            WorkGiver_GrowerSow_JobOnCell.ShouldSuppressSow(
                jobDef: JobDefOf.Sow,
                cellSowAllowed: false
            )
        );

    [Test]
    public static void SowJobWithinTheSowBudgetIsUntouched() =>
        Expect.IsFalse(
            WorkGiver_GrowerSow_JobOnCell.ShouldSuppressSow(
                jobDef: JobDefOf.Sow,
                cellSowAllowed: true
            )
        );

    [Test]
    public static void NonSowJobIsNeverSuppressedEvenOutsideTheSowBudget() =>
        Expect.IsFalse(
            WorkGiver_GrowerSow_JobOnCell.ShouldSuppressSow(
                jobDef: JobDefOf.CutPlant,
                cellSowAllowed: false
            )
        );

    [Test]
    public static void SowJobWithAnOverridePlantIsRetargeted() =>
        Expect.IsTrue(
            WorkGiver_GrowerSow_JobOnCell.ShouldApplySowPlantOverride(
                jobDef: JobDefOf.Sow,
                plantOverride: new ThingDef { defName = "CascadedCrop" }
            )
        );

    [Test]
    public static void SowJobWithNoOverridePlantIsUntouched() =>
        Expect.IsFalse(
            WorkGiver_GrowerSow_JobOnCell.ShouldApplySowPlantOverride(
                jobDef: JobDefOf.Sow,
                plantOverride: null
            )
        );

    [Test]
    public static void NonSowJobIsNeverRetargetedEvenWithAnOverridePlant() =>
        Expect.IsFalse(
            WorkGiver_GrowerSow_JobOnCell.ShouldApplySowPlantOverride(
                jobDef: JobDefOf.CutPlant,
                plantOverride: new ThingDef { defName = "CascadedCrop" }
            )
        );
}
