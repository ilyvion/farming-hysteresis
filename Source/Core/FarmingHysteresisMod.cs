using HarmonyLib;
using Verse;
using System.Reflection;
using UnityEngine;

namespace FarmingHysteresis
{
    public class FarmingHysteresisMod : Mod
    {
        private ModContentPack content;

        public FarmingHysteresisMod(ModContentPack content) : base(content)
        {
            this.content = content;

            new Harmony(Constants.Id).PatchAll();

            GetSettings<Settings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);

            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.Label("FarmingHysteresis.DefaultLowerBound".Translate());
            listingStandard.IntEntry(ref Settings._defaultHysteresisLowerBound, ref Settings.DefaultHysteresisLowerBoundBuffer);

            listingStandard.Label("FarmingHysteresis.DefaultUpperBound".Translate());
            listingStandard.IntEntry(ref Settings._defaultHysteresisUpperBound, ref Settings.DefaultHysteresisUpperBoundBuffer);

            listingStandard.End();
        }

        public override string SettingsCategory()
        {
            return content.Name;
        }
    }
}
