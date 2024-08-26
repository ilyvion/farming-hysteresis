using Verse.Steam;

namespace FarmingHysteresis.Patch;

[HarmonyPatch(
    typeof(VersionUpdateDialogMaker),
    nameof(VersionUpdateDialogMaker.CreateVersionUpdateDialogIfNecessary))]
internal static class RimWorld_VersionUpdateDialogMaker_CreateVersionUpdateDialogIfNecessary
{
    private const uint IlyvionLaboratoryPublishedFileId = 3296362231;
    private const string IlyvionLaboratoryGitHubUrl =
        "https://github.com/ilyvion/ilyvion-laboratory/releases/latest";

    static bool ShownThisTime;

    private static bool IsIlyvionLaboratoryActive()
    {
#if v1_3
        return ModLister.GetActiveModWithIdentifier("ilyvion.laboratory") == null;
#else
        return ModLister.GetActiveModWithIdentifier("ilyvion.laboratory", true) == null;
#endif
    }

    private static void Postfix()
    {
        if (IsIlyvionLaboratoryActive() && FarmingHysteresisMod.Settings.ShowIlyvionLaboratoryWarning && !ShownThisTime)
        {
            ShownThisTime = true;

            Find.WindowStack.Add(new NewModRequirement(
                "Farming Hysteresis is going to require you to have ilyvion's Laboratory " +
                "installed and enabled in a future version. In order for you not to suddenly get " +
                "a bunch of errors when launching the game at a future date, I strongly " +
                "recommend you install and enable it now. This message will stop showing up on " +
                "game launch once the mod has been installed and enabled.",
                "Don't show this again",
                () =>
                {
                    FarmingHysteresisMod.Settings.ShowIlyvionLaboratoryWarning = false;
                    LoadedModManager.GetMod<FarmingHysteresisMod>().GetSettings<Settings>().Write();
                },
                "Open " + (SteamManager.Initialized ? "Steam Workshop page" : "GitHub release page"),
                () =>
                {
                    if (SteamManager.Initialized)
                    {
                        SteamUtility.OpenWorkshopPage(new(IlyvionLaboratoryPublishedFileId));
                    }
                    else
                    {
                        Application.OpenURL(IlyvionLaboratoryGitHubUrl);
                    }
                },
                buttonADestructive: true
            )
            {
                buttonCText = "Remind me next time"
            });
        }
    }
}

internal class NewModRequirement : Dialog_MessageBox
{
    public override Vector2 InitialSize => new(640f, 150f);

    internal NewModRequirement(
        string text,
        string? buttonAText = null,
        Action? buttonAAction = null,
        string? buttonBText = null,
        Action? buttonBAction = null,
        string? title = null,
        bool buttonADestructive = false,
        Action? acceptAction = null,
        Action? cancelAction = null)
        : base(
            text,
            buttonAText,
            buttonAAction,
            buttonBText,
            buttonBAction,
            title,
            buttonADestructive,
            acceptAction,
            cancelAction)
    {
        interactionDelay = 5f;
    }
}
