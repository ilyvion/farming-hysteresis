using System;
using System.Runtime.CompilerServices;
using FarmingHysteresis.Helpers.Extensions;
using RimWorld;
using Verse;

namespace FarmingHysteresis
{
	internal class FarmingHysteresisData : IBoundedValueAccessor
	{
		private System.WeakReference<Zone_Growing> zoneWeakReference;

		private bool enabled;
		private int lowerBound;
		private int upperBound;
		public LatchMode latchMode;

		public bool useGlobalValues;

		public FarmingHysteresisData(System.WeakReference<Zone_Growing> weakReference)
		{
			zoneWeakReference = weakReference;
			enabled = false;
			lowerBound = Constants.DefaultHysteresisLowerBound;
			upperBound = Constants.DefaultHysteresisUpperBound;
			latchMode = LatchMode.Unknown;
		}

		internal void ExposeData()
		{
			Scribe_Values.Look(ref enabled, "farmingHysteresisEnabled", false);
			Scribe_Values.Look(ref lowerBound, "farmingHysteresisLowerBound", Constants.DefaultHysteresisLowerBound);
			Scribe_Values.Look(ref upperBound, "farmingHysteresisUpperBound", Constants.DefaultHysteresisUpperBound);
			Scribe_Values.Look(ref latchMode, "farmingHysteresisLatchMode", LatchMode.Unknown);
			Scribe_Values.Look(ref useGlobalValues, "farmingHysteresisUseGlobalValues", false);
		}

		int IBoundedValueAccessor.LowerBoundValueRaw { get => lowerBound; set => lowerBound = value; }
		int IBoundedValueAccessor.UpperBoundValueRaw { get => upperBound; set => upperBound = value; }

		private IBoundedValueAccessor GetBoundedValueAccessor()
		{
			IBoundedValueAccessor values;
			if (!useGlobalValues)
			{
				values = this;
			}
			else
			{
				if (zoneWeakReference.TryGetTarget(out var zone))
				{
					var (harvestedThingDef, _) = zone.PlantHarvestInfo();
					values = FarmingHysteresisMapComponent.For(Find.CurrentMap).GetGlobalBoundedValueAccessorFor(harvestedThingDef);
				}
				else
				{
					throw new Exception("This should not happen. Code: FHD-GBVA");
				}
			}
			return values;
		}

		public int LowerBound
		{
			get => GetBoundedValueAccessor().LowerBoundValueRaw;
			set
			{
				IBoundedValueAccessor values = GetBoundedValueAccessor();
				if (value < 0)
				{
					value = 0;
				}
				else if (value > values.UpperBoundValueRaw)
				{
					value = values.UpperBoundValueRaw;
				}
				values.LowerBoundValueRaw = value;
			}
		}

		public int UpperBound
		{
			get => GetBoundedValueAccessor().UpperBoundValueRaw;
			set
			{
				IBoundedValueAccessor values = GetBoundedValueAccessor();
				if (value < values.LowerBoundValueRaw)
				{
					value = values.LowerBoundValueRaw;
				}
				values.UpperBoundValueRaw = value;
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

			IBoundedValueAccessor values = GetBoundedValueAccessor();
			
			// First, check the simple cases
			if (harvestedThingCount < values.LowerBoundValueRaw)
			{
				// Below lower bound: Enabled
				latchMode = LatchMode.BelowLowerBound;
				zone.allowSow = true;
				return;
			}
			else if (harvestedThingCount > values.UpperBoundValueRaw)
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
					if (harvestedThingCount > values.LowerBoundValueRaw)
					{
						latchMode = LatchMode.AboveLowerBoundEnabled;
						zone.allowSow = true;
					}
					return;

				case LatchMode.AboveUpperBound:
					// If we were previously above the upper bound, it's time to enter
					// the "above lower bound disabled" state.
					if (harvestedThingCount < values.UpperBoundValueRaw)
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
			return dataTable.GetValue(zone, (z) => new FarmingHysteresisData(new System.WeakReference<Zone_Growing>(z)));
		}
	}
}