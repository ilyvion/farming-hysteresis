using FarmingHysteresis.ITabs;
using VanillaPlantsExpandedMorePlants;

namespace FarmingHysteresis.VanillaPlantsExpandedMorePlants.Patch;

/// <summary>
/// Add Hysteresis inspect tab to growing zones
/// </summary>
[HarmonyPatch(typeof(Zone), nameof(Zone.GetInspectTabs))]
internal static class Zone_GetInspectTabs
{
    private static readonly ITab[] ITabs = [new ITab_Hysteresis()];

    private static IEnumerable<InspectTabBase> Postfix(
        IEnumerable<InspectTabBase> values,
        Zone __instance
    )
    {
        if (__instance is not Zone_GrowingAquatic and not Zone_GrowingSandy)
        {
            return values;
        }

        values = values == null ? ITabs : values.Concat(ITabs);

        return values;
    }
}
