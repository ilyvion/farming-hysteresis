namespace FarmingHysteresis.ColonyManagerRedux;

/// <summary>
/// Where a save stands on the one-time migration gate that decides whether Colony Manager Redux
/// takes over Farming Hysteresis control for it.
/// </summary>
internal enum CmrMigrationGateStatus
{
    /// <summary>No gate was ever needed for this save - either takeover was off at load time, or
    /// there was no old-style setup to strand.</summary>
    NotGated,

    /// <summary>The gate fired and the one-time dialog is pending/was shown but not yet answered
    /// (e.g. the game was saved and reloaded before the player responded).</summary>
    AwaitingChoice,

    /// <summary>The player declined migrating. Takeover stays suppressed for this save until
    /// they manually trigger it later (see <c>ManagerTab_FarmingHysteresis</c>'s migrate button).</summary>
    Declined,

    /// <summary>The player migrated (via the dialog or the manual button). Takeover now follows
    /// the global "take over Farming Hysteresis control" setting normally.</summary>
    Migrated,
}

/// <summary>
/// Per-save state backing <see cref="CmrMigrationGate"/>. Deliberately lives in this integration
/// assembly rather than core <c>FarmingHysteresis</c> - RimWorld auto-instantiates every loaded
/// <see cref="GameComponent"/> subclass regardless of assembly, so this simply never exists for a
/// save loaded without the CMR integration active, the same "IfModActive" boundary the rest of
/// this integration already relies on.
/// </summary>
#pragma warning disable CS9113 // Parameter is unread.
internal sealed class CmrMigrationGameComponent(Game game) : GameComponent
#pragma warning restore CS9113
{
    public CmrMigrationGateStatus Status = CmrMigrationGateStatus.NotGated;

    /// <summary>
    /// Pure predicate behind <see cref="IsSuppressingTakeover"/>, split out so the rule (which
    /// statuses count as "still gated") is unit-testable without a live <see cref="Game"/>.
    /// </summary>
    internal static bool IsSuppressing(CmrMigrationGateStatus status) =>
        status is CmrMigrationGateStatus.AwaitingChoice or CmrMigrationGateStatus.Declined;

    /// <summary>
    /// While true, CMR takeover is suppressed for this save regardless of the global "take over
    /// Farming Hysteresis control" setting.
    /// </summary>
    public bool IsSuppressingTakeover => IsSuppressing(Status);

    public static CmrMigrationGameComponent For(Game game)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        var instance = game.GetComponent<CmrMigrationGameComponent>();
        if (instance != null)
        {
            return instance;
        }

        instance = new CmrMigrationGameComponent(game);
        game.components.Add(instance);
        return instance;
    }

    /// <summary>
    /// Whether the currently loaded game's gate is suppressing takeover - <c>false</c> when no
    /// game is loaded (e.g. the main menu), matching how <see cref="ManagerSettings_FarmingHysteresis"/>
    /// behaves before any save exists.
    /// </summary>
    internal static bool IsCurrentSaveSuppressingTakeover() =>
        Current.Game != null && For(Current.Game).IsSuppressingTakeover;

    internal static CmrMigrationGateStatus CurrentStatus =>
        Current.Game == null ? CmrMigrationGateStatus.NotGated : For(Current.Game).Status;

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        CmrMigrationGate.HandleGameLoaded();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref Status, "status", CmrMigrationGateStatus.NotGated);
    }
}
