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
        private const string modVersion = "2.0.0";

        internal static string savedGamesPath = "";

        public static bool LoadedFromAutosave = false;


        // General settings
        public static ConfigEntry<int> ConfigMaxSaveFiles;
        public static ConfigEntry<bool> ConfigAutosaveOnTimer;
        public static ConfigEntry<bool> ConfigAutosaveOnSleep;
        public static ConfigEntry<bool> ConfigHardcoreMode;
        public static ConfigEntry<KeyboardShortcut> ConfigQuicksaveKey;

        // Autosave conditions
        public static ConfigEntry<int> ConfigSecondsBetweenAutosaves;
        public static ConfigEntry<float> ConfigMinimumPlayerHealthPercent;

        private void InitializeConfig()
        {
            /*** // TODO: IMPLEMENT NEW SETTINGS, HOTKEYS, BOOLS ETC. AND TEST // ***/

            ConfigMaxSaveFiles = Config.Bind(
                configDefinition: new ConfigDefinition(section: "General",
                                                       key: "Maximum Autosave Slots"),
                defaultValue: 3,
                configDescription: new ConfigDescription(description: "Total autosave slots. Must be at least 1.",
                                                         acceptableValues: new AcceptableValueRange<int>(1, 9999)));

            ConfigAutosaveOnTimer = Config.Bind(
                configDefinition: new ConfigDefinition(section: "General",
                                                       key: "Autosave Using Time Intervals"),
                defaultValue: true,
                configDescription: new ConfigDescription(description: "Autosave every X seconds as defined in the mod settings."));

            ConfigAutosaveOnSleep = Config.Bind(
                configDefinition: new ConfigDefinition(section: "General",
                                                       key: "Autosave On Sleep"),
                defaultValue: true,
                configDescription: new ConfigDescription(description: "Autosave when the player goes to sleep."));

            ConfigHardcoreMode = Config.Bind(
                configDefinition: new ConfigDefinition(section: "General",
                                                       key: "Hardcore Mode"),
                defaultValue: false,
                configDescription: new ConfigDescription(description: "If true, autosaves will override the normal save slot instead of using separate slots."));

            ConfigQuicksaveKey = Config.Bind(
                configDefinition: new ConfigDefinition(section: "General",
                                                       key: "Quicksave Hotkey"),
                defaultValue: new KeyboardShortcut(KeyCode.F9),                     ///// CHECK IF THIS HOTKEY IS ALREADY TAKEN /////                          
                configDescription: new ConfigDescription(description: "Keybinding used to quickly save the game manually."));

            ConfigSecondsBetweenAutosaves = Config.Bind(
                configDefinition: new ConfigDefinition(section: "Autosave Conditions",
                                                       key: "Seconds Between Autosaves"),
                defaultValue: 900,
                configDescription: new ConfigDescription(description: "Time to wait between autosave attempts. Must be at least 120.",
                                                         acceptableValues: new AcceptableValueRange<int>(120, int.MaxValue)));

            ConfigMinimumPlayerHealthPercent = Config.Bind(
                configDefinition: new ConfigDefinition(section: "Autosave Conditions",
                                                       key: "Minimum Player Health Percent"),
                defaultValue: 0.25f,
                configDescription: new ConfigDescription(description: "Autosaves will not occur if player health is below this percent. Change to 0 to disable this option.",
                                                         acceptableValues: new AcceptableValueRange<float>(0f, 1f)));
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
