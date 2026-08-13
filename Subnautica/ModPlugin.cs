using BepInEx;
using Nautilus.Handlers;
using UnityEngine;

namespace SubnauticaAutosave
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("com.snmodding.nautilus")]
    public class ModPlugin : ModPluginBase
    {
        public const string modGUID = "Dingo.SN.SubnauticaAutosave";
        public const string modName = "Autosave";
        public const string modVersion = "3.1.8.3031";

        private void Awake()
        {
            Instance= this;

            LanguageHandler.RegisterLocalizationFolder();

            options = OptionsPanelHandler.RegisterModOptions<ModOptions>();
            
            Keybinds.Initialize();
            
            HarmonyPatches.InitializeHarmony();
        }

        private void Update()
        {
            // GameInput.input null before init and during scene transitions
            if (!GameInput.IsInitialized)
            {
                return;
            }

            if (GameInput.GetButtonDown(Keybinds.Quicksave))
            {
                if (SaveLoadManager.main.isSaving)
                {
#if DEBUG
                    LogMessage("Quicksave skipped: save operation in progress");
#endif
                }
                else
                {
                    IngameMenu.main?.SaveGame();
                }
            }

#if DEBUG
            if (GameInput.GetButtonDown(Keybinds.DebugAutosaveTrigger))
            {
                LogMessage("Pressed debug trigger key, executing autosave");

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
