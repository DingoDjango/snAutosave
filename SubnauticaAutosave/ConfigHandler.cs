using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

namespace SubnauticaAutosave
{
	/* Json serialization from:
	 * https://docs.unity3d.com/Manual/JSONSerialization.html
	 *
	 * Directory fetching snippet from:
	 * https://stackoverflow.com/questions/52797/how-do-i-get-the-path-of-the-assembly-the-code-is-in */

	internal static class ConfigHandler
	{
		private const string SettingsFileName = "settings.json";

		private static bool ValidateValues(Config cfg)
		{
#if (!DEBUG)
			if (cfg.SecondsBetweenAutosaves < 120)
			{
				Entry.LogMessage("Please allow at least two minutes between saves. Config invalidated.");

				return false;
			}

			if (cfg.MaxSaveFiles < 1)
			{
				Entry.LogMessage("Please allow at least one autosave file slot. Config invalidated.");

				return false;
			}

			if (cfg.HardcoreMode.GetType() != typeof(bool))
			{
				Entry.LogMessage("Please use only true or false for HardcoreMode.");

				return false;
			}
#endif

			return true;
		}

		internal static void LoadConfig()
		{
			try
			{
				string settingsFilePath = Path.Combine(Entry.GetAssemblyDirectory, SettingsFileName);
				string settingsAsJson = File.ReadAllText(settingsFilePath);
				Config configFromJson = JsonConvert.DeserializeObject<Config>(settingsAsJson);

				if (ValidateValues(configFromJson))
				{
					Entry.GetConfig = configFromJson;
				}

				else
				{
					Entry.LogMessage("Failed to validate config values. Using defaults.");

					Entry.GetConfig = new Config();
				}

#if DEBUG
				Entry.GetConfig.SecondsBetweenAutosaves = 60;
				Entry.GetConfig.MaxSaveFiles = 4;
#endif
			}

			catch (Exception ex)
			{
				Entry.LogMessage(ex.ToString());
				Entry.LogMessage("Caught exception while executing LoadConfig");
			}
		}
	}
}
