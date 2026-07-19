using FarmingHysteresis.Defs;
using FarmingHysteresis.Extensions;

namespace FarmingHysteresis;

/// <summary>
/// The mod's original, always-on hysteresis engine. Reproduces the pre-extraction behavior
/// verbatim: every controlled plant grower is re-evaluated on every map tick, and the default
/// UI is always shown.
/// </summary>
internal sealed class DefaultHysteresisController : IHysteresisController
{
    public static DefaultHysteresisController Instance { get; } = new();

    private DefaultHysteresisController() { }

    public void Tick(Map map)
    {
        foreach (var plantGrower in FarmingHysteresisControlDef.AllControlledPlantGrowers(map))
        {
            var data = plantGrower.GetFarmingHysteresisData();
            if (data.Enabled)
            {
                data.UpdateLatchModeAndHandling(plantGrower);
            }
        }
    }

    public bool ShowGrowerUi => true;

    public bool ShowMainTab => true;
}
