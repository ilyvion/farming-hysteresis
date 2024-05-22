using System;
using System.Collections.Generic;
using HarmonyLib;
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
        private static bool _countAllOnMap = false;
        private static HysteresisMode _hysteresisMode = HysteresisMode.Sowing;
        private static bool _showOldCommands = false;
        private static bool _showHysteresisMainTab = true;

        internal static int DefaultHysteresisLowerBound { get => _defaultHysteresisLowerBound; set => _defaultHysteresisLowerBound = value; }
        internal static int DefaultHysteresisUpperBound { get => _defaultHysteresisUpperBound; set => _defaultHysteresisUpperBound = value; }
        internal static bool EnabledByDefault { get => _enabledByDefault; set => _enabledByDefault = value; }
        internal static bool UseGlobalValuesByDefault { get => _useGlobalValuesByDefault; set => _useGlobalValuesByDefault = value; }
        internal static bool CountAllOnMap { get => _countAllOnMap; set => _countAllOnMap = value; }
        internal static HysteresisMode HysteresisMode { get => _hysteresisMode; set => _hysteresisMode = value; }
        internal static bool ShowOldCommands { get => _showOldCommands; set => _showOldCommands = value; }
        internal static bool ShowHysteresisMainTab { get => _showHysteresisMainTab; set => _showHysteresisMainTab = value; }

        internal static bool ControlSowing => _hysteresisMode == HysteresisMode.Sowing || _hysteresisMode == HysteresisMode.SowingAndHarvesting;
        internal static bool ControlHarvesting => _hysteresisMode == HysteresisMode.Harvesting || _hysteresisMode == HysteresisMode.SowingAndHarvesting;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref _defaultHysteresisLowerBound, "defaultHysteresisLowerBound", Constants.DefaultHysteresisLowerBound);
            Scribe_Values.Look(ref _defaultHysteresisUpperBound, "defaultHysteresisUpperBound", Constants.DefaultHysteresisUpperBound);
            Scribe_Values.Look(ref _enabledByDefault, "enabledByDefault", true);
            Scribe_Values.Look(ref _useGlobalValuesByDefault, "useGlobalValuesByDefault", true);
            Scribe_Values.Look(ref _countAllOnMap, "countAllOnMap", false);
            Scribe_Values.Look(ref _hysteresisMode, "hysteresisMode", HysteresisMode.Sowing);
            Scribe_Values.Look(ref _showOldCommands, "showOldCommands", false);
            Scribe_Values.Look(ref _showHysteresisMainTab, "showHysteresisMainTab", true);
        }

        public static void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.CheckboxLabeled("FarmingHysteresis.EnabledByDefault".Translate(), ref _enabledByDefault);
            listingStandard.CheckboxLabeled("FarmingHysteresis.UseGlobalValuesByDefault".Translate(), ref _useGlobalValuesByDefault);

            // Calculate where the CountAllOnMap checkbox will go
            var textHeight = Text.CalcHeight("FarmingHysteresis.CountAllOnMap".Translate(), listingStandard.ColumnWidth);
            Rect textRect = new Rect(
                Traverse.Create(listingStandard).Field<float>("curX").Value,
                Traverse.Create(listingStandard).Field<float>("curY").Value,
                listingStandard.ColumnWidth,
                textHeight);

            listingStandard.CheckboxLabeled("FarmingHysteresis.CountAllOnMap".Translate(), ref _countAllOnMap, "FarmingHysteresis.CountAllOnMapTooltip".Translate());

            // Render expensive icon inline in CountAllOnMap checkbox row
            Rect iconRect = new Rect(inRect.xMax - 16f - 32f, 0f, 16f, 16f)
                .CenteredOnYIn(textRect);
            TooltipHandler.TipRegion(iconRect, "FarmingHysteresis.Expensive.Tooltip".Translate());
            GUI.color = _countAllOnMap ? Resources.Orange : Color.grey;
            GUI.DrawTexture(iconRect, Resources.Stopwatch);
            GUI.color = Color.white;

            listingStandard.CheckboxLabeled(
                "FarmingHysteresis.ShowOldCommands".Translate(),
                ref _showOldCommands,
                "FarmingHysteresis.ShowOldCommandsTooltip".Translate());
            listingStandard.CheckboxLabeled(
                "FarmingHysteresis.ShowHysteresisMainTab".Translate(),
                ref _showHysteresisMainTab,
                "FarmingHysteresis.ShowHysteresisMainTabTooltip".Translate());

#if v1_3
            if (listingStandard.ButtonTextLabeled(
                "FarmingHysteresis.HysteresisMode".Translate(),
                "FarmingHysteresis.Control".Translate(_hysteresisMode.AsString())))
            {
#else
            if (listingStandard.ButtonTextLabeledPct(
                "FarmingHysteresis.HysteresisMode".Translate(),
                "FarmingHysteresis.Control".Translate(_hysteresisMode.AsString()),
                0.6f,
                TextAnchor.MiddleLeft))
            {
#endif
                List<FloatMenuOption> list = new()
                {
                    new FloatMenuOption(
                        "FarmingHysteresis.Control".Translate("FarmingHysteresis.Sowing".Translate()),
                        () => _hysteresisMode = HysteresisMode.Sowing),
                    new FloatMenuOption(
                        "FarmingHysteresis.Control".Translate("FarmingHysteresis.Harvesting".Translate()),
                        () => _hysteresisMode = HysteresisMode.Harvesting),
                    new FloatMenuOption(
                        "FarmingHysteresis.Control".Translate("FarmingHysteresis.SowingAndHarvesting".Translate()),
                        () => _hysteresisMode = HysteresisMode.SowingAndHarvesting)
                };
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
