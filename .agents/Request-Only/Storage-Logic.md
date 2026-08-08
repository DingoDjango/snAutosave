# Storage Logic — Subnautica save/load flow

Reference for snAutosave work. Facts verified against decompiled vanilla source (`SN_Source/Subnautica_Assembly/Assembly-CSharp` + `-firstpass`), Nautilus source (`Nautilus/Nautilus`), current snAutosave code. Line refs vanilla files.

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
- `SaveToTemporaryStorageAsync(Texture2D)` public — async write of current state into temp storage. Self-freezes `FreezeTime.Id.Save`, sets `isSaving=true`, pushes input handler. Map mod patches this postfix (saves map image alongside) — see `SubnauticaMap-newman/SaveLoadManager_SaveToTemporaryStorageAsync_Patch.cs`.
- `SaveToDeepStorageAsync()` public — copies temp storage into active slot folder (`CopyFilesToContainerAsync(currentSlot, tempPath, updated, deleted, unchanged)`). Sets `isSaving=true` during. On success updates `lastSaveTime = DateTime.Now`.
- Diff logic: files newer than `lastSaveTime` = updated; `.deleted` markers in temp → target files deleted from slot; older files unchanged. NOT "skip save when nothing changed".
- `isSaving` public (`SaveLoadManager.cs:304`), `notificationSaveInProgress` event public (`:336`).
- `GetTemporarySavePath()` static public (`:393`). `ScreenshotManager.Initialize(GetTemporarySavePath())` — screenshots live in TEMP, not slot.
- Delete-meta = `{file}.deleted` (`GetDeleteMetaFileName` `:565`, `IsDeleteMetaFileName` `:575`, `GetFileNameForDeleteMetaFileName` `:570`). Markers created in temp by: `DeleteFileInTemporaryStorage` (`:941`), `BatchUpgrade` batch-object cleanup (`BatchUpgrade.cs:32`), `ScreenshotManager` (`ScreenshotManager.cs:404`). Deep storage turns markers into slot deletions + tombstones.
- Private state machine iterators (`SaveToTemporaryStorageAsync`/`SaveToDeepStorageAsync` bodies) — compiler-generated `MoveNext()`. Mod does not touch these directly.
- `SanityCheck` (`:430`) — fails when `isSaving`/`isLoading`; public save calls rely on it.

### IngameMenu — singleton `IngameMenu.main`

- `SaveGame()` public — starts `SaveGameAsync` on `CoroutineHost` (`:388-391`).
- Private `SaveGameAsync()` iterator (`:450`) — menu panel dance, screenshot capture, temp save, deep save, error popup (`ReportSaveError` `:424`), analytics. snAutosave invokes via reflection.
- Private `GetAllowSaving()` (`:206`) — cinematics/intro/`Player.allowSaving`/`SaveLoadManager.isSaving` gate. snAutosave invokes via reflection.

### Storage layer

- `PlatformUtils.main.GetUserStorage()` — returns `UserStorage` interface (platform abstraction).
- `UserStoragePC` — PC implementation. Private field `savePath` = real SavedGames directory. snAutosave reflects it: `AccessTools.Field(typeof(UserStoragePC), "savePath")`. Alternative public path: `ScreenshotManager.savePath` is TEMP, not SavedGames — do not use for slot dir.
- Save pipeline: temp storage first, deep storage commit second.

### Main menu / load

- `MainMenuLoadPanel.UpdateLoadButtonState(MainMenuLoadButton)` private (`MainMenuLoadPanel.cs:77`). Duplicate private copy in `MainMenuSaveMigrationPanel.cs:98`.
- `MainMenuLoadButton` — fields `saveGame` (folder name), `saveGameLengthText` (UI text). `GetGameInfo()` reads metadata via `ILoadButtonDelegate`.
- `SaveLoadManager.GameInfo` — metadata from `gameinfo.json`.
- `Utils.PrettifyDate(long dateTicks)` (`Utils.cs:1612`) — date string on save buttons.
- `WaitScreen.ReportStageDurations()` (`WaitScreen.cs:187`) — called when main scene load finishes (`MainSceneLoading.cs:33`). snAutosave uses it: schedule first autosave after load.

### Gameplay hooks

