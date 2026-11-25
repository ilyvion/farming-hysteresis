using SmartFarming;

namespace FarmingHysteresis.SmartFarming.Patch;

/// <summary>
/// Captures the sow decision Smart Farming's own petty-jobs calculation just wrote to <see
/// cref="Zone_Growing.allowSow"/>, so <see cref="SmartFarmingCooperation.AllowSow"/> can replay
/// it between Smart Farming's own (much less frequent) recalculations - see <see
/// cref="SmartFarmingCooperation"/> for why this cooperation is needed at all. On zones whose
/// sowing Farming Hysteresis controls, it then puts back Farming Hysteresis's own allow-sow flag
/// (see <see cref="SmartFarmingIntegration.ReconcileAllowSow"/>), which Smart Farming otherwise
/// overwrites with its sow mode's.
/// </summary>
[HarmonyPatch(
    typeof(MapComponent_SmartFarming),
    nameof(MapComponent_SmartFarming.CalculateAverages)
)]
internal static class MapComponent_SmartFarming_CalculateAverages
{
    private static void Prefix(Zone_Growing zone, out bool __state) => __state = zone.allowSow;

    private static void Postfix(Zone_Growing zone, ZoneData zoneData, bool __state)
    {
        SmartFarmingIntegration.Cooperation.CacheAllowSow(zone, zone.allowSow);

        if (!FarmingHysteresisMod.HysteresisController.ControlsSowingOf(zone))
        {
            return;
        }

        zone.allowSow = SmartFarmingIntegration.ReconcileAllowSow(
            zoneData.sowMode,
            __state,
            zone.allowSow,
            zoneData.noPettyJobs
        );
    }
}
