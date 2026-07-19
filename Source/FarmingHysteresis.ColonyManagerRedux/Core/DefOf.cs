using ColonyManagerRedux;

namespace FarmingHysteresis.ColonyManagerRedux;

#pragma warning disable CS8618, CA2211

/// <summary>
/// This integration's own history chapter defs. CMR's own
/// <see cref="ManagerJobHistoryChapterDef"/> instances (<c>CM_HistoryStock</c> etc.) live in its
/// core mod's own <c>Common/Defs</c> and could in principle be reused, but its
/// <c>ManagerJobHistoryChapterDefOf</c> lookup class is <c>internal</c> to
/// <c>ColonyManagerRedux.Managers</c>, which this integration never references - so this
/// integration needs its own defs and its own <c>DefOf</c> lookup class, resolved by defName
/// like any other third-party mod would.
/// </summary>
[DefOf]
internal static class ManagerJobHistoryChapterDefOf
{
    public static ManagerJobHistoryChapterDef FH_HistoryStock;
    public static ManagerJobHistoryChapterDef FH_HistoryLower;
    public static ManagerJobHistoryChapterDef FH_HistoryUpper;

    static ManagerJobHistoryChapterDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(ManagerJobHistoryChapterDefOf));
    }
}

/// <summary>
/// This integration's own <see cref="ManagerDef"/> - resolved by defName like any other
/// third-party mod would, same reasoning as <see cref="ManagerJobHistoryChapterDefOf"/>. Used by
/// <see cref="ManagerSettings_FarmingHysteresis.Instance"/> to always look up the current,
/// authoritative settings object rather than caching a reference that can go stale.
/// </summary>
[DefOf]
internal static class ManagerDefOf
{
    public static ManagerDef CM_FarmingHysteresisManager;

    static ManagerDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(ManagerDefOf));
    }
}
