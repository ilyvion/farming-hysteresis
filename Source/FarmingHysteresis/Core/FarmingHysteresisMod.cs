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
            Settings.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return content.Name;
        }
    }
}
