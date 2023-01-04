using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FarmingHysteresis
{
    public class Settings : ModSettings
    {
        private static string? _defaultHysteresisLowerBoundBuffer;
        private static string? _defaultHysteresisUpperBoundBuffer;

        private static int _defaultHysteresisLowerBound = Constants.DefaultHysteresisLowerBound;
        private static int _defaultHysteresisUpperBound = Constants.DefaultHysteresisUpperBound;
        private static bool _enabledByDefault = true;
        private static bool _useGlobalValuesByDefault = true;
        private static HysteresisMode _hysteresisMode = HysteresisMode.Sowing;

        internal static int DefaultHysteresisLowerBound { get => _defaultHysteresisLowerBound; set => _defaultHysteresisLowerBound = value; }
        internal static int DefaultHysteresisUpperBound { get => _defaultHysteresisUpperBound; set => _defaultHysteresisUpperBound = value; }
        internal static bool EnabledByDefault { get => _enabledByDefault; set => _enabledByDefault = value; }
        internal static bool UseGlobalValuesByDefault { get => _useGlobalValuesByDefault; set => _useGlobalValuesByDefault = value; }
        internal static HysteresisMode HysteresisMode { get => _hysteresisMode; set => _hysteresisMode = value; }

        internal static bool ControlSowing => _hysteresisMode == HysteresisMode.Sowing || _hysteresisMode == HysteresisMode.SowingAndHarvesting;
        internal static bool ControlHarvesting => _hysteresisMode == HysteresisMode.Harvesting || _hysteresisMode == HysteresisMode.SowingAndHarvesting;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref _defaultHysteresisLowerBound, "defaultHysteresisLowerBound", Constants.DefaultHysteresisLowerBound);
            Scribe_Values.Look(ref _defaultHysteresisUpperBound, "defaultHysteresisUpperBound", Constants.DefaultHysteresisUpperBound);
            Scribe_Values.Look(ref _enabledByDefault, "enabledByDefault", true);
            Scribe_Values.Look(ref _useGlobalValuesByDefault, "useGlobalValuesByDefault", true);
            Scribe_Values.Look(ref _hysteresisMode, "hysteresisMode", HysteresisMode.Sowing);
        }

        public static void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.CheckboxLabeled("FarmingHysteresis.EnabledByDefault".Translate(), ref _enabledByDefault);
            listingStandard.CheckboxLabeled("FarmingHysteresis.UseGlobalValuesByDefault".Translate(), ref _useGlobalValuesByDefault);

            if (listingStandard.ButtonTextLabeledPct(
                "FarmingHysteresis.HysteresisMode".Translate(),
                "FarmingHysteresis.Control".Translate(_hysteresisMode.AsString()),
                0.6f,
                TextAnchor.MiddleLeft))
            {
                List<FloatMenuOption> list = new();
                list.Add(new FloatMenuOption(
                    "FarmingHysteresis.Control".Translate("FarmingHysteresis.Sowing".Translate()),
                    () => _hysteresisMode = HysteresisMode.Sowing));
                list.Add(new FloatMenuOption(
                    "FarmingHysteresis.Control".Translate("FarmingHysteresis.Harvesting".Translate()),
                    () => _hysteresisMode = HysteresisMode.Harvesting));
                list.Add(new FloatMenuOption(
                    "FarmingHysteresis.Control".Translate("FarmingHysteresis.SowingAndHarvesting".Translate()),
                    () => _hysteresisMode = HysteresisMode.SowingAndHarvesting));
                Find.WindowStack.Add(new FloatMenu(list));
            }

            listingStandard.Label("FarmingHysteresis.DefaultLowerBound".Translate());
            listingStandard.IntEntry(ref _defaultHysteresisLowerBound, ref _defaultHysteresisLowerBoundBuffer);

            listingStandard.Label("FarmingHysteresis.DefaultUpperBound".Translate());
            listingStandard.IntEntry(ref _defaultHysteresisUpperBound, ref _defaultHysteresisUpperBoundBuffer);

            listingStandard.End();
        }
    }

    public enum HysteresisMode
    {
        Sowing,
        Harvesting,
        SowingAndHarvesting,
    }
    public static class HysteresisModeExtensions
    {
        public static string AsString(this HysteresisMode mode)
        {
            switch (mode)
            {
                case HysteresisMode.Sowing:
                    return "FarmingHysteresis.Sowing".Translate();
                case HysteresisMode.Harvesting:
                    return "FarmingHysteresis.Harvesting".Translate();
                case HysteresisMode.SowingAndHarvesting:
                    return "FarmingHysteresis.SowingAndHarvesting".Translate();

                default:
                    throw new Exception($"Uncovered HysteresisMode: {mode}");
            }
        }
    }
}
