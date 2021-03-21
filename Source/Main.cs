using HarmonyLib;
using Verse;

namespace FarmingHysteresis
{
	[StaticConstructorOnStartup]
	internal static class Mod
	{

		static Mod() => new Harmony(Constants.Id).PatchAll();
	}
}
