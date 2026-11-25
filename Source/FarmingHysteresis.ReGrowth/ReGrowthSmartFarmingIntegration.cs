using ReGrowthCore;

namespace FarmingHysteresis.ReGrowth;

/// <summary>
/// Connects the Smart Farming copy bundled into ReGrowth 2 to the shared <see
/// cref="SmartFarmingCooperation"/>. ReGrowth's <see
/// cref="MapComponent_SmartFarming.CalculateAverages"/> also switches a zone's Smart Farming sow
/// mode from <see cref="ZoneData.SowMode.On"/> to <see cref="ZoneData.SowMode.Off"/> whenever it
/// finds sowing disallowed, and its sow work-giver patch then refuses to sow an <see
/// cref="ZoneData.SowMode.Off"/> zone at all, so hysteresis could never turn sowing back on (see
/// <see cref="Patch.MapComponent_SmartFarming_CalculateAverages"/>).
/// </summary>
internal static class ReGrowthSmartFarmingIntegration
{
    internal static readonly SmartFarmingCooperation Cooperation = new(HasNoPettyJobsEnabled);

    /// <summary>
    /// Pure decision logic behind <see cref="Patch.MapComponent_SmartFarming_CalculateAverages"/>
    /// for a zone whose sowing Farming Hysteresis controls: given the zone's sow mode and
    /// allow-sow flag before and after Smart Farming's recalculation, returns the values the zone
    /// should be left with. A switch from <see cref="ZoneData.SowMode.On"/> to <see
    /// cref="ZoneData.SowMode.Off"/> during the recalculation can only be Smart Farming reacting
    /// to Farming Hysteresis's own allow-sow flag, so it is undone; the allow-sow flag then
    /// follows <see cref="SmartFarmingCooperation.ReconcileAllowSow"/>.
    /// </summary>
    internal static (ZoneData.SowMode SowMode, bool AllowSow) Reconcile(
        ZoneData.SowMode sowModeBefore,
        bool allowSowBefore,
        ZoneData.SowMode sowModeAfter,
        bool allowSowAfter,
        bool noPettyJobs
    )
    {
        var sowMode =
            sowModeBefore == ZoneData.SowMode.On && sowModeAfter == ZoneData.SowMode.Off
                ? ZoneData.SowMode.On
                : sowModeAfter;
        var allowSow = SmartFarmingCooperation.ReconcileAllowSow(
            sowMode == ZoneData.SowMode.Off,
            allowSowBefore,
            allowSowAfter,
            noPettyJobs
        );
        return (sowMode, allowSow);
    }

    private static bool HasNoPettyJobsEnabled(Zone_Growing zone) =>
        zone.Map?.GetComponent<MapComponent_SmartFarming>() is { } comp
        && comp.growZoneRegistry.TryGetValue(zone.ID, out var zoneData)
        && zoneData.noPettyJobs
        && zoneData.sowMode != ZoneData.SowMode.Off;
}
