using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace SubnauticaAutosave
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class ModPlugin : BaseUnityPlugin
    {
        private const string modGUID = "Dingo.SN.SubnauticaAutosave";
        internal const string modName = "Subnautica Autosave";
        private const string modVersion = "2.0.4";

        private const int MaxMinutesBetweenSaves = 9999;
        private const int MaxSaveFiles = 9999;        

        public static bool LoadedFromAutosave = false;

        /* Autosave conditions */
        public static ConfigEntry<bool> ConfigAutosaveOnTimer;
        public static ConfigEntry<bool> ConfigAutosaveOnSleep;
        public static ConfigEntry<int> ConfigMinutesBetweenAutosaves;
        public static ConfigEntry<float> ConfigMinimumPlayerHealthPercent;

        /* General settings */
        public static ConfigEntry<bool> ConfigShowSaveNames;
        public static ConfigEntry<int> ConfigMaxSaveFiles;
        public static ConfigEntry<bool> ConfigHardcoreMode;
        public static ConfigEntry<KeyboardShortcut> ConfigQuicksaveKey;
        public static ConfigEntry<bool> ConfigForceFullSaves;

        private void RescheduleOnSettingChanged()
        {
            if (ConfigAutosaveOnTimer.Value)
            {
#if DEBUG
                LogMessage("RescheduleOnSettingChanged() - trying to reschedule next save.");
#endif

                Player.main?.GetComponent<AutosaveController>()?.ScheduleAutosave(ConfigMinutesBetweenAutosaves.Value, true);
            }
        }

        private void InitializeConfig()
        {
            /* Autosave conditions */
            ConfigAutosaveOnTimer = Config.Bind(
                configDefinition: new ConfigDefinition(section: "Autosave Conditions",
                                                       key: "Autosave Using Time Intervals"),
                defaultValue: true,
                configDescription: new ConfigDescription(description: "Autosave every X minutes as defined under Autosave Conditions."));

            ConfigAutosaveOnTimer.SettingChanged += delegate
            {
                RescheduleOnSettingChanged();
            };

            ConfigAutosaveOnSleep = Config.Bind(
                configDefinition: new ConfigDefinition(section: "Autosave Conditions",
                                                       key: "Autosave on Sleep"),
                defaultValue: true,
                configDescription: new ConfigDescription(description: "Autosave when the player goes to sleep."));

            ConfigMinutesBetweenAutosaves = Config.Bind(
                configDefinition: new ConfigDefinition(section: "Autosave Conditions",
                                                       key: "Minutes Between Autosaves"),
                defaultValue: 15,
                configDescription: new ConfigDescription(description: "Time (in minutes) to wait between autosaves.\nMust be at least 1.",
                                                         acceptableValues: new AcceptableValueRange<int>(1, MaxMinutesBetweenSaves)));

            ConfigMinutesBetweenAutosaves.SettingChanged += delegate
            {
                RescheduleOnSettingChanged();
            };

            ConfigMinimumPlayerHealthPercent = Config.Bind(
                configDefinition: new ConfigDefinition(section: "Autosave Conditions",
                                                       key: "Minimum Player Health Percent"),
                defaultValue: 0.25f,
                configDescription: new ConfigDescription(description: "Delay save if player health is below this percent.\nChange to 0 to disable this option.",
                                                         acceptableValues: new AcceptableValueRange<float>(0f, 1f)));

            /* General settings */
            ConfigShowSaveNames = Config.Bind(
                configDefinition: new ConfigDefinition(section: "General",
                                                       key: "Show Save Names"),
                defaultValue: true,
                configDescription: new ConfigDescription(description: "Show slot names in the main menu loading screen."));

            ConfigMaxSaveFiles = Config.Bind(
                configDefinition: new ConfigDefinition(section: "General",
                                                       key: "Maximum Autosave Slots"),
                defaultValue: 3,
                configDescription: new ConfigDescription(description: "Total autosave slots per playthrough.\nMust be at least 1.",
                                                         acceptableValues: new AcceptableValueRange<int>(1, MaxSaveFiles))); // SaveLoadManager.MaxSlotsAllowed returns 10000 (or 10 for Windows Store version for some reason)

            ConfigHardcoreMode = Config.Bind(
                configDefinition: new ConfigDefinition(section: "General",
                                                       key: "Hardcore Mode"),
                defaultValue: false,
                configDescription: new ConfigDescription(description: "Override the main save instead of using separate autosave slots."));

            ConfigQuicksaveKey = Config.Bind(
                configDefinition: new ConfigDefinition(section: "General",
                                                       key: "Quicksave Hotkey"),
                defaultValue: new KeyboardShortcut(KeyCode.F9),
                configDescription: new ConfigDescription(description: "Keybinding used to save the game manually.\nSame functionality as saving through the ingame menu."));

            ConfigForceFullSaves = Config.Bind(
                configDefinition: new ConfigDefinition(section: "General",
                                                       key: "Force Full Saves"),
                defaultValue: true,
                configDescription: new ConfigDescription(description: "Always save all screenshots, cache and other files.\nMay result in longer save times."));
        }

        internal static void LogMessage(string message)
        {
            Debug.Log($"{modName} :: " + message);
        }

        public void Start()
        {
            InitializeConfig();

            HarmonyPatches.InitializeHarmony();
        }

        public void Update()
        {
            if (GameInput.GetKeyDown(ConfigQuicksaveKey.Value.MainKey))
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
