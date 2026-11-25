using SmartFarming;

namespace FarmingHysteresis.SmartFarming;

/// <summary>
/// Bridges Smart Farming's own "No petty jobs" sow decision into <see
/// cref="FarmingHysteresisMod.AllowSowVeto"/>. Smart Farming's <see
/// cref="MapComponent_SmartFarming"/> recomputes <see cref="Zone_Growing.allowSow"/> for
/// petty-jobs-enabled zones only once every 2500 ticks (see
/// <see cref="Patch.MapComponent_SmartFarming_CalculateAverages"/>), far less often than either
/// Farming Hysteresis engine reasserts its own decision onto the same field - without this
/// integration, Smart Farming's setting never sticks. The veto never recomputes Smart Farming's
/// own math; it only ever replays the value Smart Farming itself last wrote, captured at write
/// time so the veto can't be fed back its own AND result on a later call (which would ratchet
/// sowing permanently off).
/// </summary>
internal static class SmartFarmingIntegration
{
#pragma warning disable IDE0028 // Simplify collection initialization
    private static readonly ConditionalWeakTable<Zone_Growing, StrongBox<bool>> cachedAllowSow =
        new();
#pragma warning restore IDE0028 // Simplify collection initialization

    /// <summary>
    /// Records the value Smart Farming's <see
    /// cref="MapComponent_SmartFarming"/> just wrote to <paramref name="zone"/>'s <see
    /// cref="Zone_Growing.allowSow"/>, for <see cref="AllowSow"/> to consult on Farming
    /// Hysteresis's own, far more frequent, write cadence.
    /// </summary>
    internal static void CacheAllowSow(Zone_Growing zone, bool allowSow) =>
        cachedAllowSow.GetValue(zone, _ => new StrongBox<bool>()).Value = allowSow;

    /// <summary>
    /// The <see cref="FarmingHysteresisMod.AllowSowVeto"/> implementation: true (no veto) unless
    /// <paramref name="grower"/> is a <see cref="Zone_Growing"/> with Smart Farming's "No petty
    /// jobs" currently toggled on, in which case Smart Farming's own last-computed sow decision
    /// (see <see cref="CacheAllowSow"/>) wins. <c>noPettyJobs</c> is re-checked live on every call
    /// (never cached) so turning the setting off in Smart Farming's UI stops the veto immediately.
    /// </summary>
    internal static bool AllowSow(IPlantToGrowSettable grower) =>
        grower is not Zone_Growing zone
        || !HasNoPettyJobsEnabled(zone)
        || !cachedAllowSow.TryGetValue(zone, out var box)
        || box.Value;

    /// <summary>
    /// Pure decision logic behind <see cref="Patch.MapComponent_SmartFarming_CalculateAverages"/>
    /// for a zone whose sowing Farming Hysteresis controls: given the zone's allow-sow flag before
    /// and after Smart Farming's recalculation, returns the value the zone should be left with. A
    /// zone the player set to <see cref="ZoneData.SowMode.Off"/> keeps Smart Farming's result.
    /// Otherwise Farming Hysteresis's allow-sow flag stands, further restricted by the "No petty
    /// jobs" decision when that is on.
    /// </summary>
    internal static bool ReconcileAllowSow(
        ZoneData.SowMode sowMode,
        bool allowSowBefore,
        bool allowSowAfter,
        bool noPettyJobs
    ) =>
        sowMode == ZoneData.SowMode.Off ? allowSowAfter
        : noPettyJobs ? allowSowBefore && allowSowAfter
        : allowSowBefore;

    private static bool HasNoPettyJobsEnabled(Zone_Growing zone) =>
        zone.Map?.GetComponent<MapComponent_SmartFarming>() is { } comp
        && comp.growZoneRegistry.TryGetValue(zone.ID, out var zoneData)
        && zoneData.noPettyJobs;
}
