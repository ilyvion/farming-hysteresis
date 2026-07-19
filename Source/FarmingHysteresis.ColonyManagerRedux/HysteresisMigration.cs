using ColonyManagerRedux;
using FarmingHysteresis.Defs;
using FarmingHysteresis.Extensions;

namespace FarmingHysteresis.ColonyManagerRedux;

/// <summary>
/// Best-effort translation of a map's old-style (per-grower) hysteresis setup into CMR jobs, used
/// by <see cref="CmrMigrationGate"/>. Per the confirmed migration scope: only growers that
/// currently have hysteresis <i>enabled</i> are considered; they're grouped by the plant they're
/// currently set to grow (one job per distinct plant, <see cref="GrowerAssignmentMode.Specific"/>
/// with every grower in the group), and each job's bounds are set to whichever (Lower, Upper)
/// pair is most common among that group's growers - exact scopes/bounds-source tiers from the old
/// engine are deliberately not preserved, only the plant grouping and a representative bound pair.
/// </summary>
internal static class HysteresisMigration
{
    internal static void MigrateMap(Map map)
    {
        var manager = Manager.For(map);

        var enabledGrowers = FarmingHysteresisControlDef
            .AllControlledPlantGrowers(map)
            .Where(grower => grower.GetFarmingHysteresisData().Enabled);

        foreach (var (plantDef, growers) in GroupByTargetPlant(enabledGrowers))
        {
            CreateJobForGroup(manager, plantDef, growers);
        }
    }

    /// <summary>
    /// Groups <paramref name="growers"/> by their current <c>GetPlantDefToGrow()</c>, dropping
    /// growers with no plant chosen yet and plants with no <c>harvestedThingDef</c> (e.g.
    /// decorative-only plants like roses) - there'd be nothing for the resulting job's
    /// <see cref="Trigger_Hysteresis"/> to ever count for those, same reasoning as
    /// <see cref="ManagerJob_FarmingHysteresis.IsValidTargetPlantCandidate"/>.
    /// </summary>
    internal static IEnumerable<(
        ThingDef PlantDef,
        List<IPlantToGrowSettable> Growers
    )> GroupByTargetPlant(IEnumerable<IPlantToGrowSettable> growers) =>
        growers
            .Select(grower => (Grower: grower, PlantDef: grower.GetPlantDefToGrow()))
            .Where(x => x.PlantDef?.plant.harvestedThingDef != null)
            .GroupBy(x => x.PlantDef)
            .Select(group => (group.Key, group.Select(x => x.Grower).ToList()));

    private static void CreateJobForGroup(
        Manager manager,
        ThingDef plantDef,
        List<IPlantToGrowSettable> growers
    )
    {
        var def = DefDatabase<ManagerDef>.GetNamed("CM_FarmingHysteresisManager");
        var job = manager.NewJob<ManagerJob_FarmingHysteresis>(def);

        job.AssignmentMode = GrowerAssignmentMode.Specific;
        job.AddRotationEntry(plantDef);
        foreach (var grower in growers)
        {
            switch (grower)
            {
                case Zone zone:
                    _ = job.SpecificGrowingZones.Add(zone);
                    break;
                case Building_PlantGrower building:
                    _ = job.SpecificPlantGrowerBuildings.Add(building);
                    break;
                default:
                    break;
            }
        }

        var (lower, upper) = SelectMostCommonBounds([.. growers.Select(BoundsOf)], Rand.Range);
        job.HysteresisTrigger.Lower = lower;
        job.HysteresisTrigger.Upper = upper;

        // Mirrors ManagerTab_FarmingHysteresis's own Manage! button (IsManaged = true alongside
        // JobTracker.Add) - JobTracker.Add alone doesn't commit the job, so without this the
        // migrated job would sit in the tracker read as unmanaged, showing "Manage!" instead of
        // "Delete" and never actually running.
        job.IsManaged = true;
        manager.JobTracker.Add(job);
    }

    private static (int Lower, int Upper) BoundsOf(IPlantToGrowSettable grower)
    {
        var data = grower.GetFarmingHysteresisData();
        return (data.LowerBound, data.UpperBound);
    }

    /// <summary>
    /// Picks whichever (Lower, Upper) pair occurs most often in <paramref name="bounds"/>,
    /// breaking ties via <paramref name="randomIndex"/> (real callers pass
    /// <see cref="Rand.Range(int, int)"/>; tests pass a fixed picker) - split out so the
    /// mode/tie-break logic is unit-testable
    /// without live growers.
    /// </summary>
    internal static (int Lower, int Upper) SelectMostCommonBounds(
        IReadOnlyList<(int Lower, int Upper)> bounds,
        Func<int, int, int> randomIndex
    )
    {
        var winners = bounds
            .GroupBy(pair => pair)
            .GroupBy(g => g.Count())
            .OrderByDescending(g => g.Key)
            .First()
            .Select(g => g.Key)
            .ToList();

        return winners.Count == 1 ? winners[0] : winners[randomIndex(0, winners.Count)];
    }
}
