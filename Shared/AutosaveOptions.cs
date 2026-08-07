using System;
using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using UnityEngine;

namespace SubnauticaAutosave
{
    [Menu("Subnautica Autosave")]
    public class AutosaveOptions : ConfigFile
    {
        // Fired when timing-related options change; ModPlugin subscribes to reschedule the next save.
        public static Action OnTimingChanged;

        /* General settings */
        [Toggle(null, LabelLanguageId = "AutosaveOnTimer", TooltipLanguageId = "Tooltip_AutosaveOnTimer")]
        [OnChange(nameof(OnAutosaveOnTimerChanged))]
        public bool AutosaveOnTimer = true;

        [Toggle(null, LabelLanguageId = "AutosaveOnSleep", TooltipLanguageId = "Tooltip_AutosaveOnSleep")]
        public bool AutosaveOnSleep = true;

        [Slider(null, 1, ModPluginBase.MaxSaveFiles, DefaultValue = 3, Format = "{0:F0}", Step = 1, LabelLanguageId = "MaxSaveFiles", TooltipLanguageId = "Tooltip_MaxSaveFiles")]
        public int MaxSaveFiles = 3;

        [Toggle(null, LabelLanguageId = "HardcoreMode", TooltipLanguageId = "Tooltip_HardcoreMode")]
        public bool HardcoreMode = false;

        [Keybind(null, LabelLanguageId = "QuicksaveKey", TooltipLanguageId = "Tooltip_QuicksaveKey")]
        public KeyCode QuicksaveKey = KeyCode.F9;

        /* Autosave conditions */
        [Slider(null, 1, ModPluginBase.MaxMinutesBetweenSaves, DefaultValue = 15, Format = "{0:F0}", Step = 1, LabelLanguageId = "MinutesBetweenAutosaves", TooltipLanguageId = "Tooltip_MinutesBetweenAutosaves")]
        [OnChange(nameof(OnMinutesBetweenAutosavesChanged))]
        public int MinutesBetweenAutosaves = 15;

        [Slider(null, 0f, 1f, DefaultValue = 0.25f, Format = "{0:P0}", Step = 0.05f, LabelLanguageId = "MinimumPlayerHealthPercent", TooltipLanguageId = "Tooltip_MinimumPlayerHealthPercent")]
        public float MinimumPlayerHealthPercent = 0.25f;

        [Toggle(null, LabelLanguageId = "DelaySaveOnManual", TooltipLanguageId = "Tooltip_DelaySaveOnManual")]
        public bool DelaySaveOnManual = false;

        /* Other settings */
        [Toggle(null, LabelLanguageId = "ShowSaveMessages", TooltipLanguageId = "Tooltip_ShowSaveMessages")]
        public bool ShowSaveMessages = true;

        [Toggle(null, LabelLanguageId = "ShowSaveNames", TooltipLanguageId = "Tooltip_ShowSaveNames")]
        public bool ShowSaveNames = true;

        [Toggle(null, LabelLanguageId = "ComprehensiveSaves", TooltipLanguageId = "Tooltip_ComprehensiveSaves")]
        public bool ComprehensiveSaves = true;

        [Toggle(null, LabelLanguageId = "UseCustomDateFormat", TooltipLanguageId = "Tooltip_UseCustomDateFormat")]
        public bool UseCustomDateFormat = false;

        [Choice(null, LabelLanguageId = "CustomDateTimeFormat", TooltipLanguageId = "Tooltip_CustomDateTimeFormat",
            Options = new[]
            {
                "DateTimeFormat_DMMMYYYY_24Hour",
                "DateTimeFormat_DMY_24Hour",
                "DateTimeFormat_YMD_24Hour",
                "DateTimeFormat_DMMMYYYY_12Hour",
                "DateTimeFormat_MDY_12Hour"
            })]
        public DateTimeFormat CustomDateTimeFormat = DateTimeFormat.DMMMYYYY_24Hour;

        private void OnAutosaveOnTimerChanged(object sender, ToggleChangedEventArgs e)
        {
            OnTimingChanged?.Invoke();
        }

        private void OnMinutesBetweenAutosavesChanged(object sender, SliderChangedEventArgs e)
        {
            OnTimingChanged?.Invoke();
        }
    }
}
