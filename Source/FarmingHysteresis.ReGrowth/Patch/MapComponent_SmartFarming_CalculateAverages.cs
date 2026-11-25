using ReGrowthCore;

namespace FarmingHysteresis.ReGrowth.Patch;

/// <summary>
/// Captures the "No petty jobs" sow decision ReGrowth's Smart Farming just computed, and, on
/// zones whose sowing Farming Hysteresis controls, undoes the sow mode and allow-sow changes it
/// made in reaction to Farming Hysteresis's own allow-sow flag - see <see
/// cref="ReGrowthSmartFarmingIntegration"/> and <see cref="SmartFarmingCooperation"/>.
/// </summary>
[HarmonyPatch(
    typeof(MapComponent_SmartFarming),
    nameof(MapComponent_SmartFarming.CalculateAverages)
)]
internal static class MapComponent_SmartFarming_CalculateAverages
{
    internal readonly record struct State(ZoneData.SowMode SowMode, bool AllowSow);

    private static void Prefix(Zone zone, ZoneData zoneData, out State? __state) =>
        __state = zone is Zone_Growing growing ? new(zoneData.sowMode, growing.allowSow) : null;

    private static void Postfix(Zone zone, ZoneData zoneData, State? __state)
    {
        if (__state is not { } before || zone is not Zone_Growing growing)
        {
            return;
        }

        if (zoneData.noPettyJobs && before.SowMode != ZoneData.SowMode.Off)
        {
            ReGrowthSmartFarmingIntegration.Cooperation.CacheAllowSow(growing, growing.allowSow);
        }

        if (!FarmingHysteresisMod.HysteresisController.ControlsSowingOf(growing))
        {
            return;
        }

        (zoneData.sowMode, growing.allowSow) = ReGrowthSmartFarmingIntegration.Reconcile(
            before.SowMode,
            before.AllowSow,
            zoneData.sowMode,
            growing.allowSow,
            zoneData.noPettyJobs
        );
    }
}
