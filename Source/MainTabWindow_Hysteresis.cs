using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FarmingHysteresis
{
    public class MainTabWindow_Hysteresis : MainTabWindow
    {
        private enum HysteresisTab : byte
        {
            HysteresisValues,
            SomethingElse,
        }

        private List<TabRecord> tabs = new List<TabRecord>();

        private static HysteresisTab currentTab = HysteresisTab.HysteresisValues;

        private Dictionary<ThingDef, IBoundedValueAccessor> globalBoundAccessors = new Dictionary<ThingDef, IBoundedValueAccessor>();
        private Dictionary<ThingDef, BoundValues> globalBounds = new();
        private Dictionary<ThingDef, String> globalBoundLowerBuffers = new Dictionary<ThingDef, String>();
        private Dictionary<ThingDef, String> globalBoundUpperBuffers = new Dictionary<ThingDef, String>();

        public override void PreOpen()
        {
            base.PreOpen();
            tabs.Clear();
            tabs.Add(new TabRecord("FarmingHysteresis.GlobalHysteresisBounds".Translate(), delegate
            {
                currentTab = HysteresisTab.HysteresisValues;
            }, () => currentTab == HysteresisTab.HysteresisValues));
            // tabs.Add(new TabRecord("SomethingElse".Translate(), delegate
            // {
            //     currentTab = HysteresisTab.SomethingElse;
            // }, () => currentTab == HysteresisTab.SomethingElse));

            globalBoundAccessors.Clear();
            globalBoundLowerBuffers.Clear();
            globalBoundUpperBuffers.Clear();
            globalBounds.Clear();
            foreach (ThingDef plantDef in DefDatabase<ThingDef>.AllDefs.Where((ThingDef def) => def.category == ThingCategory.Plant))
            {
                var harvestedThingDef = plantDef.plant.harvestedThingDef;
                if (harvestedThingDef == null || globalBoundAccessors.ContainsKey(harvestedThingDef))
                {
                    continue;
                }
                globalBoundAccessors.Add(
                    harvestedThingDef,
                    FarmingHysteresisMapComponent.For(Find.CurrentMap).GetGlobalBoundedValueAccessorFor(harvestedThingDef)
                );
                globalBounds.Add(harvestedThingDef, globalBoundAccessors[harvestedThingDef].BoundValueRaw);
                globalBoundLowerBuffers.Add(harvestedThingDef, null);
                globalBoundUpperBuffers.Add(harvestedThingDef, null);
            }
        }

        public override void DoWindowContents(Rect rect)
        {
            Rect rect2 = rect;
            rect2.yMin += 45f;
            TabDrawer.DrawTabs(rect2, tabs);
            switch (currentTab)
            {
                case HysteresisTab.HysteresisValues:
                    DoHysteresisValuesPage(rect2);
                    break;
                    // case HysteresisTab.SomethingElse:
                    //     DoSomethingElsePage(rect2);
                    //     break;
            }
        }

        private Vector2 messagesScrollPos;
        private float scrollViewHeight;

        private void DoHysteresisValuesPage(Rect rect2)
        {
            Rect viewRect = new Rect(0f, 0f, rect2.width - 16f, scrollViewHeight);
            Widgets.BeginScrollView(rect2, ref messagesScrollPos, viewRect);

            float num = 0f;
            foreach (ThingDef harvestedThingDef in globalBoundAccessors.Keys) // DefDatabase<ThingDef>.AllDefs.Where((ThingDef def) => def.category == ThingCategory.Plant))
            {
                var value = globalBounds[harvestedThingDef].Lower;
                var buffer = globalBoundLowerBuffers[harvestedThingDef];

                GUI.color = new Color(1f, 1f, 1f, 0.2f);
                Widgets.DrawLineHorizontal(0f, num, viewRect.width);
                GUI.color = Color.white;

                num += DrawPlantRow(harvestedThingDef, num, viewRect);
            }

            if (Event.current.type == EventType.Layout)
            {
                scrollViewHeight = num;
            }
            Widgets.EndScrollView();
        }

        const float PLANT_ROW_HEIGHT = 52f;
        const float PLANT_ROW_GAP_WIDTH = 32f;

        private float DrawPlantRow(ThingDef harvest, float rowY, Rect fillRect)
        {
            Rect rowRect = new Rect(0f, rowY, fillRect.width, PLANT_ROW_HEIGHT);
            Rect labelRect = new Rect(90f, rowY, 250f, PLANT_ROW_HEIGHT);
            Rect plantIconRect = new Rect(24f, rowY + 3f, 42f, 42f);

            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            Widgets.DrawHighlightIfMouseover(rowRect);
            GUI.color = Color.white;

            DrawHarvestIconWithLabelAndTooltip(plantIconRect, labelRect, harvest);

            var lowerBoundRect = DrawLowerBoundWidget(labelRect, rowY, harvest);
            var upperBoundRect = DrawUpperBoundWidget(lowerBoundRect, rowY, harvest);

            return PLANT_ROW_HEIGHT;
        }

        public static void DrawHarvestIconWithLabelAndTooltip(Rect harvestIconRect, Rect harvestLabelRect, ThingDef harvestDef)
        {
            GUI.DrawTexture(harvestIconRect, harvestDef.uiIcon);

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(harvestLabelRect, harvestDef.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;

            if (Mouse.IsOver(harvestIconRect))
            {
                TipSignal tip = new TipSignal(harvestDef.LabelCap.Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + harvestDef.description);
                TooltipHandler.TipRegion(harvestIconRect, tip);
            }
            if (Mouse.IsOver(harvestLabelRect))
            {
                TipSignal tip = new TipSignal(harvestDef.LabelCap.Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + harvestDef.description);
                TooltipHandler.TipRegion(harvestLabelRect, tip);
            }

            if (Widgets.ButtonInvisible(harvestIconRect, doMouseoverSound: false) || Widgets.ButtonInvisible(harvestLabelRect, doMouseoverSound: false))
            {
                Find.WindowStack.Add(new Dialog_InfoCard(harvestDef));
            }
        }

        public Rect DrawLowerBoundWidget(Rect prevRect, float rowY, ThingDef harvestDef)
        {
            var lowerBoundRect = new Rect(prevRect.xMax, rowY, 250f, PLANT_ROW_HEIGHT);
            var listingStandard = new Listing_Standard();
            listingStandard.Begin(lowerBoundRect);
            ref var value = ref globalBounds[harvestDef].Lower;
            var buffer = globalBoundLowerBuffers[harvestDef];
            listingStandard.Label("FarmingHysteresis.LowerBoundLabel".Translate());
            listingStandard.IntEntry(ref value, ref buffer);
            listingStandard.End();

            return lowerBoundRect;
        }

        public Rect DrawUpperBoundWidget(Rect prevRect, float rowY, ThingDef harvestDef)
        {
            var upperBoundRect = new Rect(prevRect.xMax + PLANT_ROW_GAP_WIDTH, rowY, 250f, PLANT_ROW_HEIGHT);
            var listingStandard = new Listing_Standard();
            listingStandard.Begin(upperBoundRect);
            ref var value = ref globalBounds[harvestDef].Upper;
            var buffer = globalBoundUpperBuffers[harvestDef];
            listingStandard.Label("FarmingHysteresis.UpperBoundLabel".Translate());
            listingStandard.IntEntry(ref value, ref buffer);
            listingStandard.End();

            return upperBoundRect;
        }

        // private void DoSomethingElsePage(Rect rect2)
        // {
        // }
    }
}
