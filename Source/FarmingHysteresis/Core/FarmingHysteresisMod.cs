using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FarmingHysteresis.VanillaPlantsExpandedMorePlants")]

namespace FarmingHysteresis;

public class FarmingHysteresisMod : IlyvionMod
{
#pragma warning disable CS8618 // Set by constructor
    private static FarmingHysteresisMod _instance;
    public static FarmingHysteresisMod Instance
    {
        get => _instance;
        private set => _instance = value;
    }
#pragma warning restore CS8618

    public FarmingHysteresisMod(ModContentPack content) : base(content)
    {
        // This is kind of stupid, but also kind of correct. Correct wins.
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        Instance = this;

        // apply fixes
        var harmony = new Harmony(content.PackageId);
        //Harmony.DEBUG = true;
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        //Harmony.DEBUG = false;

        LongEventHandler.ExecuteWhenFinished(() =>
        {
            // We need to load settings here at the latest because if we end up waiting until during
            //  a game load, it leads to the ScribeLoader exception
            // "Called InitLoading() but current mode is LoadingVars"
            // because you can't Scribe multiple things at once.
            _ = Settings;
        });
    }

    protected override bool HasSettings => true;
    public static Settings Settings => Instance.GetSettings<Settings>();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Settings.DoSettingsWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return Content.Name;
    }
}
