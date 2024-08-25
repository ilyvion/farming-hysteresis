using FarmingHysteresis.ITabs;

namespace FarmingHysteresis.Patch;

/// <summary>
/// Add Hysteresis inspect tab to growing zones
/// </summary>
// TODO: Move this to a RimWorld.DefGenerator.GenerateImpliedDefs_PreResolve patch;
// See <https://discord.com/channels/214523379766525963/215496692047413249/1267148367792963677>
[HarmonyPatch(typeof(Zone), nameof(Zone.GetInspectTabs))]
internal static class Zone_GetInspectTabs
{
    private static readonly ITab[] ITabs =
    [
        new ITab_Hysteresis()
    ];

    private static IEnumerable<InspectTabBase> Postfix(IEnumerable<InspectTabBase> values, Zone __instance)
    {
        if (__instance is not Zone_Growing)
        {
            return values;
        }

        if (values == null)
        {
            values = ITabs;
        }
        else
        {
            values = values.Concat(ITabs);
        }

        return values;
    }
}
