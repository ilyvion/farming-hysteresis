using System.Linq;
using RimWorld;
using Verse;

namespace FarmingHysteresis
{
	public class FarmingHysteresisMapComponent : MapComponent, ILoadReferenceable
	{
		private int id = -1;

		public FarmingHysteresisMapComponent(Map map) : base(map)
		{
			// if not created in SavingLoading, give yourself the ID of the map you were constructed on.
			if (Scribe.mode == Verse.LoadSaveMode.Inactive) id = map.uniqueID;
		}

		public string GetUniqueLoadID()
		{
			return "FarmingHysteresisMapComponent_" + id;
		}

		public override void MapComponentTick()
		{
			base.MapComponentTick();

			// No need to make these checks every single tick; once every in-game hour should suffice.
			if (Find.TickManager.TicksGame % 2500 != 0) return;

			foreach (var zone in map.zoneManager.AllZones.OfType<Zone_Growing>())
			{
				var data = zone.GetFarmingHysteresisData();
				if (data.enabled)
				{
					data.UpdateLatchModeAndSowing(zone);
				}
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref id, "id", -1, true);
		}
	}
}
