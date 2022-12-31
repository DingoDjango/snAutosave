using System;
using System.Collections;
using System.Collections.Generic;
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
        private const int RetryTicks = 5;
        private const int PriorWarningTicks = 30;
        private const string AutosaveSuffixFormat = "_autosave{0:0000}";

        private int lastUsedAutosaveSlot = -1;

        private bool isSaving = false;

        private int totalTicks = 0;

        private int nextSaveTriggerTick = 120;

        public bool IsSaving => this.IsSaving;

        private string SlotSuffixFormatted(int slotNumber)
        {
            // Example output: "_autosave0003"
            return string.Format(AutosaveSuffixFormat, slotNumber);
        }

        private int GetAutosaveSlotNumberFromDir(string directoryName)
        {
            int slotNumber = int.Parse(directoryName.Split(new string[] { "autosave" }, StringSplitOptions.None).Last());

#if DEBUG
            ModPlugin.LogMessage($"GetAutosaveSlotNumberFromDir returned {slotNumber}");
#endif

            return slotNumber;
        }

        private bool IsAllowedAutosaveSlotNumber(int slotNumber)
        {
            return slotNumber < ModPlugin.ConfigMaxSaveFiles.Value;
        }

        private int GetLatestAutosaveForSlot(string mainSaveSlot)
        {
            string savedGamesDir = ModPlugin.savedGamesPath;
            string searchPattern = "*" + mainSaveSlot + "_autosave" + "*";   /**********************/

            if (Directory.Exists(savedGamesDir))
            {
                DirectoryInfo[] saveDirectories = new DirectoryInfo(savedGamesDir).GetDirectories(searchPattern, SearchOption.TopDirectoryOnly);

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
                ModPlugin.LogMessage("Could not find saved games directory. Is UserStoragePC patched?");
            }
#endif

            return -1;
        }

        private int NextAutosaveSlotNumber()
        {
            if (this.lastUsedAutosaveSlot > 0 && this.lastUsedAutosaveSlot < ModPlugin.ConfigMaxSaveFiles.Value)
            {
                return this.lastUsedAutosaveSlot + 1;
            }

            else return 1;
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
                ModPlugin.LogMessage("GetAllowSaving was not accessed correctly.");

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

        /***
        private void CopyScreenshotFiles(string originalSlot, string targetSlot)
        {
            string originalScreenshotsDir = Path.Combine(Path.Combine(GetSavePath(), originalSlot), ScreenshotManager.screenshotsFolderName);

            if (Directory.Exists(originalScreenshotsDir))
            {
                string newScreenshotsDir = originalScreenshotsDir.Replace(originalSlot, targetSlot);

                // .CreateDirectory harmlessly terminates if the target already exists
                Directory.CreateDirectory(newScreenshotsDir);

                string[] filesToCopy = Directory.GetFiles(originalScreenshotsDir, "*", SearchOption.TopDirectoryOnly);

                // Copy all the files & replace any files with the same name
                foreach (string screenshot in filesToCopy)
                {
                    File.Copy(screenshot, screenshot.Replace(originalSlot, targetSlot), true);
                }
            }
        }
        ***/

        private IEnumerator AutosaveCoroutine()
        {
            this.isSaving = true;

            bool hardcoreMode = ModPlugin.ConfigHardcoreMode.Value;

            ErrorMessage.AddWarning("AutosaveStarting".Translate());

            yield return null;

            /****/
            string mainSaveSlot = SaveLoadManager.main.GetCurrentSlot();


            SaveLoadManager.main.SetCurrentSlot("autosave_test");
            /***/

            IEnumerator saveGameAsync = (IEnumerator)typeof(IngameMenu).GetMethod("SaveGameAsync", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(IngameMenu.main, null);

            yield return CoroutineHost.StartCoroutine(saveGameAsync);

#if DEBUG
            ModPlugin.LogMessage("saveGameAsync reached.");
#endif

            /***/
            SaveLoadManager.main.SetCurrentSlot(mainSaveSlot);
            /****/

            if (!hardcoreMode)
            {
                int nextAutosaveSlot = this.NextAutosaveSlotNumber();

                // delete old autosave contents
                // copy main save files to autosave slot

                this.lastUsedAutosaveSlot = nextAutosaveSlot;
            }

            yield return null;

            int autosaveInterval = ModPlugin.ConfigSecondsBetweenAutosaves.Value;

            this.nextSaveTriggerTick += autosaveInterval;

            yield return null;

            ErrorMessage.AddWarning("AutosaveEnding".FormatTranslate(autosaveInterval.ToString()));

            this.isSaving = false;

#if DEBUG
            ModPlugin.LogMessage("Reached post-autosave without crashing");
#endif

            yield break;
        }

        private void Tick()
        {
            this.totalTicks++;

            if (this.totalTicks == this.nextSaveTriggerTick - PriorWarningTicks)
            {
                ErrorMessage.AddWarning("AutosaveWarning".FormatTranslate(PriorWarningTicks.ToString()));
            }

            else if (this.totalTicks >= this.nextSaveTriggerTick && !this.isSaving)
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

        public void DelayAutosave()
        {
            this.nextSaveTriggerTick += RetryTicks;
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
                        ModPlugin.LogMessage(ex.ToString());
                        ModPlugin.LogMessage("Failed to execute save coroutine. Something went wrong.");
                    }
                }

                else
                {
                    ErrorMessage.AddWarning("AutosaveInProgress".Translate());
                }
            }

            else
            {
#if DEBUG
                ModPlugin.LogMessage("IsSafeToSave returned false.");
#endif
            }

            return false;
        }

        // Monobehaviour.Awake(), called before Start()
        public void Awake()
        {
            this.nextSaveTriggerTick = ModPlugin.ConfigSecondsBetweenAutosaves.Value;

            if (!ModPlugin.ConfigHardcoreMode.Value)
            {
                this.lastUsedAutosaveSlot = this.GetLatestAutosaveForSlot(SaveLoadManager.main.GetCurrentSlot());
            }
        }

        // Monobehaviour.Start
        public void Start()
        {
            this.InvokeRepeating(nameof(AutosaveController.Tick), 1f, 1f);
        }
    }
}
