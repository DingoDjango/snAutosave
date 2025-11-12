using BepInEx;
using BepInEx.Configuration;
using Nautilus.Handlers;
using UnityEngine;

namespace SubnauticaAutosave
{
	[BepInPlugin(modGUID, modName, modVersion)]
	[BepInDependency("com.snmodding.nautilus")]
	[BepInDependency("snbz.subnauticamap.mod", BepInDependency.DependencyFlags.SoftDependency)]
	public class ModPlugin : ModPluginBase
	{
		public const string modGUID = "Dingo.SNBZ.SubnauticaAutosave";
		public const string modName = "Subnautica Autosave BZ";
		public const string modVersion = "2.4.0";

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

		public override void Awake()
		{
			LanguageHandler.RegisterLocalizationFolder();

			this.InitializeConfig();

			this.ModSettings = new ModSettings();

			HarmonyPatches.InitializeHarmony();
		}

		public override void Update()
		{
			if (Input.GetKeyDown(ConfigQuicksaveKey.Value))
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
	}
}
