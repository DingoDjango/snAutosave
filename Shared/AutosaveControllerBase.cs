using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;
using UWE;

namespace SubnauticaAutosave
{
    /* Some of the following code is based on "Safe Autosave" by berkay2578:
     * https://www.nexusmods.com/subnautica/mods/94
     * https://github.com/berkay2578/SubnauticaMods/tree/master/SafeAutosave */

    public abstract class AutosaveControllerBase : MonoBehaviour
    {
        private const float InitialSaveDelaySeconds = 900f;

        private static readonly FieldInfo SavePathField = AccessTools.Field(typeof(UserStoragePC), "savePath");

        private static readonly FieldInfo BuilderToolIsConstructingField = AccessTools.Field(typeof(BuilderTool), "isConstructing");

        private static readonly MethodInfo GetAllowSavingMethod = AccessTools.Method(typeof(IngameMenu), "GetAllowSaving");

        private static readonly string[] BackupSuffixes = { "-old", "-old(1)", "-old(2)", "-old(3)", "-old(4)", "-old(5)", "-old(6)", "-old(7)", "-old(8)", "-old(9)" };

        public const string AutosaveSuffixFormat = "_auto{0:0000}";

        // Set by IngameMenu.ReportSaveError patch, cleared before invoke, read after yield.
        internal static SaveLoadManager.SaveResult lastSaveResult = null;

        // Blocks new autosave triggers while the error report popup is open.
        internal static bool awaitingConfirmation = false;

        protected int latestAutosaveSlot = -1;

        protected bool isSaving = false;

        protected bool warningTriggered = false;

        protected float nextSaveTriggerTime = Time.time + InitialSaveDelaySeconds;

        public UserStorage GlobalUserStorage => PlatformUtils.main?.GetUserStorage();

        public string SavedGamesDirPath
        {
            get
            {
                UserStorage globalUserStorage = this.GlobalUserStorage;

                if (globalUserStorage == null || SavePathField == null)
                {
                    return null;
                }

                DirectoryInfo saveDir = new DirectoryInfo((string)SavePathField.GetValue(globalUserStorage));
                string savePath = saveDir.FullName;

#if DEBUG
                ModPlugin.LogMessage($"Save path is {savePath}");
#endif

                return savePath;
            }
        }

        private bool GetAllowSavingExtra()
        {
            if (Player.main.GetPDA().isInUse)
            {
                return false;
            }

            // Vanilla GetAllowSaving has 60s timeout escape; block mid-cinematic saves
            if (PlayerCinematicController.cinematicModeCount > 0)
            {
                return false;
            }

            // Respawn screen outlives vanilla 5s allowSaving grace
            if (!Player.main.liveMixin.IsAlive())
            {
                return false;
            }

            // Vehicles: piloting, enter/exit, docked
            if (!ModPlugin.options.SaveWhilePiloting && Player.main.isPiloting)
            {
                return false;
            }

            // Builder menu open or ghost placing; menu closes on save → janky camera
            // Actively constructing existing piece (build beam active)
            if (Builder.isPlacing || uGUI_BuilderMenu.IsOpen() ||
                (Inventory.main.GetHeldTool() is BuilderTool buildTool &&
                 BuilderToolIsConstructingField != null &&
                 (bool)BuilderToolIsConstructingField.GetValue(buildTool)))
            {
                return false;
            }

            // Any UI input group active: sign/subname/console/crafting/builder menu etc.
            if (FPSInputModule.current != null && FPSInputModule.current.lastGroup != null)
            {
                return false;
            }

            return true;
        }

        private bool GetAllowSavingOptionals()
        {
            float safeHealthFraction = ModPlugin.options.MinimumPlayerHealthPercent;

            if (safeHealthFraction > 0f && !this.IsSafePlayerHealth(safeHealthFraction))
            {
                return false;
            }

            return true;
        }

