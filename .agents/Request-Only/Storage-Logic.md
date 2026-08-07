# Storage Logic — Subnautica save/load flow

Reference for snAutosave work. Facts from snAutosave code, SubnauticaMap-newman patch, known game API. No decompiled vanilla source in repo (SN_Source holds DLLs only).

## Core model

- Slot = save identity. Slot name = save folder name.
- Real saves live in SavedGames dir, one folder per slot: `slot0000`, `slot0001`.
- Slot folder holds `gameinfo.json` (metadata: timestamp, mode) + world data.
- Base game: one folder per campaign, manual save only. No autosave.
- Load: game reads slot folder into temp storage.
- Save: game writes temp storage into slot folder (deep storage).
- snAutosave: many folders per slot. `slot0000` + suffix `_autoNNNN` = `slot0000_auto0003`. Same campaign, own folder, own save.

## Vanilla save/load classes

### SaveLoadManager — singleton `SaveLoadManager.main`

- `GetCurrentSlot()` / `SetCurrentSlot(string)` — active slot name. Switching slot redirects where next save lands.
- `SaveGame` — manual save entry (pause menu / PDA).
- `SaveToTemporaryStorageAsync(Texture2D)` — async write of current state into temp storage. Map mod patches this postfix (saves map image alongside).
- `SaveToDeepStorageAsync` — async copy of temp storage into real slot folder. Has `lastSaveTime` guard: skips write when nothing changed.
- Private field `lastSaveTime`.
- Nested state machine type `SaveToDeepStorageAsync` with private `MoveNext()` — actual iterator body.

### IngameMenu — singleton `IngameMenu.main`

- `SaveGame()` — public save. Fires whole save pipeline.
- Private `SaveGameAsync()` — coroutine doing real work. snAutosave invokes via reflection.
- Private `GetAllowSaving` — can-save check. snAutosave invokes via reflection.

### Storage layer

- `PlatformUtils.main.GetUserStorage()` — returns `UserStorage` interface (platform abstraction).
- `UserStoragePC` — PC implementation. Private field `savePath` = real SavedGames directory. snAutosave reflects it: `AccessTools.Field(typeof(UserStoragePC), "savePath")`.
- Save pipeline: temp storage first, deep storage commit second.

### Main menu / load

- `MainMenuLoadPanel.UpdateLoadButtonState(MainMenuLoadButton lb)` — refresh one load button.
- `MainMenuLoadButton` — fields `saveGame` (folder name), `saveGameLengthText` (UI text). `GetGameInfo()` reads metadata.
- `GameInfo` — metadata from `gameinfo.json`.
- `Utils.PrettifyDate(long dateTicks)` — date string on save buttons.
- `WaitScreen.ReportStageDurations()` — last call when loading save finishes. snAutosave uses it: schedule first autosave after load.

### Gameplay hooks

- `Player.Awake` — player spawn.
- `Player.main.GetPDA().isInUse` — PDA open check (block autosave while PDA open).
- `Bed.OnHandClick` — sleep click. `Player.timeLastSleep` — last sleep time (vanilla `kSleepInterval = 600f`).
- `SubRoot.OnPlayerEntered` / `SubRoot.OnPlayerExited` — player enters/exits base or vehicle.
- `FreezeTime.Begin(FreezeTime.Id)` / `FreezeTime.End()` — freeze world during save.
- `CoroutineHost` — vanilla host for save coroutines.

## snAutosave pieces

### AutosaveControllerBase (Shared) — MonoBehaviour on Player

- const `AutosaveSuffixFormat = "_auto{0:0000}"` — folder suffix.
- const `PriorWarningSeconds = 30`.
- `GlobalUserStorage` — `PlatformUtils.main.GetUserStorage()`.
- `SavedGamesDirPath` — reflects `UserStoragePC.savePath`, wraps in `DirectoryInfo`.
- `GetMainSlotName(string currentSlot)` — `currentSlot.Split('_')[0]`. Input `slot0000_auto0003`, output `slot0000`.
- `SlotSuffixFormatted(int slotNumber)` — `string.Format(AutosaveSuffixFormat, n)`.
- `IsAllowedAutosaveSlotNumber(int)` — slot number within limit (MaxSaveFiles 99).
- `GetLatestAutosaveForSlot(string mainSaveSlot)` — scan SavedGamesDirPath for dirs `*{mainSaveSlot}_auto*`, newest `gameinfo.json` LastWriteTime.
- `RotateAutosaveSlotNumber()` — next number for rotation.
- `IsSafePlayerHealth(float minHealthPercent)` — optional health gate.
- `IsSafeToSave()` — private `IngameMenu.GetAllowSaving` (reflection) + PDA closed + health gate.
- `SetMainSlotIfAutosave()` — if current slot name contains "auto": `SetSlot(mainSaveSlot)`. Used before manual saves.
- `AutosaveCoroutine()` — core flow:

  1. `SetMainSlotIfAutosave()` — never autosave from autosave folder.
  2. `mainSaveSlot = SaveLoadManager.main.GetCurrentSlot()`.
  3. If not hardcore: `autosaveSlotName = mainSaveSlot + SlotSuffixFormatted(RotateAutosaveSlotNumber())`; `SetSlot(autosaveSlotName)`.
  4. `FreezeTime.Begin(FreezeTime.Id.None)`.
  5. Invoke private `IngameMenu.SaveGameAsync` (reflection) — vanilla save into temp storage + deep storage, lands in active slot folder.
  6. `yield return saveGameAsync`.
  7. If not hardcore && ComprehensiveSaves: `MirrorTemporarySaveToSlot(mainSaveSlot + SlotSuffixFormatted(latestAutosaveSlot))` — deterministic mod-owned mirror of temp into autosave slot, run after vanilla deep save. Best-effort: per-file failures logged, never fail vanilla result.
  8. If not hardcore: `SetSlot(mainSaveSlot)` — restore main slot.
  9. `ScheduleAutosave()`.
  10. `FreezeTime.End()`.

