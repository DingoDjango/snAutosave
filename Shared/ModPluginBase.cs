using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace SubnauticaAutosave
{
    public abstract class ModPluginBase : BaseUnityPlugin
    {
        public const int MaxMinutesBetweenSaves = 600; // 10 hours should be enough
        public const int MaxSaveFiles = 99;

        /* General mod settings */
        public const string categoryGeneral = "General Settings";
        public static ConfigEntry<bool> ConfigAutosaveOnTimer;
        public static ConfigEntry<bool> ConfigAutosaveOnSleep;
        public static ConfigEntry<int> ConfigMaxSaveFiles;
        public static ConfigEntry<bool> ConfigHardcoreMode;
        public static ConfigEntry<KeyCode> ConfigQuicksaveKey;

        /* Autosave conditions */
        public const string categoryConditions = "Autosave Conditions";
        public static ConfigEntry<int> ConfigMinutesBetweenAutosaves;
        public static ConfigEntry<float> ConfigMinimumPlayerHealthPercent;
        public static ConfigEntry<bool> ConfigDelaySaveOnManual;

        /* Other settings */
        public const string categoryOther = "Other Settings";
        public static ConfigEntry<bool> ConfigShowSaveMessages;
        public static ConfigEntry<bool> ConfigShowSaveNames;
        public static ConfigEntry<bool> ConfigComprehensiveSaves;
        public static ConfigEntry<bool> ConfigUseCustomDateFormat;
        public static ConfigEntry<DateTimeFormat> ConfigCustomDateTimeFormat;

        public abstract void RescheduleOnSettingChanged();

        public virtual void InitializeConfig()
        {
            /* General settings */
            ConfigAutosaveOnTimer = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: categoryGeneral,
                                                       key: "Autosave Using Time Intervals"),
                defaultValue: true,
                configDescription: new ConfigDescription(description: $"Autosave every X minutes as defined under {categoryConditions}.",
                acceptableValues: null, tags: new ConfigurationManagerAttributes { Category = categoryGeneral, Order = 19 }));

            ConfigAutosaveOnSleep = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: categoryGeneral,
                                                       key: "Autosave On Sleep"),
                defaultValue: true,
                configDescription: new ConfigDescription(description: "Autosave when the player goes to sleep.",
                acceptableValues: null, tags: new ConfigurationManagerAttributes { Category = categoryGeneral, Order = 18 }));

            ConfigMaxSaveFiles = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: categoryGeneral,
                                                       key: "Maximum Autosave Slots"),
                defaultValue: 3,
                configDescription: new ConfigDescription(description: "Total autosave slots per playthrough.\nMust be at least 1.",
                                                         acceptableValues: new AcceptableValueRange<int>(1, MaxSaveFiles), // SaveLoadManager.MaxSlotsAllowed returns 10000 (or 10 for Windows Store version for some reason)
                                                         tags: new ConfigurationManagerAttributes { Category = categoryGeneral, Order = 17 }));

            ConfigHardcoreMode = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: categoryGeneral,
                                                       key: "Hardcore Mode"),
                defaultValue: false,
                configDescription: new ConfigDescription(description: "Override the main save instead of using separate autosave slots.",
                                                         acceptableValues: null,
                                                         tags: new ConfigurationManagerAttributes { Category = categoryGeneral, Order = 16 }));

            ConfigQuicksaveKey = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: categoryGeneral,
                                                       key: "Quicksave Hotkey"),
                defaultValue: KeyCode.F9,
                configDescription: new ConfigDescription(description: "Keybinding used to save the game manually.\nSame functionality as saving through the ingame menu.",
                                                         acceptableValues: null,
                                                         tags: new ConfigurationManagerAttributes { Category = categoryGeneral, Order = 15 }));


            /* Autosave conditions */
            ConfigMinutesBetweenAutosaves = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: categoryConditions,
                                                       key: "Minutes Between Autosaves"),
                defaultValue: 15,
                configDescription: new ConfigDescription(description: "Time (in minutes) to wait between autosaves.\nMust be at least 1.",
                                                         acceptableValues: new AcceptableValueRange<int>(1, MaxMinutesBetweenSaves),
                                                         tags: new ConfigurationManagerAttributes { Category = categoryConditions, Order = 19 }));

            ConfigMinimumPlayerHealthPercent = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: categoryConditions,
                                                       key: "Minimum Player Health Percent"),
                defaultValue: 0.25f,
                configDescription: new ConfigDescription(description: "Delay save if player health is below this percent.\nChange to 0 to disable this option.",
                                                         acceptableValues: new AcceptableValueRange<float>(0f, 1f),
                                                         tags: new ConfigurationManagerAttributes { Category = categoryConditions, Order = 18 }));

            ConfigDelaySaveOnManual = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: categoryConditions,
                                                       key: "Manual Save Resets Timer"),
                defaultValue: false,
                configDescription: new ConfigDescription(description: "Reset the next autosave timing when the player performs a manual save.",
                                                         acceptableValues: null,
                                                         tags: new ConfigurationManagerAttributes { Category = categoryConditions, Order = 17 }));


            /* Other settings */
            ConfigShowSaveMessages = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: categoryOther,
                key: "Show AutoSave Messages"),
                defaultValue: true,
                configDescription: new ConfigDescription(description: "Display messages, such as warning about autosaves.",
                                                         acceptableValues: null,
                                                         tags: new ConfigurationManagerAttributes { Category = categoryOther, Order = 19 }));

            ConfigShowSaveNames = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: categoryOther,
                                                       key: "Show Save Names"),
                defaultValue: true,
                configDescription: new ConfigDescription(description: "Show slot names in the main menu loading screen.",
                                                         acceptableValues: null,
                                                         tags: new ConfigurationManagerAttributes { Category = categoryOther, Order = 18 }));

            ConfigComprehensiveSaves = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: categoryOther,
                                                       key: "Save All Files"),
                defaultValue: true,
                configDescription: new ConfigDescription(description: "Always save all screenshots, cache and other files.\nMay result in longer save times.",
                                                         acceptableValues: null,
                                                         tags: new ConfigurationManagerAttributes { Category = categoryOther, Order = 17 }));

            ConfigUseCustomDateFormat = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: categoryOther,
                key: "Use Custom Date Format"),
                defaultValue: false,
                configDescription: new ConfigDescription(description: "Use custom saved game date-time in the main menu.",
                                                         acceptableValues: null,
                                                         tags: new ConfigurationManagerAttributes { Category = categoryOther, Order = 16 }));

            ConfigCustomDateTimeFormat = this.Config.Bind(
                configDefinition: new ConfigDefinition(section: categoryOther,
                key: "Custom Date Format"),
                defaultValue: DateTimeFormat.DMMMYYYY_24Hour,
                configDescription: new ConfigDescription(description: "If enabled, choose how save dates are shown.",
                                                         acceptableValues: null,
                                                         tags: new ConfigurationManagerAttributes { Category = categoryOther, Order = 15 }));
        }

        public abstract void Start();

        public abstract void Update();
    }
}