        private IEnumerator AutosaveCoroutine()
        {
#if DEBUG
            ModPlugin.LogMessage($"AutosaveCoroutine() - Beginning at {Time.time}.");
#endif

            this.isSaving = true;

            this.SetMainSlotIfAutosave();

            string mainSaveSlot = SaveLoadManager.main.GetCurrentSlot();

            string backupPath = null;
            bool abort = false;

            FreezeTime.Begin(FreezeTime.Id.None);

            Exception failure = null;

            try
            {
#if DEBUG
                ModPlugin.LogMessage("AutosaveCoroutine() - Froze time.");
#endif

                lastSaveResult = null;

                if (!ModPlugin.options.HardcoreMode)
                {
                    string autosaveSlotName = mainSaveSlot + this.SlotSuffixFormatted(this.RotateAutosaveSlotNumber());

                    this.SetSlot(autosaveSlotName);

                    backupPath = this.PrepareAutosaveSlot(autosaveSlotName, out abort);
                }

                if (!abort)
                {
                    IEnumerator saveGameAsync = null;

                    try
                    {
                        saveGameAsync = (IEnumerator)typeof(IngameMenu).GetMethod("SaveGameAsync", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(IngameMenu.main, null);
                    }
                    catch (Exception e)
                    {
                        failure = e;
                    }

                    if (failure == null)
                    {
                        yield return saveGameAsync;

#if DEBUG
                        ModPlugin.LogMessage("AutosaveCoroutine() - saveGameAsync executed.");
#endif

                        try
                        {
                            this.ScheduleAutosave();

#if DEBUG
                            ModPlugin.LogMessage("AutosaveCoroutine() - End of routine.");
#endif
                        }
                        catch (Exception e)
                        {
                            failure = e;
                        }
                    }
                }
            }
            finally
            {
                // always restore slot + unpause + reset state
                this.SetSlot(mainSaveSlot);

                FreezeTime.End(FreezeTime.Id.None);

                this.warningTriggered = false;

                this.isSaving = false;
            }

            bool saveFailed = failure != null || (lastSaveResult != null && !lastSaveResult.success);

            if (!saveFailed)
            {
                if (!string.IsNullOrEmpty(backupPath))
                {
                    this.DeleteBackupAsync(backupPath);
                }
            }
            else
            {
                string errorCode = lastSaveResult != null ? lastSaveResult.error.ToString() : "None";
                string errorMessage = lastSaveResult != null ? lastSaveResult.errorMessage : null;

                ModPlugin.LogMessage($"AutosaveCoroutine() - Autosave failure (exception: {failure}, error: {errorCode}, message: {errorMessage}). Backup kept: {backupPath}");
            }
        }

        // Renames the existing slot dir to the next free backup name, then pre-seeds a fresh slot.
        // Returns this cycle's backupPath (null = abort, or no backup created on a fresh slot).
        private string PrepareAutosaveSlot(string autosaveSlotName, out bool abort)
        {
            abort = false;

            string slotPath = Path.Combine(this.SavedGamesDirPath, autosaveSlotName);
            string tempPath = SaveLoadManager.GetTemporarySavePath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            try
            {
                if (!Directory.Exists(slotPath))
                {
                    // fresh slot: pre-seed from temp, no backup this cycle
                    this.PreSeedTemporarySaveToSlot(tempPath, slotPath);

                    return null;
                }

                string newestBackup = this.FindNewestBackup(slotPath);

                if (newestBackup != null && AutosaveControllerBase.GameInfoIdentical(Path.Combine(slotPath, "gameinfo.json"), Path.Combine(newestBackup, "gameinfo.json")))
                {
                    ModPlugin.LogMessage($"PrepareAutosaveSlot() - Slot {autosaveSlotName} matches newest backup {newestBackup}. Aborting autosave, backups kept.");

                    this.ShowAutosaveWarning();

                    abort = true;

                    return null;
                }

                string backupPath = this.NextFreeBackup(slotPath);

                if (backupPath == null)
                {
                    ModPlugin.LogMessage($"PrepareAutosaveSlot() - No free backup name for {autosaveSlotName} (10 max). Aborting autosave, backups kept.");

                    this.ShowAutosaveWarning();

                    abort = true;

                    return null;
                }

                Directory.Move(slotPath, backupPath);

#if DEBUG
                ModPlugin.LogMessage($"PrepareAutosaveSlot() - Renamed {slotPath} to {backupPath}.");
#endif

                this.PreSeedTemporarySaveToSlot(tempPath, slotPath);

                return backupPath;
            }
            catch (Exception ex)
            {
                ModPlugin.LogMessage($"PrepareAutosaveSlot() - Failed for {slotPath}: {ex}");

                this.ShowAutosaveWarning();

                abort = true;

                return null;
            }
        }

        // Highest-numbered existing backup: -old, -old(1) ... -old(9). Returns null if none.
        private string FindNewestBackup(string slotPath)
        {
            string newestBackup = null;

            foreach (string backupPath in BackupSuffixes)
            {
                string candidate = slotPath + backupPath;

                if (Directory.Exists(candidate))
                {
                    newestBackup = candidate;
                }
            }

            return newestBackup;
        }

        // First free backup name. Returns null when all 10 are taken.
        private string NextFreeBackup(string slotPath)
        {
            foreach (string backupPath in BackupSuffixes)
            {
                string candidate = slotPath + backupPath;

                if (!Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        // Byte-compares gameinfo.json of two dirs. Missing file on either side → not identical.
        private static bool GameInfoIdentical(string firstGameInfoPath, string secondGameInfoPath)
        {
            if (!File.Exists(firstGameInfoPath) || !File.Exists(secondGameInfoPath))
            {
                return false;
            }

            try
            {
                byte[] firstBytes = File.ReadAllBytes(firstGameInfoPath);
                byte[] secondBytes = File.ReadAllBytes(secondGameInfoPath);

                if (firstBytes.Length != secondBytes.Length)
                {
                    return false;
                }

                for (int i = 0; i < firstBytes.Length; i++)
                {
                    if (firstBytes[i] != secondBytes[i])
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                ModPlugin.LogMessage($"GameInfoIdentical() - Failed to compare {firstGameInfoPath} and {secondGameInfoPath}: {ex}");

                return false;
            }
        }

        // Recursive copy of ALL temp files into the slot dir (rows, screenshots, cyclops, anything).
        private void PreSeedTemporarySaveToSlot(string tempPath, string slotPath)
        {
            if (!Directory.Exists(tempPath))
            {
                ModPlugin.LogMessage($"PreSeedTemporarySaveToSlot() - Temporary save folder does not exist: {tempPath}");

                return;
            }

            Directory.CreateDirectory(slotPath);

            int copiedFiles = 0;

            foreach (string tempFile in Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories))
            {
                try
                {
                    string relativePath = tempFile.Substring(tempPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string destinationPath = Path.Combine(slotPath, relativePath);

                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                    File.Copy(tempFile, destinationPath, true);

                    copiedFiles++;
                }
                catch (Exception ex)
                {
                    ModPlugin.LogMessage($"PreSeedTemporarySaveToSlot() - Failed to copy {tempFile}: {ex}");
                }
            }

#if DEBUG
            ModPlugin.LogMessage($"PreSeedTemporarySaveToSlot() - Pre-seeded {copiedFiles} files from {tempPath} to {slotPath}.");
#endif
        }

        // Background delete of THIS cycle's backup only, off the main thread. Older backups never touched.
        private void DeleteBackupAsync(string backupPath)
        {
            try
            {
                System.Threading.ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        Directory.Delete(backupPath, true);

#if DEBUG
                        ModPlugin.LogMessage($"DeleteBackupAsync() - Deleted backup {backupPath}.");
#endif
                    }
                    catch (Exception ex)
                    {
                        ModPlugin.LogMessage($"DeleteBackupAsync() - Failed to delete backup {backupPath}: {ex}");
                    }
                });
            }
            catch (Exception ex)
            {
                ModPlugin.LogMessage($"DeleteBackupAsync() - Failed to queue deletion of {backupPath}: {ex}");
            }
        }

        // Vanilla-style modal (mirrors IngameMenu.ReportSaveError). Freeze id separate from mod's Id.None.
        private void ShowAutosaveWarning()
        {
            if (awaitingConfirmation)
            {
                return;
            }

            awaitingConfirmation = true;

            FreezeTime.Begin(FreezeTime.Id.IngameMenu);

            uGUI.main.confirmation.Show("AutosaveErrorReport".Translate(), new uGUI_SceneConfirmation.ConfirmationFinishedDelegate(this.OnAutosaveWarningConfirmed), null);

#if DEBUG
            ModPlugin.LogMessage("ShowAutosaveWarning() - Autosave error report popup shown.");
#endif
        }

        private void OnAutosaveWarningConfirmed(bool confirmed)
        {
            FreezeTime.End(FreezeTime.Id.IngameMenu);

            awaitingConfirmation = false;

            ModPlugin.LogMessage($"OnAutosaveWarningConfirmed() - Autosave error report acknowledged (confirmed: {confirmed}).");
        }

        public string SlotSuffixFormatted(int slotNumber)
        {
            // Example output: "_auto0003"
            return string.Format(AutosaveSuffixFormat, slotNumber);
        }

        public string GetCurrentMainSlot()
        {
            return this.GetMainSlotName(SaveLoadManager.main.GetCurrentSlot());
        }

        public string GetMainSlotName(string currentSlot)
        {
            // Input:   slot0000_auto0001
            // Output:  slot0000
            return currentSlot.Split('_')[0];
        }

        public int GetAutosaveSlotNumberFromDir(string directoryName)
        {
            string slotPart = directoryName.Split(new[] { "auto" }, StringSplitOptions.None).Last();
            int slotNumber;

            if (!int.TryParse(slotPart, out slotNumber))
            {
#if DEBUG
                ModPlugin.LogMessage($"GetAutosaveSlotNumberFromDir could not parse {directoryName}");
#endif

                return -1;
            }

            return slotNumber;
        }

        public bool IsAllowedAutosaveSlotNumber(int slotNumber)
        {
            return slotNumber <= ModPlugin.options.MaxSaveFiles;
        }

        public int GetLatestAutosaveForSlot(string mainSaveSlot)
        {
            if (mainSaveSlot.Contains("auto"))
            {
                mainSaveSlot = this.GetMainSlotName(mainSaveSlot);
            }

            string savedGamesDir = this.SavedGamesDirPath;
            string searchPattern = "*" + mainSaveSlot + "_auto" + "*";

            if (!string.IsNullOrEmpty(savedGamesDir) && Directory.Exists(savedGamesDir))
            {
                DirectoryInfo[] saveDirectories = new DirectoryInfo(savedGamesDir).GetDirectories(searchPattern, SearchOption.TopDirectoryOnly);

#if DEBUG
                ModPlugin.LogMessage($"GetLatestAutosaveForSlot found {saveDirectories.Length} autosaves for {mainSaveSlot}");
#endif

                if (saveDirectories.Length > 0)
                {
                    // Skip interrupted saves (no gameinfo.json)
                    IEnumerable<DirectoryInfo> saveSlotsByLastModified = saveDirectories
                        .Select(d => new { Dir = d, Info = d.GetFiles("gameinfo.json").FirstOrDefault() })
                        .Where(x => x.Info != null)
                        .OrderByDescending(x => x.Info.LastWriteTime)
                        .Select(x => x.Dir);

                    foreach (DirectoryInfo saveDir in saveSlotsByLastModified)
                    {
                        int autosaveSlotNumber = this.GetAutosaveSlotNumberFromDir(saveDir.Name);

                        if (autosaveSlotNumber <= 0)
                        {
                            continue;
                        }

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
                ModPlugin.LogMessage($"savedGamesDir == {savedGamesDir}. Could not get save path.");
            }
#endif

            return -1;
        }

        public int RotateAutosaveSlotNumber()
        {
            if (this.latestAutosaveSlot < 0 || this.latestAutosaveSlot >= ModPlugin.options.MaxSaveFiles)
            {
                this.latestAutosaveSlot = 1;
            }
            else
            {
                this.latestAutosaveSlot++;
            }

            return this.latestAutosaveSlot;
        }

        public bool IsSafePlayerHealth(float minHealthPercent)
        {
#if DEBUG
            ModPlugin.LogMessage($"Setting minHealthPercent returned {minHealthPercent}");
#endif

            return Player.main.liveMixin.GetHealthFraction() >= minHealthPercent;
        }

        public bool IsSafeToSave()
        {
            /* vanilla checks (cinematics, saving status) */

            bool saveAllowed = (bool)GetAllowSavingMethod?.Invoke(IngameMenu.main, null);

#if DEBUG
            if (GetAllowSavingMethod == null)
            {
                ModPlugin.LogMessage("GetAllowSaving is null, returning false.");

                return false;
            }
#endif

            if (!saveAllowed)
            {
#if DEBUG
                ModPlugin.LogMessage($"Did not save. GetAllowSaving returned {saveAllowed}.");
#endif

                return false;
            }

            return this.GetAllowSavingExtra() && this.GetAllowSavingOptionals();
        }

        public void Tick()
        {
            if (ModPlugin.options.AutosaveOnTimer)
            {
                int priorWarningSeconds = ModPlugin.options.AutosaveWarningTime;

                if (ModPlugin.options.ShowSaveMessages && !this.warningTriggered && Time.time >= this.nextSaveTriggerTime - priorWarningSeconds)
                {
                    ErrorMessage.AddWarning("AutosaveWarning".FormatTranslate(priorWarningSeconds.ToString()));

                    this.warningTriggered = true;
                }

                else if (!this.isSaving && Time.time >= this.nextSaveTriggerTime)
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

        public void SetMainSlotIfAutosave()
        {
            string currentSlot = SaveLoadManager.main.GetCurrentSlot();

            if (currentSlot.Contains("auto"))
            {
                string mainSaveSlot = this.GetMainSlotName(currentSlot);

                this.SetSlot(mainSaveSlot);
            }
        }

        public void ScheduleAutosave(bool settingsChanged = false, bool showMessage = true)
        {
            if (ModPlugin.options.AutosaveOnTimer)
            {
                int addedMinutes = ModPlugin.options.MinutesBetweenAutosaves;

#if DEBUG
                ModPlugin.LogMessage($"ScheduleAutosave() - settingsChanged == {settingsChanged}");
                ModPlugin.LogMessage($"ScheduleAutosave() - previous trigger time == {this.nextSaveTriggerTime}");
#endif

                // Time.time returns a float in terms of seconds
                this.nextSaveTriggerTime = Time.time + (60 * addedMinutes);

#if DEBUG
                ModPlugin.LogMessage($"ScheduleAutosave() - new trigger time == {this.nextSaveTriggerTime}");
#endif
                if (ModPlugin.options.ShowSaveMessages && showMessage)
                {
                    ErrorMessage.AddWarning("AutosaveEnding".FormatTranslate(addedMinutes.ToString()));
                }
            }
        }

        public void DelayAutosave(float addedSeconds = 5f)
        {
#if DEBUG
            ModPlugin.LogMessage($"DelayAutosave() - previous trigger time == {this.nextSaveTriggerTime}");
#endif

            this.nextSaveTriggerTime += addedSeconds;

#if DEBUG
            ModPlugin.LogMessage($"DelayAutosave() - new trigger time == {this.nextSaveTriggerTime}");
#endif
        }

        public bool TryExecuteAutosave()
        {
            if (awaitingConfirmation)
            {
                return false;
            }

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
#if DEBUG
            ModPlugin.LogMessage($"AutosaveController.Awake() - Initial save trigger set to {this.nextSaveTriggerTime}");
#endif

            if (!ModPlugin.options.HardcoreMode)
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
            this.InvokeRepeating(nameof(AutosaveControllerBase.Tick), 1f, 1f);
        }

        public abstract void SetSlot(string newSlot);
    }
}