- `MirrorTemporarySaveToSlot(string autosaveSlotName)` — ComprehensiveSaves mirror. Phase A: `Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories)`, per-file `File.Copy(overwrite: true)` into slot dir, track copied set. Phase B: purge slot files not copied, keep `*.delete-meta` + `*.zip` (temp holds uncompressed rows only). Per-file try/catch → `ModPlugin.LogMessage`.
- `Tick()` — `InvokeRepeating` 1s.
- `ScheduleAutosave(bool settingsChanged, bool showMessage)`, `DelayAutosave(float addedSeconds = 5f)`, `TryExecuteAutosave()` — scheduling.
- Abstract `SetSlot(string newSlot)` — game-specific.

### AutosaveController (Subnautica)

- `SetSlot(string newSlot)` — `SaveLoadManager.main.SetCurrentSlot(newSlot)`. Active slot switch = save folder switch.

### ModPlugin (Subnautica)

- `Awake()` — `LanguageHandler.RegisterLocalizationFolder()`, options register, `HarmonyPatches.InitializeHarmony()`.
- `Update()` — quicksave key -> `IngameMenu.main?.SaveGame()`.
- `LogMessage(string)` — `Debug.Log(modName :: msg)`. All patch error output.

### HarmonyPatches — 8 patch targets, all manual, all try/catch (item 8 logs via BepInEx `logSource.LogError`, rest via `LogMessage`)

1. `Utils.PrettifyDate` prefix — custom date format if option `UseCustomDateFormat`.
2. `IngameMenu.SaveGame` prefix + postfix — prefix: `SetMainSlotIfAutosave()` (manual save always lands in main slot, never autosave folder). Postfix: reschedule autosave if option `DelaySaveOnManual`.
3. `MainMenuLoadPanel.UpdateLoadButtonState` postfix — option `ShowSaveNames`: append `[Auto] slotname` to button text when folder name contains "auto".
4. `Player.Awake` postfix — `AddComponent<AutosaveController>()`.
5. `WaitScreen.ReportStageDurations` postfix — after load: `ScheduleAutosave(showMessage: false)` — first autosave of session.
6. `Bed.OnHandClick` postfix — option `AutosaveOnSleep`; recent-sleep check (`timeLastSleep + 200f > timePassedAsFloat`) then `TryExecuteAutosave()`.
7. `SubRoot.OnPlayerEntered` / `SubRoot.OnPlayerExited` postfix — `DelayAutosave()` (avoid save mid-transition).
8. `UserStoragePC.CopyFilesToContainerAsyncImpl` postfix — after vanilla copy reads `AsyncOperation` from wrapper (state param; `ioThread.Enqueue(delegate, this, wrapper)`), logs via `logSource.LogError` when `result != Success` (includes `errorMessage`). No IL manipulation, no transpiler. Result still Failed, flow untouched.

## Design summary

- snAutosave writes no own save format. Reuses vanilla pipeline.
- Trick: switch active slot via `SaveLoadManager.SetCurrentSlot()` to `slot0000_autoNNNN`, run vanilla `IngameMenu.SaveGameAsync`, switch back to main slot.
- Temp storage -> deep storage commit writes into whichever folder is active. That is how extra folders per slot get created.
- Load menu lists every folder as separate save. `[Auto]` prefix marks autosave folders.
- Manual save guard: prefix on `IngameMenu.SaveGame` restores main slot first — player's manual save always overwrites main campaign folder.
- Hardcore mode: autosave disabled (rotation + slot switch skipped).
- ComprehensiveSaves: mod-owned deterministic mirror of temp storage into autosave slot after vanilla deep save (copy-all + purge, keep `*.delete-meta` + `*.zip`). Vanilla copy stays primary; mirror is best-effort completeness guarantee. Manual saves stay 100% vanilla.
