#if v1_7_OR_GREATER
using ColonyManagerRedux;
using DevTools.Testing;

namespace FarmingHysteresis.ColonyManagerRedux.Tests;

// Regression guard: CoreCLR's JIT can inline the one-line Zone.Deregister into its callers, so
// this goes through vanilla's caller, Zone.Delete.
[TestFixture(TestType.Playing)]
internal sealed class ZoneDeregisterTests
{
    [Test]
    public static void DeletingAZoneRemovesItFromSpecificGrowingZones()
    {
        var map = Find.CurrentMap;
        var manager = Manager.For(map);
        var job = new ManagerJob_FarmingHysteresis(manager);
        manager.JobTracker.Add(job);
        try
        {
            var zone = new Zone_Growing(map.zoneManager);
            map.zoneManager.RegisterZone(zone);
            _ = job.SpecificGrowingZones.Add(zone);

            zone.Delete(playSound: false);

            Expect.IsFalse(map.zoneManager.AllZones.Contains(zone));
            Expect.IsFalse(job.SpecificGrowingZones.Contains(zone));
        }
        finally
        {
            manager.JobTracker.Delete(job, cleanup: false);
        }
    }
}
#endif
