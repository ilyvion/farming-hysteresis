using FarmingHysteresis.Defs;
using VanillaPlantsExpandedMorePlants;

namespace FarmingHysteresis.VanillaPlantsExpandedMorePlants.Defs;

internal class FarmingHysteresisControlWorker_Zone_GrowingAquatic : FarmingHysteresisControlWorker
{
    public override IEnumerable<IPlantToGrowSettable> GetControlledPlantGrowers(Map map) =>
        map.zoneManager.AllZones.OfType<Zone_GrowingAquatic>();

    public override bool HandleAllowSow => true;

    public override bool GetAllowSow(IPlantToGrowSettable plantGrower)
    {
        return ((Zone_GrowingAquatic)plantGrower).allowSow;
    }

    public override void SetAllowSow(IPlantToGrowSettable plantGrower, bool value)
    {
        ((Zone_GrowingAquatic)plantGrower).allowSow = value;
    }
}

internal class FarmingHysteresisControlWorker_Zone_GrowingSandy : FarmingHysteresisControlWorker
{
    public override IEnumerable<IPlantToGrowSettable> GetControlledPlantGrowers(Map map) =>
        map.zoneManager.AllZones.OfType<Zone_GrowingSandy>();

    public override bool HandleAllowSow => true;

    public override bool GetAllowSow(IPlantToGrowSettable plantGrower)
    {
        return ((Zone_GrowingSandy)plantGrower).allowSow;
    }

    public override void SetAllowSow(IPlantToGrowSettable plantGrower, bool value)
    {
        ((Zone_GrowingSandy)plantGrower).allowSow = value;
    }
}
