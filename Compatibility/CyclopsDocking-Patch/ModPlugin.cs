using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace SubnauticaAutosave_Compatibility
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency(CyclopsHarmonyId, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(AutosaveHarmonyId, BepInDependency.DependencyFlags.SoftDependency)]
    public class ModPlugin : BaseUnityPlugin
    {
        // From Cyclops Docking - CyclopsDockingMod.cs
        private const string CyclopsHarmonyId = "com.osubmarin.cyclopsdockingmod";
        // From Autosave - ModPlugin.cs
        private const string AutosaveHarmonyId = "Dingo.SN.SubnauticaAutosave";

        public const string modGUID = "Dingo.SN.SubnauticaAutosave_Comp_CyclopsDocking";
        public const string modName = "Subnautica Autosave and Cyclops Docking Compatibility Patch";
        public const string modVersion = "1.0.8.3031";

        private void Awake()
        {
            try
            {
                SaveHooks.Initialize();
                var compat = new Harmony("com.snautosave.cyclopscompat");
                SaveHooks.MoveCyclopsSaveHooks(compat, CyclopsHarmonyId);
                SaveHooks.ApplyPatches(compat);
#if DEBUG
                // [DEBUG-TEMP]
                LogMessage("Hooks installed, ready");
                // [DEBUG-TEMP-END]
#endif
            }
            catch (Exception ex)
            {
                LogError($"Compat init failed: {ex}");
            }
        }

        public static void LogMessage(string message)
        {
            Debug.Log($"{modName} :: {message}");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"{modName} :: {message}");
        }
    }
}
