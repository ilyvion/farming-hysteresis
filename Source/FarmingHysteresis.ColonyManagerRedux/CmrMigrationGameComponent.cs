namespace FarmingHysteresis.ColonyManagerRedux;

/// <summary>
/// Where a save stands on the one-time migration gate that decides whether Colony Manager Redux
/// takes over Farming Hysteresis control for it.
/// </summary>
internal enum CmrMigrationGateStatus
{
    /// <summary>
    /// This save has never been through the migration gate - it's an old save that predates
    /// this per-save status tracking entirely.
    /// </summary>
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
    /// <summary>
    /// Deliberately defaults to whatever <see cref="ComputeDefaultStatus"/> resolves to
    /// for a freshly constructed component.
    /// <see cref="ExposeData"/> uses a different default - <see cref="CmrMigrationGateStatus.NotGated"/>
    /// - for the "node missing from this save's XML" case, which asymmetry is exactly the point:
    /// because the two defaults differ, any status other than <see cref="CmrMigrationGateStatus.NotGated"/>
    /// gets written to disk explicitly (Scribe skips writing a value that matches its own default),
    /// so once a save has ever resolved this gate - migrated, declined, or simply started fresh
    /// under this scheme - that decision sticks forever. <see cref="CmrMigrationGateStatus.NotGated"/>
    /// is only ever seen for a save that predates this component's status tracking
    /// altogether (nothing was ever scribed for it to load), which is exactly the case
    /// <see cref="CmrMigrationGate"/> needs to catch.
    /// </summary>
    public CmrMigrationGateStatus Status = ComputeDefaultStatus(
        ManagerSettings_FarmingHysteresis.Instance?.TakeOverHysteresisControl == true
    );

    /// <summary>
    /// Pure predicate behind <see cref="Status"/>'s field initializer, split out so the rule is
    /// unit-testable without a live <see cref="ManagerSettings_FarmingHysteresis"/> instance. A
    /// brand new save with takeover already on has nothing to migrate from, so it starts already
    /// <see cref="CmrMigrationGateStatus.Migrated"/>; one that starts with takeover off is
    /// actually running the legacy per-grower engine, so it starts
    /// <see cref="CmrMigrationGateStatus.Declined"/> - takeover stays suppressed until the player
    /// explicitly migrates later, exactly as if they'd been asked and declined.
    /// </summary>
    internal static CmrMigrationGateStatus ComputeDefaultStatus(bool takeoverHysteresisControl) =>
        takeoverHysteresisControl
            ? CmrMigrationGateStatus.Migrated
            : CmrMigrationGateStatus.Declined;

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
