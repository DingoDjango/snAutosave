using BepInEx;
using Nautilus.Handlers;
using System.Collections;
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

        private Keybinds keyBinds;

        // Implemented as coroutine because buggy otherwise
        private IEnumerator InitializeBindings()
        {
            while (!GameInput.IsInitialized)
            {
                yield return new WaitForEndOfFrame();
            }

#if DEBUG
            // Unbind vanilla "[" + "]" from secondary bindings
            GameInput.SetBinding(GameInput.Device.Keyboard, GameInput.Button.CycleNext, GameInput.BindingSet.Secondary, string.Empty);
            GameInput.SetBinding(GameInput.Device.Keyboard, GameInput.Button.CyclePrev, GameInput.BindingSet.Secondary, string.Empty);
#endif

            keyBinds = new Keybinds();

            yield break;
        }

        private void Awake()
        {
            Instance= this;

            LanguageHandler.RegisterLocalizationFolder();

            options = OptionsPanelHandler.RegisterModOptions<ModOptions>();
            
            HarmonyPatches.InitializeHarmony();

            StartCoroutine(InitializeBindings());
        }

        private void Update()
        {
            if (!GameInput.IsInitialized)
            {
                return;
            }

            if (GameInput.GetButtonDown(keyBinds.Quicksave))
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
            if (GameInput.GetButtonDown(keyBinds.DebugAutosaveTrigger))
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
