using SmartFarming;

namespace FarmingHysteresis.SmartFarming;

/// <summary>
/// Connects Smart Farming's <see cref="MapComponent_SmartFarming"/> to the shared <see
/// cref="SmartFarmingCooperation"/>.
/// </summary>
internal static class SmartFarmingIntegration
{
    internal static readonly SmartFarmingCooperation Cooperation = new(HasNoPettyJobsEnabled);

    /// <summary>
    /// <see cref="SmartFarmingCooperation.ReconcileAllowSow"/> for Smart Farming's own <see
    /// cref="ZoneData.SowMode"/>.
    /// </summary>
    internal static bool ReconcileAllowSow(
        ZoneData.SowMode sowMode,
        bool allowSowBefore,
        bool allowSowAfter,
        bool noPettyJobs
    ) =>
        SmartFarmingCooperation.ReconcileAllowSow(
            sowMode == ZoneData.SowMode.Off,
            allowSowBefore,
            allowSowAfter,
            noPettyJobs
        );

    private static bool HasNoPettyJobsEnabled(Zone_Growing zone) =>
        zone.Map?.GetComponent<MapComponent_SmartFarming>() is { } comp
        && comp.growZoneRegistry.TryGetValue(zone.ID, out var zoneData)
        && zoneData.noPettyJobs;
}
