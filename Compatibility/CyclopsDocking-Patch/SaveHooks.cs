using System;
using System.Reflection;
using CyclopsDockingMod.Fixers;
using HarmonyLib;

namespace SubnauticaAutosave_Compatibility
{
    // Mirrors CyclopsDocking-Continued's IngameMenu save hooks, but routes them to
    // SaveGameAsync (private iterator) so they fire on snAutosave's reflection path too.
    internal static class SaveHooks
    {
        private static MethodInfo _saveRoutes;
        private static FieldInfo _enableAutopilotFeature;

        private static void SaveCyclopsData()
        {
            try
            {
                BaseFixer.SaveBaseParts();
                if (_enableAutopilotFeature != null && (bool)_enableAutopilotFeature.GetValue(null) && _saveRoutes != null)
                {
                    _saveRoutes.Invoke(null, null);
                }
            }
            catch (Exception ex)
            {
                ModPlugin.LogError($"CyclopsDocking save failed: {ex}");
            }
        }

        private static void SaveGameAsync_Postfix()
        {
            SaveCyclopsData();
        }

        private static void QuitGame_Postfix(bool quitToDesktop)
        {
            if (!GameModeUtils.IsPermadeath())
            {
                return;
            }
            SaveCyclopsData();
        }

        internal static void Initialize()
        {
            try
            {
                var asm = typeof(BaseFixer).Assembly;
                var autoPilot = asm.GetType("CyclopsDockingMod.Routing.AutoPilot");
                var configOptions = asm.GetType("CyclopsDockingMod.ConfigOptions");

                if (autoPilot != null)
                {
                    _saveRoutes = autoPilot.GetMethod("SaveRoutes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }

                if (configOptions != null)
                {
                    _enableAutopilotFeature = configOptions.GetField("EnableAutopilotFeature", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }
            }
            catch (Exception ex)
            {
                ModPlugin.LogError($"SaveHooks init failed: {ex}");
            }
        }

        // Remove CyclopsDocking's postfixes from public methods that snAutosave bypasses.
        // Re-attached at SaveGameAsync (every save path) and QuitGame (permadeath).
        internal static void MoveCyclopsSaveHooks(Harmony compat, string cyclopsHarmonyId)
        {
            var saveGame = AccessTools.Method(typeof(IngameMenu), nameof(IngameMenu.SaveGame));
            var quitGame = AccessTools.Method(typeof(IngameMenu), nameof(IngameMenu.QuitGame));

            try
            {
                compat.Unpatch(saveGame, HarmonyPatchType.Postfix, cyclopsHarmonyId);
            }
            catch (Exception ex)
            {
                ModPlugin.LogError($"Unpatch IngameMenu.SaveGame failed: {ex}");
            }

            try
            {
                compat.Unpatch(quitGame, HarmonyPatchType.Postfix, cyclopsHarmonyId);
            }
            catch (Exception ex)
            {
                ModPlugin.LogError($"Unpatch IngameMenu.QuitGame failed: {ex}");
            }
        }

        // Manual patch entrypoints. Per .agents/Style.md harmony patching is manual.
        internal static void ApplyPatches(Harmony harmony)
        {
            var saveGameAsync = AccessTools.Method(typeof(IngameMenu), "SaveGameAsync");
            var quitGame = AccessTools.Method(typeof(IngameMenu), nameof(IngameMenu.QuitGame));

            try
            {
                harmony.Patch(original: saveGameAsync, postfix: new HarmonyMethod(typeof(SaveHooks), nameof(SaveGameAsync_Postfix)));
            }
            catch (Exception ex)
            {
                ModPlugin.LogError($"Patch IngameMenu.SaveGameAsync failed: {ex}");
            }

            try
            {
                harmony.Patch(original: quitGame, postfix: new HarmonyMethod(typeof(SaveHooks), nameof(QuitGame_Postfix)));
            }
            catch (Exception ex)
            {
                ModPlugin.LogError($"Patch IngameMenu.QuitGame failed: {ex}");
            }
        }
    }
}
