using SMLHelper.V2.Json;
using SMLHelper.V2.Options.Attributes;
using UnityEngine;

namespace SubnauticaAutosave_BZ
{
    [Menu("Subnautica Autosave BZ")]
    public class SMLConfig : ConfigFile
    {
        private const int _MaxMinutesBetweenSaves = 300; // SML v2.14 menu bugs out if max values for sliders are too high...
        private const int _MaxSaveFiles = 50;

        /* Autosave conditions */
        [Toggle(Id = "AutosaveOnTimer", Label ="Autosave Using Time Intervals", Tooltip = "Autosave every X minutes as defined under Autosave Conditions."), OnChange(nameof(RescheduleOnSettingChanged))]
        public bool ConfigAutosaveOnTimer = true;
        [Toggle(Id = "AutosaveOnSleep", Label = "Autosave On Sleep", Tooltip = "Autosave when the player goes to sleep.")]
        public bool ConfigAutosaveOnSleep = true;
        [Slider(DefaultValue = 20, Format = "{0:F0}", Id = "MinutesBetweenAutosaves", Label = "Minutes Between Autosaves", Max = _MaxMinutesBetweenSaves, Min = 5, Step = 5, Tooltip = "Time (in minutes) to wait between autosaves.\nMust be at least 1."), OnChange(nameof(RescheduleOnSettingChanged))]
        public int ConfigMinutesBetweenAutosaves = 20;
        [Slider(DefaultValue = 25, Format = "{0:F0}%", Id = "MinimumPlayerHealthPercent", Label = "Minimum Player Health Percent", Max = 100, Min = 0, Step = 1, Tooltip = "Delay save if player health is below this percent.\nChange to 0 to disable this option.")]
        public int ConfigMinimumPlayerHealthPercent = 25;

        /* General settings */
        [Toggle(Id = "ShowSaveNames", Label = "Show Save Names", Tooltip = "Show slot names in the main menu loading screen.")]
        public bool ConfigShowSaveNames = true;
        [Slider(DefaultValue = 5, Format = "{0:F0}", Id = "MaxSaveFiles", Label = "Maximum Autosave Slots", Max = _MaxSaveFiles, Min = 1, Step = 1, Tooltip = "Total autosave slots per playthrough.\nMust be at least 1.")]
        public int ConfigMaxSaveFiles = 5;
        [Toggle(Id = "HardcoreMode", Label = "Hardcore Mode", Tooltip = "Override the main save instead of using separate autosave slots.")]
        public bool ConfigHardcoreMode = false;
        [Keybind(Id = "QuicksaveKey", Label = "Quicksave Hotkey", Tooltip = "Keybinding used to save the game manually.\nSame functionality as saving through the ingame menu.")]
        public KeyCode ConfigQuicksaveKey = KeyCode.F9;
        [Toggle(Id = "ComprehensiveSaves", Label = "Save All Files", Tooltip = "Always save all screenshots, cache and other files.\nMay result in longer save times.")]
        public bool ConfigComprehensiveSaves = true;

        private void RescheduleOnSettingChanged()
        {
            if (this.ConfigAutosaveOnTimer)
            {
#if DEBUG
                HarmonyPatches.LogMessage("RescheduleOnSettingChanged() - trying to reschedule next save.");
#endif

                Player.main?.GetComponent<AutosaveController>()?.ScheduleAutosave(this.ConfigMinutesBetweenAutosaves, true);
            }
        }
    }
}
