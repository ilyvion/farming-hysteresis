using System.Reflection;

namespace FarmingHysteresis.VanillaPlantsExpandedMorePlants;

public class VanillaPlantsExpandedMorePlantsMod : Mod
{
    public VanillaPlantsExpandedMorePlantsMod(ModContentPack content)
        : base(content)
    {
        new Harmony(Constants.Id).PatchAll(Assembly.GetExecutingAssembly());

        FarmingHysteresisMod.Instance.LogMessage("\"Vanilla Plants Expanded - More Plants\" interop loaded successfully!");
    }
}
