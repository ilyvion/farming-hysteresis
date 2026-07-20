#if v1_5_OR_GREATER
using Verse.Steam;

namespace FarmingHysteresis.Patch;

[HarmonyPatch(
    typeof(VersionUpdateDialogMaker),
    nameof(VersionUpdateDialogMaker.CreateVersionUpdateDialogIfNecessary)
)]
internal static class RimWorld_VersionUpdateDialogMaker_CreateVersionUpdateDialogIfNecessary_ColonyManagerReduxSuggestion
{
    private const uint ColonyManagerReduxPublishedFileId = 3310027356;
    private const string ColonyManagerReduxGitHubUrl =
        "https://github.com/ilyvion/colony-manager-redux";

    private static bool ShownThisTime;

    private static bool IsColonyManagerReduxActive() =>
        ModLister.GetActiveModWithIdentifier("ilyvion.colonymanagerredux", true) != null;

    private static void Postfix()
    {
        if (
            !IsColonyManagerReduxActive()
            && FarmingHysteresisMod.Settings.ShowColonyManagerReduxSuggestion
            && !ShownThisTime
        )
        {
            ShownThisTime = true;

            Find.WindowStack.Add(
                new NewModRequirement(
                    "Farming Hysteresis will continue to work as it always has on its own, but "
                        + "development has shifted toward using it alongside Colony Manager Redux. "
                        + "With Colony Manager Redux, you'll get an improved UI and automation "
                        + "that Farming Hysteresis's older interface won't provide. You don't need "
                        + "to install Colony Manager Redux — everything you already have will keep "
                        + "working exactly as before for as long as I can support it. However, if "
                        + "you want the newest and future features and improvements, the only way "
                        + "to get them is to use Farming Hysteresis together with Colony Manager Redux.",
                    "Don't show this again",
                    () =>
                    {
                        FarmingHysteresisMod.Settings.ShowColonyManagerReduxSuggestion = false;
                        LoadedModManager
                            .GetMod<FarmingHysteresisMod>()
                            .GetSettings<Settings>()
                            .Write();
                    },
                    "Open "
                        + (
                            SteamManager.Initialized ? "Steam Workshop page" : "GitHub release page"
                        ),
                    () =>
                    {
                        if (SteamManager.Initialized)
                        {
                            SteamUtility.OpenWorkshopPage(new(ColonyManagerReduxPublishedFileId));
                        }
                        else
                        {
                            Application.OpenURL(ColonyManagerReduxGitHubUrl);
                        }
                    }
                )
                {
                    buttonCText = "Remind me next time",
                }
            );
        }
    }
}
#endif
