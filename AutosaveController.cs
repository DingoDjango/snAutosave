﻿using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UWE;

namespace SubnauticaAutosave
{
    /* Some of the following code is based on "Safe Autosave" by berkay2578:
	 * https://www.nexusmods.com/subnautica/mods/94
	 * https://github.com/berkay2578/SubnauticaMods/tree/master/SafeAutosave
	 *
	 * Directory replication code from:
	 * https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp */

    public class AutosaveController : MonoBehaviour
    {
        private const int PriorWarningSeconds = 30;
        private const string AutosaveSuffixFormat = "_auto{0:0000}";

        private int latestAutosaveSlot = -1;

        private bool isSaving = false;

        private bool warningTriggered = false;

        private float nextSaveTriggerTime = Time.realtimeSinceStartup + 120f;

        private UserStorage GlobalUserStorage => PlatformUtils.main.GetUserStorage();

        private string SavedGamesDirPath
        {
            get
            {
                if (GlobalUserStorage == null)
                {
                    return null;
                }

                DirectoryInfo saveDir = new DirectoryInfo((string)AccessTools.Field(typeof(UserStoragePC), "savePath").GetValue(GlobalUserStorage));
                string savePath = saveDir.FullName;

#if DEBUG
                ModPlugin.LogMessage($"Save path is {savePath}");
#endif

                return savePath;
            }
        }

        private string SlotSuffixFormatted(int slotNumber)
        {
            // Example output: "_auto0003"
            return string.Format(AutosaveSuffixFormat, slotNumber);
        }

        private string GetMainSlotName(string currentSlot)
        {
            // Input:   slot0000_auto0001
            // Output:  slot0000
            return currentSlot.Split('_')[0];
        }

        private int GetAutosaveSlotNumberFromDir(string directoryName)
        {
            int slotNumber = int.Parse(directoryName.Split(new string[] { "auto" }, StringSplitOptions.None).Last());

#if DEBUG
            ModPlugin.LogMessage($"GetAutosaveSlotNumberFromDir returned {slotNumber} for {directoryName}");
#endif

            return slotNumber;
        }

        private bool IsAllowedAutosaveSlotNumber(int slotNumber)
        {
            return slotNumber <= ModPlugin.ConfigMaxSaveFiles.Value;
        }

        private int GetLatestAutosaveForSlot(string mainSaveSlot)
        {
            if (mainSaveSlot.Contains("auto"))
            {
                mainSaveSlot = this.GetMainSlotName(mainSaveSlot);
            }

            string savedGamesDir = this.SavedGamesDirPath;
            string searchPattern = "*" + mainSaveSlot + "_auto" + "*";

            if (Directory.Exists(savedGamesDir))
            {
                DirectoryInfo[] saveDirectories = new DirectoryInfo(savedGamesDir).GetDirectories(searchPattern, SearchOption.TopDirectoryOnly);

#if DEBUG
                ModPlugin.LogMessage($"GetLatestAutosaveForSlot found {saveDirectories.Count()} autosaves for {mainSaveSlot}");
#endif

                if (saveDirectories.Count() > 0)
                {
                    IOrderedEnumerable<DirectoryInfo> saveSlotsByLastModified = saveDirectories.OrderByDescending(d => d.GetFiles("gameinfo.json")[0].LastWriteTime);

                    foreach (DirectoryInfo saveDir in saveSlotsByLastModified)
                    {
                        int autosaveSlotNumber = GetAutosaveSlotNumberFromDir(saveDir.Name);

                        if (IsAllowedAutosaveSlotNumber(autosaveSlotNumber))
                        {
                            // The most recent save slot used, which matches the maximum save slots setting
                            return autosaveSlotNumber;
                        }
                    }
                }
            }

#if DEBUG
            else
            {
                ModPlugin.LogMessage($"savedGamesDir == {savedGamesDir}. Could not get save path.");
            }
#endif

            return -1;
        }

        private int RotateAutosaveSlotNumber()
        {
            if (this.latestAutosaveSlot < 0 || this.latestAutosaveSlot >= ModPlugin.ConfigMaxSaveFiles.Value)
            {
                this.latestAutosaveSlot = 1;
            }

            else
            {
                this.latestAutosaveSlot++;
            }

            return this.latestAutosaveSlot;
        }

        private bool IsSafePlayerHealth(float minHealthPercent)
        {
            return Player.main.liveMixin.GetHealthFraction() >= minHealthPercent;
        }

        private bool IsSafeToSave()
        {
            /* Use vanilla checks for cinematics and current saving status */

            MethodInfo getAllowSaving = AccessTools.Method(typeof(IngameMenu), "GetAllowSaving");

#if DEBUG
            if (getAllowSaving == null)
            {
                ModPlugin.LogMessage("GetAllowSaving is null, returning false.");

                return false;
            }
#endif

            bool saveAllowed = (bool)getAllowSaving?.Invoke(IngameMenu.main, null);

            if (!saveAllowed)
            {
#if DEBUG
                ModPlugin.LogMessage($"Did not save. GetAllowSaving returned {saveAllowed}.");
#endif

                return false;
            }

            /* (Optional) Check if player health is in allowed range */

            float safeHealthFraction = ModPlugin.ConfigMinimumPlayerHealthPercent.Value;

            if (safeHealthFraction > 0f && !this.IsSafePlayerHealth(safeHealthFraction))
            {
                return false;
            }

            return true;
        }

