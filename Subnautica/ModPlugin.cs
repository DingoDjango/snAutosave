using BepInEx;
using Nautilus.Handlers;
using UnityEngine;

namespace SubnauticaAutosave
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("com.snmodding.nautilus")]
    public class ModPlugin : ModPluginBase
    {
        public const string modGUID = "Dingo.SN.SubnauticaAutosave";
        public const string modName = "Subnautica Autosave";
        public const string modVersion = "3.0.8.3031";

        private void Awake()
        {
            LanguageHandler.RegisterLocalizationFolder();

            options = OptionsPanelHandler.RegisterModOptions<AutosaveOptions>();

            AutosaveOptions.OnTimingChanged += RescheduleOnSettingChanged;
            
            Keybinds.Initialize();
            
            HarmonyPatches.InitializeHarmony();
        }

        private void OnDestroy()
        {
            AutosaveOptions.OnTimingChanged -= RescheduleOnSettingChanged;
        }

        private void RescheduleOnSettingChanged()
        {
            Player.main?.GetComponent<AutosaveController>()?.ScheduleAutosave(settingsChanged: true, showMessage: false);
        }

        private void Update()
        {
            // GameInput.input null before init and during scene transitions
            if (!GameInput.IsInitialized)
            {
                return;
            }

            if (GameInput.GetButtonDown(Keybinds.Quicksave))
            {
                if (SaveLoadManager.main.isSaving)
                {
#if DEBUG
                    LogMessage("Quicksave skipped: save operation in progress");
#endif
                }
                else
                {
                    IngameMenu.main?.SaveGame();
                }
            }

#if DEBUG
            if (Input.GetKeyDown(KeyCode.LeftBracket))
            {
                LogMessage("Pressed [ key, trying to execute autosave");

                Player.main?.GetComponent<AutosaveController>()?.TryExecuteAutosave();
            }
#endif
        }

        public static void LogMessage(string message)
        {
            Debug.Log($"{modName} :: {message}");
        }
    }
}
