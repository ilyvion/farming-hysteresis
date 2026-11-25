using DevTools.Testing;
using static FarmingHysteresis.ColonyManagerRedux.ManagerJob_FarmingHysteresis;
#if v1_7_OR_GREATER
using ColonyManagerRedux;
#endif

#if v1_7_OR_GREATER
using FarmingHysteresis.Extensions;
#endif

namespace FarmingHysteresis.ColonyManagerRedux.Tests;

// A grower that drops out of a job's control must be handed back, unless it's gone or another job
// now controls it.
[TestFixture(TestType.MainMenu)]
internal sealed class ComputeGrowersToReleaseTests
{
    [Test]
    public static void ReleasesGrowersNoLongerControlled() =>
        Expect.AreEqual(
            "b",
            string.Join(",", ComputeGrowersToRelease(["a", "b"], ["a"], _ => false))
        );

    [Test]
    public static void KeepsGrowersStillControlled() =>
        Expect.IsEmpty(ComputeGrowersToRelease(["a", "b"], ["a", "b", "c"], _ => false));

    [Test]
    public static void SkipsGrowersThatShouldBeLeftAlone() =>
        Expect.AreEqual(
            "c",
            string.Join(",", ComputeGrowersToRelease(["a", "b", "c"], [], g => g is "a" or "b"))
        );
}

#if v1_7_OR_GREATER
// Regression guards for a CMR job's grower handling:
// - a managed grower that can't grow the active crop must be left alone instead of being gated,
//   so it can never stall the job for every other grower;
// - a grower that leaves the job's scope must get sowing allowed again instead of being stuck
//   with whatever the job last set.
[TestFixture(TestType.Playing)]
internal sealed class ControlledGrowersTests
{
    [Test]
    public static void IncompatibleGrowerIsLeftAloneAndCompatibleOneIsGated()
    {
        var map = Find.CurrentMap;
        WithJob(
            map,
            (job, zone) =>
            {
                var basin = SpawnHydroponicsBasin(map);
                try
                {
                    _ = job.SpecificPlantGrowerBuildings.Add(basin);
                    Expect.IsTrue(basin.GetAllowSow());

                    job.ReassertControlledGrowers();

                    Expect.IsFalse(job.CanGrowActiveCropOn(basin));
                    Expect.IsTrue(job.CanGrowActiveCropOn(zone));
                    Expect.IsFalse(job.ControlledGrowers.Contains(basin));
                    Expect.IsTrue(basin.GetAllowSow());
                    Expect.IsFalse(zone.allowSow);
                }
                finally
                {
                    basin.Destroy();
                }
            }
        );
    }

    [Test]
    public static void GrowerLeavingScopeGetsSowingAllowedAgain()
    {
        var map = Find.CurrentMap;
        WithJob(
            map,
            (job, zone) =>
            {
                job.ReassertControlledGrowers();
                Expect.IsFalse(zone.allowSow);

                _ = job.SpecificGrowingZones.Remove(zone);
                job.ReassertControlledGrowers();

                Expect.IsTrue(zone.allowSow);
            }
        );
    }

    [Test]
    public static void DeletingTheJobGivesSowingBack()
    {
        var map = Find.CurrentMap;
        WithJob(
            map,
            (job, zone) =>
            {
                job.ReassertControlledGrowers();
                Expect.IsFalse(zone.allowSow);

                job.CleanUp();

                Expect.IsTrue(zone.allowSow);
            }
        );
    }

    /// <summary>
    /// Runs <paramref name="test"/> against a CMR-controlled, sowing-only job in Specific mode
    /// whose active crop is corn (soil only), latched above its upper bound, with one growing zone
    /// selected. Restores the controller and removes the job and zone afterwards.
    /// </summary>
    private static void WithJob(Map map, Action<ManagerJob_FarmingHysteresis, Zone_Growing> test)
    {
        var previousController = FarmingHysteresisMod.HysteresisController;
        var manager = Manager.For(map);
        var job = new ManagerJob_FarmingHysteresis(manager)
        {
            AssignmentMode = GrowerAssignmentMode.Specific,
            HysteresisMode = HysteresisMode.Sowing,
        };
        job.AddRotationEntry(DefDatabase<ThingDef>.GetNamed("Plant_Corn"));
        job.RotationEntries[0].LatchModeValue = LatchMode.AboveUpperBound;

        var zone = new Zone_Growing(map.zoneManager);
        map.zoneManager.RegisterZone(zone);
        zone.AddCell(FindFreeCell(map, IntVec2.One));
        _ = job.SpecificGrowingZones.Add(zone);

        FarmingHysteresisMod.HysteresisController = CmrHysteresisController.Instance;
        manager.JobTracker.Add(job);
        job.IsManaged = true;
        try
        {
            test(job, zone);
        }
        finally
        {
            manager.JobTracker.Delete(job, cleanup: false);
            FarmingHysteresisMod.HysteresisController = previousController;
            zone.Delete(playSound: false);
        }
    }

    private static Building_PlantGrower SpawnHydroponicsBasin(Map map)
    {
        var def = ThingDefOf.HydroponicsBasin;
        var basin = (Building_PlantGrower)ThingMaker.MakeThing(def, GenStuff.DefaultStuffFor(def));
        basin.SetFaction(Faction.OfPlayer);
        return (Building_PlantGrower)GenSpawn.Spawn(basin, FindFreeCell(map, def.size), map);
    }

    private static IntVec3 FindFreeCell(Map map, IntVec2 size) =>
        map.AllCells.First(cell =>
            GenAdj
                .OccupiedRect(cell, Rot4.North, size)
                .All(c =>
                    c.InBounds(map)
                    && c.Standable(map)
                    && c.GetZone(map) == null
                    && c.GetFirstBuilding(map) == null
                )
        );
}
#endif
