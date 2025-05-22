using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace SubnauticaAutosave
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class ModPlugin : ModPluginBase
    {
        public const string modGUID = "Dingo.SN.SubnauticaAutosave";
        public const string modName = "Subnautica Autosave";
        public const string modVersion = "2.2.0";

        public override void RescheduleOnSettingChanged()
        {
#if DEBUG
                LogMessage("RescheduleOnSettingChanged() - trying to reschedule next save.");
#endif

                Player.main?.GetComponent<AutosaveController>()?.ScheduleAutosave(settingsChanged: true, showMessage: false);
        }

        public override void InitializeConfig()
        {
            base.InitializeConfig();

            ConfigAutosaveOnTimer.SettingChanged += delegate
            {
                this.RescheduleOnSettingChanged();
            };

            ConfigMinutesBetweenAutosaves.SettingChanged += delegate
            {
                this.RescheduleOnSettingChanged();
            };
        }

        public static void LogMessage(string message)
        {
            Debug.Log($"{modName} :: {message}");
        }

        public override void Start()
        {
            this.InitializeConfig();

            HarmonyPatches.InitializeHarmony();
        }

        public override void Update()
        {
            if (GameInput.GetKeyDown(ConfigQuicksaveKey.Value))
            {
                IngameMenu.main?.SaveGame();
            }

#if DEBUG
            if (GameInput.GetKeyDown(KeyCode.LeftBracket))
            {
                LogMessage("Pressed [ key, trying to execute autosave");

                Player.main?.GetComponent<AutosaveController>()?.TryExecuteAutosave();
            }
#endif
        }
    }
}
