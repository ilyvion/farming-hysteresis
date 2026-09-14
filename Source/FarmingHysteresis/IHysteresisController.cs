namespace FarmingHysteresis;

/// <summary>
/// Abstracts the responsibility of deciding when to enable/disable sowing and harvesting on
/// controlled plant growers, so it can be swapped between the mod's own always-on engine and
/// an external controller (e.g. a Colony Manager Redux integration). Low-level plumbing
/// (<see cref="Defs.FarmingHysteresisControlDef"/>, <c>SetHysteresisControlState</c>, and the
/// Harmony patches gating actual sow/harvest) is shared by every implementation and is not
/// part of this interface.
/// </summary>
public interface IHysteresisController
{
    /// <summary>
    /// Called periodically from <see cref="FarmingHysteresisMapComponent.MapComponentTick"/> to
    /// re-evaluate <paramref name="map"/>'s controlled plant growers and enable/disable
    /// sowing/harvesting as appropriate.
    /// </summary>
    /// <param name="map">The map to re-evaluate.</param>
    void Tick(Map map);

    /// <summary>
    /// Whether vanilla's sow work-giver should be stopped from cutting down <paramref name="plant"/>,
    /// standing on one of <paramref name="grower"/>'s cells, to clear it for the incoming crop -
    /// queried live (rather than cached) so it stops applying the moment this controller no
    /// longer has a genuine reason to protect the grower, with nothing to go stale. Takes the
    /// specific plant being targeted for cutting, not just the grower, since a grower can have a
    /// leftover rotation crop in one cell while other cells hold plants that were never part of
    /// the rotation and should always be cuttable. Only a Colony Manager Redux crop rotation job
    /// (see <c>ColonyManagerRedux.CmrHysteresisController</c>) ever has a reason to protect
    /// anything.
    /// </summary>
    bool ShouldProtectLeftoverFromCut(IPlantToGrowSettable grower, Plant plant);

    /// <summary>
    /// Whether the default per-grower hysteresis UI (the enable/disable gizmo and
    /// <c>ITab_Hysteresis</c>) should render.
    /// </summary>
    bool ShowGrowerUi { get; }

    /// <summary>
    /// Whether the map/game-tier main tab (<c>MainTabWindow_Hysteresis</c>/
    /// <c>MainButtonWorker_Hysteresis</c>) should be shown at all.
    /// </summary>
    bool ShowMainTab { get; }
}