        private IEnumerator AutosaveCoroutine()
        {
            this.isSaving = true;

            bool hardcoreMode = ModPlugin.ConfigHardcoreMode.Value; // Add autosave permadeath option as well? (bisa)

            ErrorMessage.AddWarning("AutosaveStarting".Translate());

            yield return null;

            /* Trick the game into copying screenshots and other files from temporary save storage.
             * This will make the game copy every file, which can be slower on old hardware.
             * Can be considered a beta feature. Keep track of non-forseen consequences... */
            if (ModPlugin.ConfigComprehensiveSaves.Value)
            {
                this.DoSaveLoadManagerDateHack();
            }

            yield return null;

            this.SetMainSlotIfAutosave(); // Make sure the main slot is set correctly. It should always be a clean slot name without _auto

            string mainSaveSlot = SaveLoadManager.main.GetCurrentSlot();

            if (!hardcoreMode)
            {
                string autosaveSlotName = mainSaveSlot + SlotSuffixFormatted(RotateAutosaveSlotNumber());

                SaveLoadManager.main.SetCurrentSlot(autosaveSlotName);
            }

            yield return null;

            IEnumerator saveGameAsync = (IEnumerator)typeof(IngameMenu).GetMethod("SaveGameAsync", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(IngameMenu.main, null);

            yield return CoroutineHost.StartCoroutine(saveGameAsync);

#if DEBUG
            ModPlugin.LogMessage("Post-saveGameAsync reached.");
#endif

            yield return null;

            if (!hardcoreMode)
            {
                SaveLoadManager.main.SetCurrentSlot(mainSaveSlot);

                yield return null;
            }

            if (ModPlugin.ConfigAutosaveOnTimer.Value)
            {
                int autosaveMinutesInterval = ModPlugin.ConfigMinutesBetweenAutosaves.Value;

                this.ScheduleAutosave(autosaveMinutesInterval);

                ErrorMessage.AddWarning("AutosaveEnding".FormatTranslate(autosaveMinutesInterval.ToString()));

                yield return null;
            }

            this.warningTriggered = false;
            this.isSaving = false;

#if DEBUG
            ModPlugin.LogMessage("Reached post-autosave without crashing");
#endif

            yield break;
        }

        private void Tick()
        {
            if (ModPlugin.ConfigAutosaveOnTimer.Value)
            {
                if (!this.warningTriggered && Time.realtimeSinceStartup >= this.nextSaveTriggerTime - PriorWarningSeconds)
                {
                    ErrorMessage.AddWarning("AutosaveWarning".FormatTranslate(PriorWarningSeconds.ToString()));

                    this.warningTriggered = true;
                }

                else if (!this.isSaving && Time.realtimeSinceStartup >= this.nextSaveTriggerTime)
                {
                    if (!this.TryExecuteAutosave())
                    {
#if DEBUG
                        ModPlugin.LogMessage("Could not autosave on time. Delaying autosave.");
#endif

                        this.DelayAutosave();
                    }
                }
            }
        }
        
        public void DoSaveLoadManagerDateHack()
        {
            DateTime oldTime = new DateTime(year: 1980, month: 1, day: 1);

            AccessTools.Field(typeof(SaveLoadManager), "lastSaveTime").SetValue(SaveLoadManager.main, oldTime);

#if DEBUG
            DateTime currentManagerLastSave = (DateTime)AccessTools.Field(typeof(SaveLoadManager), "lastSaveTime").GetValue(SaveLoadManager.main);

            ModPlugin.LogMessage($"Override lastSaveTime. Current value is {currentManagerLastSave}");
#endif
        }

        public void SetMainSlotIfAutosave()
        {
            string currentSlot = SaveLoadManager.main.GetCurrentSlot();

            if (currentSlot.Contains("auto"))
            {
                string mainSaveSlot = this.GetMainSlotName(currentSlot);

                SaveLoadManager.main.SetCurrentSlot(mainSaveSlot);
            }
        }

        public void ScheduleAutosave(int addedMinutes)
        {
            // Time.realtimeSinceStartup returns a float in terms of seconds
            this.nextSaveTriggerTime = Time.realtimeSinceStartup + (60 * addedMinutes);
        }

        public void DelayAutosave(float addedSeconds = 10f)
        {
            this.nextSaveTriggerTime += addedSeconds;
        }

        public bool TryExecuteAutosave()
        {
            if (this.IsSafeToSave())
            {
                if (!this.isSaving)
                {
                    try
                    {
                        CoroutineHost.StartCoroutine(this.AutosaveCoroutine());

                        return true;
                    }

                    catch (Exception ex)
                    {
                        ModPlugin.LogMessage("Failed to execute save coroutine. Something went wrong.");
                        ModPlugin.LogMessage(ex.ToString());
                    }
                }

                else
                {
                    ErrorMessage.AddWarning("AutosaveInProgress".Translate());
                }
            }

#if DEBUG
            else
            {
                ModPlugin.LogMessage("IsSafeToSave returned false.");

            }
#endif

            return false;
        }

        // Monobehaviour.Awake(), called before Start()
        public void Awake()
        {
            this.ScheduleAutosave(ModPlugin.ConfigMinutesBetweenAutosaves.Value);            

#if DEBUG
            ModPlugin.LogMessage($"AutosaveController.Awake() - Initial save trigger set to {this.nextSaveTriggerTime}");
#endif

            if (!ModPlugin.ConfigHardcoreMode.Value)
            {
                this.latestAutosaveSlot = this.GetLatestAutosaveForSlot(SaveLoadManager.main.GetCurrentSlot());
            }

#if DEBUG            
            ModPlugin.LogMessage($"AutosaveController.Awake() - Latest autosave for {SaveLoadManager.main.GetCurrentSlot()} set to {this.latestAutosaveSlot}");
#endif
        }

        // Monobehaviour.Start
        public void Start()
        {
            // Repeat the Tick method every second
            this.InvokeRepeating(nameof(AutosaveController.Tick), 1f, 1f);
        }
    }
}
