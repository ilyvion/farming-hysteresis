using ReGrowthCore;

namespace FarmingHysteresis.ReGrowth;

/// <summary>
/// Reconciles the Smart Farming copy bundled into ReGrowth 2 with Farming Hysteresis on growing
/// zones whose sowing a hysteresis engine controls. ReGrowth's <see
/// cref="MapComponent_SmartFarming.CalculateAverages"/> rewrites <see
/// cref="Zone_Growing.allowSow"/> every 2500 ticks, and switches a zone's Smart Farming sow mode
/// from <see cref="ZoneData.SowMode.On"/> to <see cref="ZoneData.SowMode.Off"/> whenever it finds
/// sowing disallowed; its sow work-giver patch then refuses to sow an <see
/// cref="ZoneData.SowMode.Off"/> zone at all, so hysteresis could never turn sowing back on (see
/// <see cref="Patch.MapComponent_SmartFarming_CalculateAverages"/>). Also bridges its "No petty
/// jobs" sow decision into <see cref="FarmingHysteresisMod.AllowSowVeto"/>, replaying the value
/// Smart Farming itself last computed, captured before it is reconciled so the veto is never fed
/// back its own AND result.
/// </summary>
internal static class ReGrowthSmartFarmingIntegration
{
#pragma warning disable IDE0028 // Simplify collection initialization
    private static readonly ConditionalWeakTable<Zone_Growing, StrongBox<bool>> cachedAllowSow =
        new();
#pragma warning restore IDE0028 // Simplify collection initialization

    /// <summary>
    /// Records the "No petty jobs" sow decision Smart Farming just computed for <paramref
    /// name="zone"/>, for <see cref="AllowSow"/> to consult on Farming Hysteresis's own, far
    /// more frequent, write cadence.
    /// </summary>
    internal static void CacheAllowSow(Zone_Growing zone, bool allowSow) =>
        cachedAllowSow.GetValue(zone, _ => new StrongBox<bool>()).Value = allowSow;

    /// <summary>
    /// The <see cref="FarmingHysteresisMod.AllowSowVeto"/> implementation: true (no veto) unless
    /// <paramref name="grower"/> is a <see cref="Zone_Growing"/> with Smart Farming's "No petty
    /// jobs" currently toggled on, in which case Smart Farming's own last-computed sow decision
    /// (see <see cref="CacheAllowSow"/>) wins. <c>noPettyJobs</c> is re-checked live on every call
    /// so turning the setting off in Smart Farming's UI stops the veto immediately.
    /// </summary>
    internal static bool AllowSow(IPlantToGrowSettable grower) =>
        grower is not Zone_Growing zone
        || !HasNoPettyJobsEnabled(zone)
        || !cachedAllowSow.TryGetValue(zone, out var box)
        || box.Value;

    /// <summary>
    /// Pure decision logic behind <see cref="Patch.MapComponent_SmartFarming_CalculateAverages"/>
    /// for a zone whose sowing Farming Hysteresis controls: given the zone's sow mode and
    /// allow-sow flag before and after Smart Farming's recalculation, returns the values the zone
    /// should be left with. A switch from <see cref="ZoneData.SowMode.On"/> to <see
    /// cref="ZoneData.SowMode.Off"/> during the recalculation can only be Smart Farming reacting
    /// to Farming Hysteresis's own allow-sow flag, so it is undone. A zone the player set to <see
    /// cref="ZoneData.SowMode.Off"/> keeps Smart Farming's result. Otherwise Farming
    /// Hysteresis's allow-sow flag stands, further restricted by the "No petty jobs" decision
    /// when that is on.
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
        var allowSow =
            sowMode == ZoneData.SowMode.Off ? allowSowAfter
            : noPettyJobs ? allowSowBefore && allowSowAfter
            : allowSowBefore;
        return (sowMode, allowSow);
    }

    private static bool HasNoPettyJobsEnabled(Zone_Growing zone) =>
        zone.Map?.GetComponent<MapComponent_SmartFarming>() is { } comp
        && comp.growZoneRegistry.TryGetValue(zone.ID, out var zoneData)
        && zoneData.noPettyJobs
        && zoneData.sowMode != ZoneData.SowMode.Off;
}
