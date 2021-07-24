using UnityEngine;
using Verse;

namespace FarmingHysteresis
{
    public class Settings : ModSettings
    {
        private static string _defaultHysteresisLowerBoundBuffer;
        private static string _defaultHysteresisUpperBoundBuffer;

        private static int _defaultHysteresisLowerBound = Constants.DefaultHysteresisLowerBound;
        private static int _defaultHysteresisUpperBound = Constants.DefaultHysteresisUpperBound;
        private static bool _enabledByDefault = true;
        private static bool _useGlobalValuesByDefault = true;

        internal static int DefaultHysteresisLowerBound { get => _defaultHysteresisLowerBound; set => _defaultHysteresisLowerBound = value; }
        internal static int DefaultHysteresisUpperBound { get => _defaultHysteresisUpperBound; set => _defaultHysteresisUpperBound = value; }
        internal static bool EnabledByDefault { get => _enabledByDefault; set => _enabledByDefault = value; }
        internal static bool UseGlobalValuesByDefault { get => _useGlobalValuesByDefault; set => _useGlobalValuesByDefault = value; }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref _defaultHysteresisLowerBound, "defaultHysteresisLowerBound", Constants.DefaultHysteresisLowerBound);
            Scribe_Values.Look(ref _defaultHysteresisUpperBound, "defaultHysteresisUpperBound", Constants.DefaultHysteresisUpperBound);
            Scribe_Values.Look(ref _enabledByDefault, "enabledByDefault", true);
            Scribe_Values.Look(ref _useGlobalValuesByDefault, "useGlobalValuesByDefault", true);
        }

        public static void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.CheckboxLabeled("FarmingHysteresis.EnabledByDefault".Translate(), ref _useGlobalValuesByDefault);
            listingStandard.CheckboxLabeled("FarmingHysteresis.UseGlobalValuesByDefault".Translate(), ref _useGlobalValuesByDefault);

            listingStandard.Label("FarmingHysteresis.DefaultLowerBound".Translate());
            listingStandard.IntEntry(ref _defaultHysteresisLowerBound, ref _defaultHysteresisLowerBoundBuffer);

            listingStandard.Label("FarmingHysteresis.DefaultUpperBound".Translate());
            listingStandard.IntEntry(ref _defaultHysteresisUpperBound, ref _defaultHysteresisUpperBoundBuffer);

            listingStandard.End();
        }
    }
}