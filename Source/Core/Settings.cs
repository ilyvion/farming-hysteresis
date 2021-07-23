using Verse;

namespace FarmingHysteresis
{
    public class Settings : ModSettings
    {
        public static string DefaultHysteresisLowerBoundBuffer;
        public static string DefaultHysteresisUpperBoundBuffer;

        internal static int _defaultHysteresisLowerBound = Constants.DefaultHysteresisLowerBound;
        internal static int _defaultHysteresisUpperBound = Constants.DefaultHysteresisUpperBound;

        public static int DefaultHysteresisLowerBound { get => _defaultHysteresisLowerBound; set => _defaultHysteresisLowerBound = value; }
        public static int DefaultHysteresisUpperBound { get => _defaultHysteresisUpperBound; set => _defaultHysteresisUpperBound = value; }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref _defaultHysteresisLowerBound, "defaultHysteresisLowerBound");
            Scribe_Values.Look(ref _defaultHysteresisUpperBound, "defaultHysteresisUpperBound");
        }
    }
}