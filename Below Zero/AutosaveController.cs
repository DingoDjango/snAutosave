using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UWE;


namespace SubnauticaAutosave_BZ
{
    /* Some of the following code is based on "Safe Autosave" by berkay2578:
	 * https://www.nexusmods.com/subnautica/mods/94
	 * https://github.com/berkay2578/SubnauticaMods/tree/master/SafeAutosave */

    public class AutosaveController : MonoBehaviour
    {
        private const int PriorWarningSeconds = 30;
        private const string AutosaveSuffixFormat = "_auto{0:0000}";

        private int latestAutosaveSlot = -1;

        private bool isSaving = false;

        private bool warningTriggered = false;

        private float nextSaveTriggerTime = Time.time + 900f;

        private UserStorage GlobalUserStorage => PlatformUtils.main.GetUserStorage();

        private string SavedGamesDirPath
        {
            get
            {
                if (this.GlobalUserStorage == null)
                {
                    return null;
                }

                DirectoryInfo saveDir = new DirectoryInfo((string)AccessTools.Field(typeof(UserStoragePC), "savePath").GetValue(this.GlobalUserStorage));
                string savePath = saveDir.FullName;

#if DEBUG
                HarmonyPatches.LogMessage($"Save path is {savePath}");
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
            HarmonyPatches.LogMessage($"GetAutosaveSlotNumberFromDir returned {slotNumber} for {directoryName}");
#endif

            return slotNumber;
        }

        private bool IsAllowedAutosaveSlotNumber(int slotNumber)
        {
            return slotNumber <= HarmonyPatches.Config.ConfigMaxSaveFiles;
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
                HarmonyPatches.LogMessage($"GetLatestAutosaveForSlot found {saveDirectories.Count()} autosaves for {mainSaveSlot}");
#endif

                if (saveDirectories.Count() > 0)
                {
                    IOrderedEnumerable<DirectoryInfo> saveSlotsByLastModified = saveDirectories.OrderByDescending(d => d.GetFiles("gameinfo.json")[0].LastWriteTime);

                    foreach (DirectoryInfo saveDir in saveSlotsByLastModified)
                    {
                        int autosaveSlotNumber = this.GetAutosaveSlotNumberFromDir(saveDir.Name);

                        if (this.IsAllowedAutosaveSlotNumber(autosaveSlotNumber))
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
                HarmonyPatches.LogMessage($"savedGamesDir == {savedGamesDir}. Could not get save path.");
            }
#endif

            return -1;
        }

        private int RotateAutosaveSlotNumber()
        {
            if (this.latestAutosaveSlot < 0 || this.latestAutosaveSlot >= HarmonyPatches.Config.ConfigMaxSaveFiles)
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
                HarmonyPatches.LogMessage("GetAllowSaving is null, returning false.");

                return false;
            }
#endif

            bool saveAllowed = (bool)getAllowSaving?.Invoke(IngameMenu.main, null);

            if (!saveAllowed)
            {
#if DEBUG
                HarmonyPatches.LogMessage($"Did not save. GetAllowSaving returned {saveAllowed}.");
#endif

                return false;
            }

            /* (Test) Delay save if PDA is in use */
            if (Player.main.GetPDA().isInUse)
            {
                return false;
            }

            /* (Optional) Check if player health is in allowed range */

            float safeHealthFraction = HarmonyPatches.Config.ConfigMinimumPlayerHealthPercent / 100f; // Convert int to fraction

            if (safeHealthFraction > 0f && !this.IsSafePlayerHealth(safeHealthFraction))
            {
                return false;
            }

            return true;
        }

        private IEnumerator AutosaveCoroutine()
        {
#if DEBUG
            HarmonyPatches.LogMessage($"AutosaveCoroutine() - Beginning at {Time.time}.");
#endif

            this.isSaving = true;

            bool hardcoreMode = HarmonyPatches.Config.ConfigHardcoreMode;
            
            // Add autosave permadeath option as well? (bisa) //

            ErrorMessage.AddWarning("AutosaveStarting".Translate());

            yield return null;
            
            if (HarmonyPatches.Config.ConfigComprehensiveSaves)
            {
                /* Trick the game into copying screenshots and other files from temporary save storage.
                 * This will make the game copy every file, which can be slower on old hardware.
                 * Can be considered a beta feature. Keep track of non-forseen consequences... */
                this.DoSaveLoadManagerDateHack();
            }
            
            /* Make sure the main slot is set correctly. 
             * It should always be a clean slot name without _auto */
            this.SetMainSlotIfAutosave(); 

            string mainSaveSlot = SaveLoadManager.main.GetCurrentSlot();

            if (!hardcoreMode)
            {
                string autosaveSlotName = mainSaveSlot + this.SlotSuffixFormatted(this.RotateAutosaveSlotNumber());

                // No idea what Reboot means but I guess it has to do with the Early Access
                SaveLoadManager.main.SetCurrentSlot(autosaveSlotName, SaveLoadManager.StoryVersion.Reboot);
            }

            yield return null;

            // Pause during save
            IngameMenu.main.Open();
            IngameMenu.main.mainPanel.SetActive(false);
            FreezeTime.Begin(FreezeTime.Id.None);

#if DEBUG
            HarmonyPatches.LogMessage("AutosaveCoroutine() - Froze time.");
#endif

            yield return null;

            IEnumerator saveGameAsync = (IEnumerator)typeof(IngameMenu).GetMethod("SaveGameAsync", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(IngameMenu.main, null);

            yield return CoroutineHost.StartCoroutine(saveGameAsync);

#if DEBUG
            HarmonyPatches.LogMessage("AutosaveCoroutine() - saveGameAsync executed.");
#endif

            // Unpause
            IngameMenu.main.Close();
            FreezeTime.End(FreezeTime.Id.None);

#if DEBUG
            HarmonyPatches.LogMessage("AutosaveCoroutine() - Unfroze time.");
#endif

            yield return null;

            if (!hardcoreMode)
            {
                SaveLoadManager.main.SetCurrentSlot(mainSaveSlot, SaveLoadManager.StoryVersion.Reboot);
            }

            if (HarmonyPatches.Config.ConfigAutosaveOnTimer)
            {
                int autosaveMinutesInterval = HarmonyPatches.Config.ConfigMinutesBetweenAutosaves;

                this.ScheduleAutosave(autosaveMinutesInterval);

                ErrorMessage.AddWarning("AutosaveEnding".FormatTranslate(autosaveMinutesInterval.ToString()));
            }

            this.warningTriggered = false;
            this.isSaving = false;

#if DEBUG
            HarmonyPatches.LogMessage("AutosaveCoroutine() - End of routine.");
#endif

            yield break;
        }

        private void Tick()
        {
            if (HarmonyPatches.Config.ConfigAutosaveOnTimer)
            {
                if (!this.warningTriggered && Time.time >= this.nextSaveTriggerTime - PriorWarningSeconds)
                {
                    ErrorMessage.AddWarning("AutosaveWarning".FormatTranslate(PriorWarningSeconds.ToString()));

                    this.warningTriggered = true;
                }

                else if (!this.isSaving && Time.time >= this.nextSaveTriggerTime)
                {
                    if (!this.TryExecuteAutosave())
                    {
#if DEBUG
                        HarmonyPatches.LogMessage("Could not autosave on time. Delaying autosave.");
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

            HarmonyPatches.LogMessage($"Override lastSaveTime. Current value is {currentManagerLastSave}");
#endif
        }

        public void SetMainSlotIfAutosave()
        {
            string currentSlot = SaveLoadManager.main.GetCurrentSlot();

            if (currentSlot.Contains("auto"))
            {
                string mainSaveSlot = this.GetMainSlotName(currentSlot);

                SaveLoadManager.main.SetCurrentSlot(mainSaveSlot, SaveLoadManager.StoryVersion.Reboot);
            }
        }

        public void ScheduleAutosave(int addedMinutes, bool settingsChanged = false)
        {
#if DEBUG
            HarmonyPatches.LogMessage($"ScheduleAutosave() - settingsChanged == {settingsChanged}");
            HarmonyPatches.LogMessage($"ScheduleAutosave() - previous trigger time == {this.nextSaveTriggerTime}");
#endif

            // Time.time returns a float in terms of seconds
            this.nextSaveTriggerTime = Time.time + (60 * addedMinutes);

#if DEBUG
            HarmonyPatches.LogMessage($"ScheduleAutosave() - new trigger time == {this.nextSaveTriggerTime}");
#endif
        }

        public void DelayAutosave(float addedSeconds = 5f)
        {
#if DEBUG
            HarmonyPatches.LogMessage($"DelayAutosave() - previous trigger time == {this.nextSaveTriggerTime}");
#endif

            this.nextSaveTriggerTime += addedSeconds;

#if DEBUG
            HarmonyPatches.LogMessage($"DelayAutosave() - new trigger time == {this.nextSaveTriggerTime}");
#endif
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
                        HarmonyPatches.LogMessage("Failed to execute save coroutine. Something went wrong.");
                        HarmonyPatches.LogMessage(ex.ToString());
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
                HarmonyPatches.LogMessage("IsSafeToSave returned false.");

            }
#endif

            return false;
        }

        // Monobehaviour.Awake(), called before Start()
        public void Awake()
        {
#if DEBUG
            HarmonyPatches.LogMessage($"AutosaveController.Awake() - Initial save trigger set to {this.nextSaveTriggerTime}");
#endif

            if (!HarmonyPatches.Config.ConfigHardcoreMode)
            {
                this.latestAutosaveSlot = this.GetLatestAutosaveForSlot(SaveLoadManager.main.GetCurrentSlot());
            }

#if DEBUG            
            HarmonyPatches.LogMessage($"AutosaveController.Awake() - Latest autosave for {SaveLoadManager.main.GetCurrentSlot()} set to {this.latestAutosaveSlot}");
#endif
        }

        // Monobehaviour.Start
        public void Start()
        {
            // Repeat the Tick method every second
            this.InvokeRepeating(nameof(AutosaveController.Tick), 1f, 1f);
        }

        public void Update()
        {
            if (GameInput.GetKeyDown(HarmonyPatches.Config.ConfigQuicksaveKey))
            {
                IngameMenu.main?.SaveGame();
            }

#if DEBUG
            if (GameInput.GetKeyDown(KeyCode.LeftBracket))
            {
                HarmonyPatches.LogMessage("Pressed [ key, trying to execute autosave");

                Player.main?.GetComponent<AutosaveController>()?.TryExecuteAutosave();
            }
#endif
        }
    }
}
