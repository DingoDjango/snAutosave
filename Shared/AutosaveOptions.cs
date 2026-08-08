using System;
using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using UnityEngine;

namespace SubnauticaAutosave
{
    [Menu(ModPlugin.modName)]
    public class AutosaveOptions : ConfigFile
    {
        // Fired when timing-related options change; ModPlugin subscribes to reschedule the next save.
        public static Action OnTimingChanged;

        // Option row GameObjects, refreshed each menu open.
        private static GameObject maxSaveFilesOptionObject;
        private static GameObject delaySaveOnManualOptionObject;
        private static GameObject minutesBetweenAutosavesOptionObject;
        private static GameObject customDateTimeFormatOptionObject;

        /* General settings */
        [Toggle(null, LabelLanguageId = "HardcoreMode", TooltipLanguageId = "Tooltip_HardcoreMode")]
        [OnChange(nameof(OnHardcoreModeChanged))]
        public bool HardcoreMode = false;

        // Only shown when Hardcore Mode is disabled.
        [Slider(null, 1, ModPluginBase.MaxSaveFiles, DefaultValue = 3, Format = "{0:F0}", Step = 1, LabelLanguageId = "MaxSaveFiles", TooltipLanguageId = "Tooltip_MaxSaveFiles")]
        [OnGameObjectCreated(nameof(OnMaxSaveFilesOptionCreated))]
        public int MaxSaveFiles = 3;

        [Toggle(null, LabelLanguageId = "AutosaveOnTimer", TooltipLanguageId = "Tooltip_AutosaveOnTimer")]
        [OnChange(nameof(OnAutosaveOnTimerChanged))]
        public bool AutosaveOnTimer = true;

        // Only shown when timer autosave is enabled.
        [Toggle(null, LabelLanguageId = "DelaySaveOnManual", TooltipLanguageId = "Tooltip_DelaySaveOnManual")]
        [OnGameObjectCreated(nameof(OnDelaySaveOnManualOptionCreated))]
        public bool DelaySaveOnManual = false;

        // Only shown when timer autosave is enabled.
        [Slider(null, 1, ModPluginBase.MaxMinutesBetweenSaves, DefaultValue = 15, Format = "{0:F0}", Step = 1, LabelLanguageId = "MinutesBetweenAutosaves", TooltipLanguageId = "Tooltip_MinutesBetweenAutosaves")]
        [OnChange(nameof(OnMinutesBetweenAutosavesChanged))]
        [OnGameObjectCreated(nameof(OnMinutesBetweenAutosavesOptionCreated))]
        public int MinutesBetweenAutosaves = 15;

        [Toggle(null, LabelLanguageId = "AutosaveOnSleep", TooltipLanguageId = "Tooltip_AutosaveOnSleep")]
        public bool AutosaveOnSleep = true;

        /* Other settings */
        [Toggle(null, LabelLanguageId = "ComprehensiveSaves", TooltipLanguageId = "Tooltip_ComprehensiveSaves")]
        public bool ComprehensiveSaves = true;

        [Toggle(null, LabelLanguageId = "ShowSaveMessages", TooltipLanguageId = "Tooltip_ShowSaveMessages")]
        public bool ShowSaveMessages = true;

        [Slider(null, 0f, 1f, DefaultValue = 0.25f, Format = "{0:P0}", Step = 0.05f, LabelLanguageId = "MinimumPlayerHealthPercent", TooltipLanguageId = "Tooltip_MinimumPlayerHealthPercent")]
        public float MinimumPlayerHealthPercent = 0.25f;

        [Toggle(null, LabelLanguageId = "ShowSaveNames", TooltipLanguageId = "Tooltip_ShowSaveNames")]
        public bool ShowSaveNames = true;

        [Toggle(null, LabelLanguageId = "UseCustomDateFormat", TooltipLanguageId = "Tooltip_UseCustomDateFormat")]
        [OnChange(nameof(OnUseCustomDateFormatChanged))]
        public bool UseCustomDateFormat = false;

        // Only shown when custom date format is enabled.
        [Choice(LabelLanguageId = "CustomDateTimeFormat", TooltipLanguageId = "Tooltip_CustomDateTimeFormat",
            Options = new[]
            {
                "DateTimeFormat_DMMMYYYY_24Hour",
                "DateTimeFormat_DMY_24Hour",
                "DateTimeFormat_YMD_24Hour",
                "DateTimeFormat_DMMMYYYY_12Hour",
                "DateTimeFormat_MDY_12Hour"
            })]
        [OnGameObjectCreated(nameof(OnCustomDateTimeFormatOptionCreated))]
        public DateTimeFormat CustomDateTimeFormat = DateTimeFormat.DMMMYYYY_24Hour;

        private void OnHardcoreModeChanged(object sender, ToggleChangedEventArgs e)
        {
            ApplyVisibility();
        }

        private void OnAutosaveOnTimerChanged(object sender, ToggleChangedEventArgs e)
        {
            OnTimingChanged?.Invoke();
            ApplyVisibility();
        }

        private void OnMinutesBetweenAutosavesChanged(object sender, SliderChangedEventArgs e)
        {
            OnTimingChanged?.Invoke();
        }

        private void OnUseCustomDateFormatChanged(object sender, ToggleChangedEventArgs e)
        {
            ApplyVisibility();
        }

        private void OnMaxSaveFilesOptionCreated(GameObjectCreatedEventArgs e)
        {
            maxSaveFilesOptionObject = e.Value;
            ApplyVisibility();
        }

        private void OnDelaySaveOnManualOptionCreated(GameObjectCreatedEventArgs e)
        {
            delaySaveOnManualOptionObject = e.Value;
            ApplyVisibility();
        }

        private void OnMinutesBetweenAutosavesOptionCreated(GameObjectCreatedEventArgs e)
        {
            minutesBetweenAutosavesOptionObject = e.Value;
            ApplyVisibility();
        }

        private void OnCustomDateTimeFormatOptionCreated(GameObjectCreatedEventArgs e)
        {
            customDateTimeFormatOptionObject = e.Value;
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            // Unity == null treats destroyed objects as null; ?. checks managed reference only
            if (maxSaveFilesOptionObject != null)
            {
                maxSaveFilesOptionObject.SetActive(!HardcoreMode);
            }
            if (delaySaveOnManualOptionObject != null)
            {
                delaySaveOnManualOptionObject.SetActive(AutosaveOnTimer);
            }
            if (minutesBetweenAutosavesOptionObject != null)
            {
                minutesBetweenAutosavesOptionObject.SetActive(AutosaveOnTimer);
            }
            if (customDateTimeFormatOptionObject != null)
            {
                customDateTimeFormatOptionObject.SetActive(UseCustomDateFormat);
            }
        }
    }
}
