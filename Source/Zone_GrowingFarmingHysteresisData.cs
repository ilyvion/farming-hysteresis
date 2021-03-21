using System;
using System.Runtime.CompilerServices;
using FarmingHysteresis.Helpers.Extensions;
using RimWorld;
using Verse;

namespace FarmingHysteresis
{
	internal class FarmingHysteresisData
	{
		private bool enabled;
		private int lowerBound;
		private int upperBound;
		public LatchMode latchMode;

		public FarmingHysteresisData()
		{
			enabled = false;
			lowerBound = Constants.DefaultHysteresisLowerBound;
			upperBound = Constants.DefaultHysteresisUpperBound;
			latchMode = LatchMode.Unknown;
		}

		internal void ExposeData(ref Zone_Growing zone)
		{
			Scribe_Values.Look(ref enabled, "farmingHysteresisEnabled", false);
			Scribe_Values.Look(ref lowerBound, "farmingHysteresisLowerBound", Constants.DefaultHysteresisLowerBound);
			Scribe_Values.Look(ref upperBound, "farmingHysteresisUpperBound", Constants.DefaultHysteresisUpperBound);
			Scribe_Values.Look(ref latchMode, "farmingHysteresisLatchMode", LatchMode.Unknown);
		}

		public int LowerBound
		{
			get { return lowerBound; }
			set
			{
				if (value < 0)
				{
					value = 0;
				}
				if (value > upperBound)
				{
					value = upperBound;
				}
				lowerBound = value;
			}
		}

		public int UpperBound
		{
			get { return upperBound; }
			set
			{
				if (value < lowerBound)
				{
					value = lowerBound;
				}
				upperBound = value;
			}
		}

		internal bool Enabled
		{
			get { return enabled; }
		}

		internal void Enable(Zone_Growing zone)
		{
			enabled = true;
			UpdateLatchModeAndSowing(zone);
		}

		internal void Disable(Zone_Growing zone)
		{
			enabled = false;
		}

		internal void UpdateLatchModeAndSowing(Zone_Growing zone)
		{
			var (harvestedThingDef, harvestedThingCount) = zone.PlantHarvestInfo();
			if (harvestedThingDef == null)
			{
				DisableDueToMissingHarvestedThingDef(zone);
				return;
			}

			// First, check the simple cases
			if (harvestedThingCount < lowerBound)
			{
				// Below lower bound: Enabled
				latchMode = LatchMode.BelowLowerBound;
				zone.allowSow = true;
				return;
			}
			else if (harvestedThingCount > upperBound)
			{
				// Above upper bound: Disabled
				latchMode = LatchMode.AboveUpperBound;
				zone.allowSow = false;
				return;
			}

			// We know harvestedThingCount is between lower and upper bound
			// at this point thanks to the above checks.

			switch (latchMode)
			{
				case LatchMode.BelowLowerBound:
				case LatchMode.Unknown:
					// If we were previously below the lower bound, it's time to enter
					// the "above lower bound enabled" state.
					if (harvestedThingCount > lowerBound)
					{
						latchMode = LatchMode.AboveLowerBoundEnabled;
						zone.allowSow = true;
					}
					return;

				case LatchMode.AboveUpperBound:
					// If we were previously above the upper bound, it's time to enter
					// the "above lower bound disabled" state.
					if (harvestedThingCount < upperBound)
					{
						latchMode = LatchMode.AboveLowerBoundDisabled;
						zone.allowSow = false;
					}
					return;
			}
		}

		internal void DisableDueToMissingHarvestedThingDef(Zone_Growing zone)
		{
			enabled = false;
			Log.Message($"Zone {zone.ID} has a plant type without a harvestable product. Disabling farming hysteresis.");
		}
	}

	internal static class FarmingHysteresisDataExtensions
	{
		private static readonly ConditionalWeakTable<Zone_Growing, FarmingHysteresisData> dataTable = new ConditionalWeakTable<Zone_Growing, FarmingHysteresisData>();

		internal static FarmingHysteresisData GetFarmingHysteresisData(this Zone_Growing zone)
		{
			return dataTable.GetOrCreateValue(zone);
		}
	}
}