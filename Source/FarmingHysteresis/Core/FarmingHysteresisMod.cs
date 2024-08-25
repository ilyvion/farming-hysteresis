using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FarmingHysteresis.VanillaPlantsExpandedMorePlants")]

namespace FarmingHysteresis;

public class FarmingHysteresisMod : Mod
{
    private readonly ModContentPack content;

    public FarmingHysteresisMod(ModContentPack content) : base(content)
    {
        this.content = content;

        new Harmony(Constants.Id).PatchAll();

        GetSettings<Settings>();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        Settings.DoSettingsWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return content.Name;
    }

    public static void Message(string msg)
    {
        Log.Message("[Farming Hysteresis] " + msg);
    }

    public static void Dev(string msg)
    {
        if (Prefs.DevMode)
        {
            Log.Message("[Farming Hysteresis][DEV] " + msg);
        }
    }

    public static void Warning(string msg)
    {
        Log.Warning("[Farming Hysteresis] " + msg);
    }

    public static void Error(string msg)
    {
        Log.Error("[Farming Hysteresis] " + msg);
    }

    public static void Exception(string msg, Exception? e = null)
    {
        Message(msg);
        if (e != null)
        {
            Log.Error(e.ToString());
        }
    }
}
