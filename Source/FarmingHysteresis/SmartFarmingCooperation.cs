namespace FarmingHysteresis;

/// <summary>
/// The sow cooperation shared by the integrations with Smart Farming and with the copy of it
/// bundled into ReGrowth 2. Smart Farming recomputes <see cref="Zone_Growing.allowSow"/> for
/// zones with "No petty jobs" on only once every 2500 ticks, far less often than either Farming
/// Hysteresis engine reasserts its own decision onto the same field, so <see cref="AllowSow"/>
/// replays the value Smart Farming itself last computed as a <see
/// cref="FarmingHysteresisMod.AllowSowVeto"/>. The value is captured when Smart Farming writes
/// it, so the veto is never fed back its own AND result (which would ratchet sowing permanently
/// off).
/// </summary>
/// <param name="hasNoPettyJobsEnabled">
/// Whether Smart Farming's "No petty jobs" decision currently applies to a zone. Checked live on
/// every <see cref="AllowSow"/> call, so turning the setting off in Smart Farming's UI stops the
/// veto immediately.
/// </param>
internal sealed class SmartFarmingCooperation(Func<Zone_Growing, bool> hasNoPettyJobsEnabled)
{
#pragma warning disable IDE0028 // Simplify collection initialization
    private readonly ConditionalWeakTable<Zone_Growing, StrongBox<bool>> cachedAllowSow = new();
#pragma warning restore IDE0028 // Simplify collection initialization

    /// <summary>
    /// Records the "No petty jobs" sow decision Smart Farming just computed for <paramref
    /// name="zone"/>, for <see cref="AllowSow"/> to consult on Farming Hysteresis's own write
    /// cadence.
    /// </summary>
    internal void CacheAllowSow(Zone_Growing zone, bool allowSow) =>
        cachedAllowSow.GetValue(zone, _ => new StrongBox<bool>()).Value = allowSow;

    /// <summary>
    /// The <see cref="FarmingHysteresisMod.AllowSowVeto"/> implementation: true (no veto) unless
    /// <paramref name="grower"/> is a <see cref="Zone_Growing"/> with "No petty jobs" in effect
    /// and a cached decision, in which case that decision (see <see cref="CacheAllowSow"/>) wins.
    /// </summary>
    internal bool AllowSow(IPlantToGrowSettable grower) =>
        grower is not Zone_Growing zone
        || !hasNoPettyJobsEnabled(zone)
        || !cachedAllowSow.TryGetValue(zone, out var box)
        || box.Value;

    /// <summary>
    /// The allow-sow flag a zone whose sowing Farming Hysteresis controls should be left with,
    /// given the flag before and after Smart Farming's recalculation. A zone whose Smart Farming
    /// sow mode is Off keeps Smart Farming's result. Otherwise Farming Hysteresis's flag stands,
    /// further restricted by the "No petty jobs" decision when that is on.
    /// </summary>
    internal static bool ReconcileAllowSow(
        bool sowModeIsOff,
        bool allowSowBefore,
        bool allowSowAfter,
        bool noPettyJobs
    ) =>
        sowModeIsOff ? allowSowAfter
        : noPettyJobs ? allowSowBefore && allowSowAfter
        : allowSowBefore;
}
