using FarmingHysteresis.Defs;
using FarmingHysteresis.Extensions;

namespace FarmingHysteresis.ColonyManagerRedux;

/// <summary>
/// One-time migration gate reconciling CMR takeover defaulting to on with saves that already
/// have old-style (per-grower) hysteresis configured, so the new default can't silently strand
/// an existing setup. Fires once per save from
/// <see cref="CmrMigrationGameComponent.FinalizeInit"/>; the player can also retrigger
/// <see cref="Migrate"/> later via the manager tab's migrate button if they declined (see
/// <c>ManagerTab_FarmingHysteresis</c>).
/// </summary>
internal static class CmrMigrationGate
{
    /// <summary>
    /// Pure decision behind <see cref="HandleGameLoaded"/>: whether this save needs a fresh
    /// migration gate opened - only when nothing's been decided for it yet, takeover is actually
    /// on, and there's old-style setup that would otherwise get silently stranded. Split out so
    /// it's unit-testable without a live <see cref="Game"/>/<see cref="Find.Maps"/>.
    /// </summary>
    internal static bool ShouldBeginMigrationGate(
        CmrMigrationGateStatus currentStatus,
        bool takeoverHysteresisControl,
        bool hasLegacyHysteresisDataConfigured
    ) =>
        currentStatus == CmrMigrationGateStatus.NotGated
        && takeoverHysteresisControl
        && hasLegacyHysteresisDataConfigured;

    /// <summary>
    /// <see cref="FarmingHysteresisMod.HysteresisController"/> is a static field that otherwise
    /// only gets refreshed when the mod settings themselves are (re-)scribed (process start, or
    /// the settings tab being written) - neither of which happens when the player merely returns
    /// to the main menu and loads a different (or the same) save within the same running
    /// process. Without unconditionally re-syncing it here, it keeps whatever value the *previous*
    /// save session left it at, regardless of this save's own <see cref="CmrMigrationGameComponent"/>
    /// status or the current global takeover setting - e.g. a save with nothing to migrate (so
    /// <see cref="ShouldBeginMigrationGate"/> is false) previously left the controller on
    /// <see cref="CmrHysteresisController"/> would stay stuck on it forever, even after the player
    /// turns "take over Farming Hysteresis control" off, until they happened to toggle the mod
    /// setting again (which calls <see cref="ManagerSettings_FarmingHysteresis.ApplyControllerState"/>
    /// as a side effect of the toggle itself).
    /// </summary>
    internal static void HandleGameLoaded()
    {
        var gate = CmrMigrationGameComponent.For(Current.Game);

        if (
            ShouldBeginMigrationGate(
                gate.Status,
                ManagerSettings_FarmingHysteresis.Instance?.TakeOverHysteresisControl == true,
                HasLegacyHysteresisDataConfigured()
            )
        )
        {
            gate.Status = CmrMigrationGateStatus.AwaitingChoice;
            LongEventHandler.ExecuteWhenFinished(ShowMigrationDialog);
        }

        ManagerSettings_FarmingHysteresis.Instance?.ApplyControllerState();
    }

    private static bool HasLegacyHysteresisDataConfigured() =>
        Find.Maps.Any(map =>
            FarmingHysteresisControlDef
                .AllControlledPlantGrowers(map)
                .Any(grower => grower.GetFarmingHysteresisData().Enabled)
        );

    internal static void ShowMigrationDialog() =>
        Find.WindowStack.Add(
            new Dialog_MessageBox(
                "FarmingHysteresis.CMR.Migration.Prompt".Translate(),
                "FarmingHysteresis.CMR.Migration.Migrate".Translate(),
                Migrate,
                "FarmingHysteresis.CMR.Migration.Decline".Translate(),
                Decline,
                "FarmingHysteresis.CMR.Migration.Title".Translate()
            )
        );

    /// <summary>
    /// Creates CMR jobs from every currently-enabled grower on every map (see
    /// <see cref="HysteresisMigration.MigrateMap"/>), then marks this save as migrated so
    /// takeover follows the global setting normally from now on. Called both from the initial
    /// dialog's "Migrate" button and from the manager tab's on-demand migrate button for saves
    /// that declined earlier.
    /// </summary>
    internal static void Migrate()
    {
        foreach (var map in Find.Maps)
        {
            HysteresisMigration.MigrateMap(map);
        }

        CmrMigrationGameComponent.For(Current.Game).Status = CmrMigrationGateStatus.Migrated;
        ManagerSettings_FarmingHysteresis.Instance?.ApplyControllerState();

        Messages.Message(
            "FarmingHysteresis.CMR.Migration.MigratedMessage".Translate(),
            MessageTypeDefOf.PositiveEvent,
            historical: false
        );
    }

    private static void Decline()
    {
        CmrMigrationGameComponent.For(Current.Game).Status = CmrMigrationGateStatus.Declined;
        ManagerSettings_FarmingHysteresis.Instance?.ApplyControllerState();

        Messages.Message(
            "FarmingHysteresis.CMR.Migration.DeclinedMessage".Translate(),
            MessageTypeDefOf.NeutralEvent,
            historical: false
        );
    }
}
