namespace FarmingHysteresis.ColonyManagerRedux;

/// <summary>
/// Installed in place of <see cref="DefaultHysteresisController"/> whenever the player has
/// enabled "take over Farming Hysteresis control" in <see cref="ManagerSettings_FarmingHysteresis"/>.
/// For now this only suppresses the mod's own UI mod-wide; the actual manager job doesn't
/// control any growers yet (see <see cref="ManagerJob_FarmingHysteresis"/>).
/// </summary>
internal sealed class CmrHysteresisController : IHysteresisController
{
    public static CmrHysteresisController Instance { get; } = new();

    private CmrHysteresisController() { }

    public void Tick(Map map)
    {
        // No job logic yet; growers are simply uncontrolled while takeover is on.
    }

    public bool OwnsGrowerUi(IPlantToGrowSettable plantGrower) => false;

    public bool OwnsMainTab => false;
}
