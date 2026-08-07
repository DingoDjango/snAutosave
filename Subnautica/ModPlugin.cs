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
        public const string modVersion = "2.4.0";

        private void Awake()
        {
            LanguageHandler.RegisterLocalizationFolder();

            options = OptionsPanelHandler.RegisterModOptions<AutosaveOptions>();

            AutosaveOptions.OnTimingChanged += RescheduleOnSettingChanged;

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
            if (Input.GetKeyDown(options.QuicksaveKey))
            {
                IngameMenu.main?.SaveGame();
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