- `Player.Awake` — player spawn.
- `Player.main.GetPDA().isInUse` — PDA open check (block autosave while PDA open).
- `Bed.OnHandClick` — sleep click. `Player.timeLastSleep`, vanilla `Bed.kSleepInterval = 600f` (`Bed.cs:285`).
- `SubRoot.OnPlayerEntered(Player)` / `SubRoot.OnPlayerExited(Player)` (`SubRoot.cs:371/428`) — player enters/exits base or vehicle.
- `FreezeTime.Begin(Id)` / `End(Id)` (`Assembly-CSharp-firstpass/UWE/FreezeTime.cs`) — freeze world during save. Vanilla freezes `Id.Save` inside `SaveToTemporaryStorageAsync`. `Id.None` = empty info, no audio bus.
- `CoroutineHost` (`Assembly-CSharp-firstpass/UWE/CoroutineHost.cs`) — vanilla host for save coroutines, static `StartCoroutine`.

## snAutosave pieces

### AutosaveControllerBase (Shared) — MonoBehaviour on Player

- const `AutosaveSuffixFormat = "_auto{0:0000}"` — folder suffix.
- const `PriorWarningSeconds = 30`.
- `GlobalUserStorage` — `PlatformUtils.main.GetUserStorage()`.
- `SavedGamesDirPath` — reflects `UserStoragePC.savePath`, wraps in `DirectoryInfo`.
- `GetMainSlotName(string currentSlot)` — `currentSlot.Split('_')[0]`. Input `slot0000_auto0003`, output `slot0000`.
- `SlotSuffixFormatted(int slotNumber)` — `string.Format(AutosaveSuffixFormat, n)`.
- `IsAllowedAutosaveSlotNumber(int)` — slot number within limit (MaxSaveFiles 99).
- `GetLatestAutosaveForSlot(string mainSaveSlot)` — scan SavedGamesDirPath for dirs `*{mainSaveSlot}_auto*`, newest `gameinfo.json` LastWriteTime. RISK: `GetFiles("gameinfo.json")[0]` — IndexOutOfRange if dir lacks file.
- `RotateAutosaveSlotNumber()` — next number for rotation.
- `IsSafePlayerHealth(float minHealthPercent)` — optional health gate.
- `IsSafeToSave()` — private `IngameMenu.GetAllowSaving` (reflection) + PDA closed + health gate.
- `SetMainSlotIfAutosave()` — if current slot name contains "auto": `SetSlot(mainSaveSlot)`. Used before manual saves.
- `AutosaveCoroutine()` — core flow:

  1. `SetMainSlotIfAutosave()` — never autosave from autosave folder.
  2. `mainSaveSlot = SaveLoadManager.main.GetCurrentSlot()`.
  3. If not hardcore: `autosaveSlotName = mainSaveSlot + SlotSuffixFormatted(RotateAutosaveSlotNumber())`; `SetSlot(autosaveSlotName)`.
  4. `FreezeTime.Begin(FreezeTime.Id.None)` — vanilla re-freezes `Id.Save` inside temp save; outer freeze not required, no try/finally guard.
  5. Invoke private `IngameMenu.SaveGameAsync` (reflection) — vanilla save into temp storage + deep storage, lands in active slot folder.
  6. `yield return saveGameAsync`.
  7. If not hardcore && ComprehensiveSaves: `MirrorTemporarySaveToSlot(mainSaveSlot + SlotSuffixFormatted(latestAutosaveSlot))` — deterministic mod-owned mirror of temp into autosave slot, run after vanilla deep save. Best-effort: per-file failures logged, never fail vanilla result.
  8. If not hardcore: `SetSlot(mainSaveSlot)` — restore main slot.
  9. `ScheduleAutosave()`.
  10. `FreezeTime.End()`.

- `MirrorTemporarySaveToSlot(string autosaveSlotName)` — ComprehensiveSaves mirror. Phase A: `Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories)`, per-file `File.Copy(overwrite: true)` into slot dir, track copied set. Phase B: purge slot files not copied, keep `.deleted` (vanilla delete-meta, via `SaveLoadManager.IsDeleteMetaFileName`) + `.zip` (batch-cell bundles; temp holds uncompressed rows only). Per-file try/catch → `ModPlugin.LogMessage`. Temp `.deleted` markers are copied in Phase A → kept. Purge risk: slot tombstones absent from temp.
- `Tick()` — `InvokeRepeating` 1s.
- `ScheduleAutosave(bool settingsChanged, bool showMessage)`, `DelayAutosave(float addedSeconds = 5f)`, `TryExecuteAutosave()` — scheduling.
- Abstract `SetSlot(string newSlot)` — game-specific.
- Messages: `AutosaveWarning` (30s before), `AutosaveEnding` (after schedule), `AutosaveInProgress` (blocked retry). "AutosaveStarting" removed.

