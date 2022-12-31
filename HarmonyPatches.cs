using System;
using HarmonyLib;
using HarmonyLib.Tools;

namespace SubnauticaAutosave
{
    public static class HarmonyPatches
    {
        public static string SavedGamesPath = "";

        private static void Patch_UserStoragePC_Postfix(string _savePath)
        {
            ModPlugin.savedGamesPath = _savePath;
        }

        private static bool Patch_ManualSaveGame_Prefix()
        {
            string currentSlot = SaveLoadManager.main.GetCurrentSlot();

            if (currentSlot.Contains("autosave"))
            {
                string mainSaveSlot = currentSlot.Split('_')[0]; /* Input: slot0000_autosave0001 // Output: slot0000 */

                SaveLoadManager.main.SetCurrentSlot(mainSaveSlot);
            }

            return true;
        }

        private static void Patch_Player_Awake_Postfix(Player __instance)
        {
            __instance.gameObject.AddComponent<AutosaveController>();
        }

        private static void Patch_Subroot_PlayerEnteredOrExited_Postfix()
        {
#if DEBUG
            ModPlugin.LogMessage("Player entered or exited sub. Delaying autosave.");
#endif

            Player.main.GetComponent<AutosaveController>()?.DelayAutosave();
        }

        private static void Patch_SetCurrentLanguage_Postfix()
        {
            Translation.ReloadLanguage();
        }

        internal static void InitializeHarmony()
        {
            Harmony harmony = new Harmony("Dingo.Harmony.SubnauticaAutosave");

            /* Patch IngameMenu.SetActive to not do anything if autosave bool is true??			/// TODO IF SOME MENU STUFF ARISES
			 * (dirty hack and then I can just use the game's save mechanism while only changing slots?) */

            // Set saved games folder path on UserStorage initialization (Steam, Epic, etc.)
            harmony.Patch(original: AccessTools.Constructor(typeof(UserStoragePC), new Type[] { typeof(string) }),
                          postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_UserStoragePC_Postfix)));

            // Correct manual save slot if loaded from autosave
            harmony.Patch(original: AccessTools.Method(typeof(IngameMenu), nameof(IngameMenu.SaveGame)),
                          prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_ManualSaveGame_Prefix)));

            // Autosave injection
            harmony.Patch(original: AccessTools.Method(typeof(Player), nameof(Player.Awake)),
                          postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_Player_Awake_Postfix)));

            // Delay autosave if player has entered or exited a base or vehicle
            HarmonyMethod delayAutosave = new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_Subroot_PlayerEnteredOrExited_Postfix));

            harmony.Patch(original: AccessTools.Method(typeof(SubRoot), nameof(SubRoot.OnPlayerEntered)),
                          postfix: delayAutosave);

            harmony.Patch(original: AccessTools.Method(typeof(SubRoot), nameof(SubRoot.OnPlayerExited)),
                          postfix: delayAutosave);

            // Reset language cache upon language change
            harmony.Patch(original: AccessTools.Method(typeof(Language), nameof(Language.SetCurrentLanguage)),
                          postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_SetCurrentLanguage_Postfix)));
        }
    }
}
