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
    private static readonly ITab[] ITabs = [new ITab_Hysteresis()];

    private static IEnumerable<InspectTabBase> Postfix(
        IEnumerable<InspectTabBase> values,
        Zone __instance
    ) => ComputeInspectTabs(__instance is Zone_Growing, values, ITabs)!;

    /// <summary>
    /// Pure decision logic behind the postfix: whether/how to append <paramref
    /// name="extraTabs"/> onto <paramref name="values"/>. Generic over a placeholder type so
    /// it's testable without live <see cref="InspectTabBase"/> instances.
    /// </summary>
    internal static IEnumerable<T>? ComputeInspectTabs<T>(
        bool isControlledZoneType,
        IEnumerable<T>? values,
        IReadOnlyList<T> extraTabs
    ) =>
        !isControlledZoneType ? values
        : values == null ? extraTabs
        : values.Concat(extraTabs);
}
