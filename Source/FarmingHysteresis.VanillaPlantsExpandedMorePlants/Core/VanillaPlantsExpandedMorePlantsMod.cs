namespace FarmingHysteresis.VanillaPlantsExpandedMorePlants;

/// <summary>
/// The interop mod class enabling Farming Hysteresis support for Vanilla Plants Expanded -
/// More Plants.
/// </summary>
public class VanillaPlantsExpandedMorePlantsMod : Mod
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VanillaPlantsExpandedMorePlantsMod"/> class.
    /// </summary>
    /// <param name="content">The mod content pack.</param>
    public VanillaPlantsExpandedMorePlantsMod(ModContentPack content)
        : base(content)
    {
        new Harmony(Constants.Id).PatchAll(Assembly.GetExecutingAssembly());

        FarmingHysteresisMod.Instance.LogMessage(
            "\"Vanilla Plants Expanded - More Plants\" interop loaded successfully!"
        );
    }
}
