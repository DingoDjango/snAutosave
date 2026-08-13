using BepInEx;
using BepInEx.Configuration;
using Nautilus.Handlers;
using UnityEngine;

namespace SubnauticaAutosave
{
	[BepInPlugin(modGUID, modName, modVersion)]
	[BepInDependency("com.snmodding.nautilus")]
	public class ModPlugin : ModPluginBase
	{
		public const string modGUID = "Dingo.SNBZ.SubnauticaAutosave";
		public const string modName = "Autosave BZ";
		public const string modVersion = "3.1.8.3031";

		private void Awake()
        {
            Instance = this;

            LanguageHandler.RegisterLocalizationFolder();

		    // Register ModOptions for Nautilus config system
		    options = OptionsPanelHandler.RegisterModOptions<ModOptions>();

		    HarmonyPatches.InitializeHarmony();
		}

		private void Update()
		{
			if (!ModOptions.hasPendingQuicksaveKey && Input.GetKeyDown(options.QuicksaveKey))
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

        public override void LogMessage(string message)
        {
            Debug.Log($"{modName} :: {message}");
        }

        public override void LogWarning(string warning)
        {
            Debug.LogWarning($"{modName} :: {warning}");
        }

        public override void LogError(string error)
        {
            Debug.LogError($"{modName} :: {error}");
        }
    }
}
