using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Nautilus.Handlers;
using Nautilus.Options;
using UnityEngine;

namespace SubnauticaAutosave
{
	public class ModSettings : ModOptions
	{
		/* General mod settings */
		private ModToggleOption autosaveOnTimerOption;
		private ModToggleOption autosaveOnSleepOption;
		private ModSliderOption maxSaveFilesOption;
		private ModToggleOption hardcoreModeOption;
		// public static ConfigEntry<KeyCode> ConfigQuicksaveKey;

		/* Autosave conditions */
		private ModSliderOption minutesBetweenAutosavesOption;
		private ModSliderOption minimumPlayerHealthPercentOption;
		private ModToggleOption delaySaveOnManualOption;

		/* Other settings */
		private ModToggleOption showSaveMessagesOption;
		private ModToggleOption showSaveNamesOption;
		private ModToggleOption comprehensiveSavesOption;
		private ModToggleOption useCustomDateFormatOption;
		private ModChoiceOption<DateTimeFormat> customDateTimeFormatOption;

		public ModSettings() : base(ModPlugin.modName)
		{
			OptionsPanelHandler.RegisterModOptions(this);

			this.autosaveOnTimerOption = ModPlugin.ConfigAutosaveOnTimer.ToModToggleOption();
			this.AddItem(this.autosaveOnTimerOption);

			this.autosaveOnSleepOption = ModPlugin.ConfigAutosaveOnSleep.ToModToggleOption();
			this.AddItem(this.autosaveOnSleepOption);

			this.maxSaveFilesOption = ModPlugin.ConfigMaxSaveFiles.ToModSliderOption(1, ModPlugin.MaxSaveFiles, 1);
			this.AddItem(this.maxSaveFilesOption);

			this.hardcoreModeOption = ModPlugin.ConfigHardcoreMode.ToModToggleOption();
			this.AddItem(this.hardcoreModeOption);



			this.minutesBetweenAutosavesOption = ModPlugin.ConfigMinutesBetweenAutosaves.ToModSliderOption(1, ModPlugin.MaxMinutesBetweenSaves, 1);
			this.AddItem(this.minutesBetweenAutosavesOption);

			this.minimumPlayerHealthPercentOption = ModPlugin.ConfigMinimumPlayerHealthPercent.ToModSliderOption(0f, 1f, 0.05f);
			this.AddItem(this.minimumPlayerHealthPercentOption);

			this.delaySaveOnManualOption = ModPlugin.ConfigDelaySaveOnManual.ToModToggleOption();
			this.AddItem(this.delaySaveOnManualOption);



			this.showSaveMessagesOption = ModPlugin.ConfigShowSaveMessages.ToModToggleOption();
			this.AddItem(this.showSaveMessagesOption);

			this.showSaveNamesOption = ModPlugin.ConfigShowSaveNames.ToModToggleOption();
			this.AddItem(this.showSaveNamesOption);

			this.comprehensiveSavesOption = ModPlugin.ConfigComprehensiveSaves.ToModToggleOption();
			this.AddItem(this.comprehensiveSavesOption);

			this.useCustomDateFormatOption = ModPlugin.ConfigUseCustomDateFormat.ToModToggleOption();
			this.AddItem(this.useCustomDateFormatOption);

			List<DateTimeFormat> dateTimeFormats = new List<DateTimeFormat>();
			foreach (DateTimeFormat dtf in Enum.GetValues(typeof(DateTimeFormat)))
			{
				dateTimeFormats.Add(dtf);
			}
			this.customDateTimeFormatOption = ModPlugin.ConfigCustomDateTimeFormat.ToModChoiceOption<DateTimeFormat>(options: dateTimeFormats);
			this.AddItem(this.customDateTimeFormatOption);
		}
	}
}
