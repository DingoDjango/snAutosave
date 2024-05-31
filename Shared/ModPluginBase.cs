using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace SubnauticaAutosave
{
    public abstract class ModPluginBase : BaseUnityPlugin
    {
        public const int MaxMinutesBetweenSaves = 600; // 10 hours should be enough
        public const int MaxSaveFiles = 99;

        /* Autosave conditions */
        public static ConfigEntry<bool> ConfigAutosaveOnTimer;
        public static ConfigEntry<bool> ConfigAutosaveOnSleep;
        public static ConfigEntry<int> ConfigMinutesBetweenAutosaves;
        public static ConfigEntry<float> ConfigMinimumPlayerHealthPercent;

        /* General settings */
        public static ConfigEntry<bool> ConfigShowSaveNames;
        public static ConfigEntry<int> ConfigMaxSaveFiles;
        public static ConfigEntry<bool> ConfigHardcoreMode;
        public static ConfigEntry<KeyCode> ConfigQuicksaveKey;
        public static ConfigEntry<bool> ConfigComprehensiveSaves;
        public static ConfigEntry<bool> ConfigDelaySaveOnManual;

        public abstract void RescheduleOnSettingChanged();

        public virtual void InitializeConfig()
        {
            /* Autosave conditions */
            ConfigAutosaveOnTimer = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: "Autosave Conditions",
                                                       key: "Autosave Using Time Intervals"),
                defaultValue: true,
                configDescription: new ConfigDescription(description: "Autosave every X minutes as defined under Autosave Conditions."));

            ConfigAutosaveOnSleep = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: "Autosave Conditions",
                                                       key: "Autosave On Sleep"),
                defaultValue: true,
                configDescription: new ConfigDescription(description: "Autosave when the player goes to sleep."));

            ConfigMinutesBetweenAutosaves = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: "Autosave Conditions",
                                                       key: "Minutes Between Autosaves"),
                defaultValue: 15,
                configDescription: new ConfigDescription(description: "Time (in minutes) to wait between autosaves.\nMust be at least 1.",
                                                         acceptableValues: new AcceptableValueRange<int>(1, MaxMinutesBetweenSaves)));

            ConfigMinimumPlayerHealthPercent = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: "Autosave Conditions",
                                                       key: "Minimum Player Health Percent"),
                defaultValue: 0.25f,
                configDescription: new ConfigDescription(description: "Delay save if player health is below this percent.\nChange to 0 to disable this option.",
                                                         acceptableValues: new AcceptableValueRange<float>(0f, 1f)));

            /* General settings */
            ConfigShowSaveNames = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: "General",
                                                       key: "Show Save Names"),
                defaultValue: true,
                configDescription: new ConfigDescription(description: "Show slot names in the main menu loading screen."));

            ConfigMaxSaveFiles = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: "General",
                                                       key: "Maximum Autosave Slots"),
                defaultValue: 3,
                configDescription: new ConfigDescription(description: "Total autosave slots per playthrough.\nMust be at least 1.",
                                                         acceptableValues: new AcceptableValueRange<int>(1, MaxSaveFiles))); // SaveLoadManager.MaxSlotsAllowed returns 10000 (or 10 for Windows Store version for some reason)

            ConfigHardcoreMode = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: "General",
                                                       key: "Hardcore Mode"),
                defaultValue: false,
                configDescription: new ConfigDescription(description: "Override the main save instead of using separate autosave slots."));

            ConfigQuicksaveKey = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: "General",
                                                       key: "Quicksave Hotkey"),
                defaultValue: KeyCode.F9,
                configDescription: new ConfigDescription(description: "Keybinding used to save the game manually.\nSame functionality as saving through the ingame menu."));

            ConfigComprehensiveSaves = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: "General",
                                                       key: "Save All Files"),
                defaultValue: true,
                configDescription: new ConfigDescription(description: "Always save all screenshots, cache and other files.\nMay result in longer save times."));

            ConfigDelaySaveOnManual = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: "General",
                                                       key: "Manual Save Resets Timer"),
                defaultValue: false,
                configDescription: new ConfigDescription(description: "Reset the next autosave timing when the player performs a manual save."));
        }

        public abstract void Start();

        public abstract void Update();
    }
}
