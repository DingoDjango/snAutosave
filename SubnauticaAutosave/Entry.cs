using System.IO;
using System.Reflection;
using UnityEngine;

namespace SubnauticaAutosave
{
	public static class Entry
	{
		internal static Config GetConfig = null;

		internal static string GetAssemblyDirectory
		{
			get
			{
				string fullPath = Assembly.GetExecutingAssembly().Location;

				return Path.GetDirectoryName(fullPath);
			}
		}

		internal static void LogMessage(string message)
		{
			Debug.Log("[snAutosave] :: " + message);
		}

		public static void Initialize()
		{
			ConfigHandler.LoadConfig();

			HarmonyPatches.InitializeHarmony();

#if DEBUG
			LogMessage("Initialized Harmony");
#endif
		}
	}
}