### AutosaveController (Subnautica)

- `SetSlot(string newSlot)` — `SaveLoadManager.main.SetCurrentSlot(newSlot)`. Active slot switch = save folder switch.

### ModPlugin (Subnautica)

- `Awake()` — `LanguageHandler.RegisterLocalizationFolder()`, options register, `HarmonyPatches.InitializeHarmony()`.
- `Update()` — quicksave key -> `IngameMenu.main?.SaveGame()`. Not gated on `SaveLoadManager.isSaving`.
- `LogMessage(string)` — `Debug.Log(modName :: msg)`. All patch error output.

### HarmonyPatches — 8 patch targets, all manual, all try/catch (item 8 logs via BepInEx `logSource.LogError`, rest via `LogMessage`)

1. `Utils.PrettifyDate` prefix — custom date format if option `UseCustomDateFormat`.
2. `IngameMenu.SaveGame` prefix + postfix — prefix: `SetMainSlotIfAutosave()` (manual save always lands in main slot, never autosave folder). Postfix: reschedule autosave if option `DelaySaveOnManual`.
3. `MainMenuLoadPanel.UpdateLoadButtonState` postfix — option `ShowSaveNames`: append `[Auto] slotname` to button text when folder name contains "auto".
4. `Player.Awake` postfix — `AddComponent<AutosaveController>()`.
5. `WaitScreen.ReportStageDurations` postfix — after load: `ScheduleAutosave(showMessage: false)` — first autosave of session.
6. `Bed.OnHandClick` postfix — option `AutosaveOnSleep`; recent-sleep check (`timeLastSleep + 200f > timePassedAsFloat`) then `TryExecuteAutosave()`. Untested.
7. `SubRoot.OnPlayerEntered` / `SubRoot.OnPlayerExited` postfix — `DelayAutosave()` (avoid save mid-transition).
8. `UserStoragePC.CopyFilesToContainerAsyncImpl` postfix — after vanilla copy reads `AsyncOperation` from wrapper (state param; `ioThread.Enqueue(delegate, this, wrapper)`), logs via `logSource.LogError` when `result != Success` (includes `errorMessage`). No IL manipulation, no transpiler. Result still Failed, flow untouched.

## Nautilus integration

- Options: `OptionsPanelHandler.RegisterModOptions<AutosaveOptions>()` + Nautilus `ConfigFile` with `[Toggle]/[Slider]/[Keybind]` + `[OnChange]`. Correct pattern.
- Localization: `LanguageHandler.RegisterLocalizationFolder()` loads `Shared/Localization/*.json`. `Translation.cs` wrapper caches + falls back to English via `LanguagePatcher.RepatchCheck`.
- Nautilus has NO save/autosave API. Save triggering stays vanilla (`SaveLoadManager`/`IngameMenu`). Do not invent Nautilus save hooks.

## Design summary

- snAutosave writes no own save format. Reuses vanilla pipeline.
- Trick: switch active slot via `SaveLoadManager.SetCurrentSlot()` to `slot0000_autoNNNN`, run vanilla `IngameMenu.SaveGameAsync`, switch back to main slot.
- Temp storage -> deep storage commit writes into whichever folder is active. That is how extra folders per slot get created.
- Load menu lists every folder as separate save. `[Auto]` prefix marks autosave folders.
- Manual save guard: prefix on `IngameMenu.SaveGame` restores main slot first — player's manual save always overwrites main campaign folder.
- Hardcore mode: autosave disabled (rotation + slot switch skipped).
- ComprehensiveSaves: mod-owned deterministic mirror of temp storage into autosave slot after vanilla deep save (copy-all + purge, keep `.deleted` + `.zip`). Vanilla copy stays primary; mirror is best-effort completeness guarantee. Manual saves stay 100% vanilla.
