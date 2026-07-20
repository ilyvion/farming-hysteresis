using FarmingHysteresis.ITabs;
using VanillaPlantsExpandedMorePlants;
using CoreZoneGetInspectTabs = FarmingHysteresis.Patch.Zone_GetInspectTabs;

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
    ) =>
        CoreZoneGetInspectTabs.ComputeInspectTabs(
            __instance is Zone_GrowingAquatic or Zone_GrowingSandy,
            values,
            ITabs
        )!;
}
