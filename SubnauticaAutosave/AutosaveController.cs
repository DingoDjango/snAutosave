using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UWE;

namespace SubnauticaAutosave
{
	/* Some of the following code is based on "Safe Autosave" by berkay2578
	 * https://www.nexusmods.com/subnautica/mods/94
	 * https://github.com/berkay2578/SubnauticaMods/tree/master/SafeAutosave */

	public class AutosaveController : MonoBehaviour
	{
		private const int RetryTicks = 5;

		private const int PriorWarningTicks = 30;

		private const string AutosaveNameFormat = "autosave_slot{0:0000}";

		private readonly List<string> allowedAutosaveNames = new List<string>();

		private string lastUsedAutosaveName;

		private bool isSaving = false;

		private int totalTicks = 0;

		private int nextSaveTriggerTick = 120;

		private string SlotNameFormatted(int slotNumber)
		{
			// Example output: "autosave_slot0003"
			return string.Format(AutosaveNameFormat, slotNumber);
		}

		private string LastUsedAutosaveFromStorage()
		{
			string savedGamesDir = SNUtils.savedGamesDir;

			if (Directory.Exists(savedGamesDir))
			{
				DirectoryInfo[] saveDirectories = new DirectoryInfo(savedGamesDir).GetDirectories("*slot*", SearchOption.TopDirectoryOnly);

				if (saveDirectories.Count() > 0)
				{
					IOrderedEnumerable<DirectoryInfo> saveSlotsByLastModified = saveDirectories.OrderByDescending(d => d.GetFiles("gameinfo.json")[0].LastWriteTime);

					foreach (DirectoryInfo save in saveSlotsByLastModified)
					{
						if (this.allowedAutosaveNames.Contains(save.Name))
						{
							// The most recent save slot used, which matches the maximum save slots setting
							return save.Name;
						}
					}
				}
			}

			return string.Empty;
		}

		private string NextAutosaveSlotName()
		{
			if (!string.IsNullOrEmpty(this.lastUsedAutosaveName))
			{
				for (int i = 0; i < this.allowedAutosaveNames.Count - 1; i++)
				{
					// Returns slot 0 if the latest autosave was the maximum slot allowed (due to .Count -1)
					if (this.allowedAutosaveNames[i] == this.lastUsedAutosaveName)
					{
						return this.allowedAutosaveNames[i + 1];
					}
				}
			}

			return this.SlotNameFormatted(0);
		}

		private bool IsSafePlayerHealth(float minHealthPercent)
		{
			float playerHealthPercent = Player.main.liveMixin.GetHealthFraction() * 100f;

			return playerHealthPercent >= minHealthPercent;
		}

		// Modified IngameMenu.GetAllowSaving
		private bool IsSafeToSave()
		{
			if (IntroVignette.isIntroActive || LaunchRocket.isLaunching)
			{
#if DEBUG
				Entry.LogMessage($"Did not save, isIntroActive == {IntroVignette.isIntroActive} / isLaunching == {LaunchRocket.isLaunching}");
#endif

				return false;
			}

			if (PlayerCinematicController.cinematicModeCount > 0 && Time.time - PlayerCinematicController.cinematicActivityStart <= 30f)
			{
#if DEBUG
				Entry.LogMessage("Did not save because cinematics are active");
#endif

				return false;
			}

			float safeHealthFraction = Entry.GetConfig.MinimumPlayerHealthPercent;

			if (safeHealthFraction > 0f && !this.IsSafePlayerHealth(safeHealthFraction))
			{
#if DEBUG
				LiveMixin playerLiveMixin = Player.main.liveMixin;

				Entry.LogMessage($"Did not save because player health was {playerLiveMixin.health} / {playerLiveMixin.maxHealth}");
#endif

				return false;
			}

			return !SaveLoadManager.main.isSaving;
		}

		// Modified IngameMenu.SaveGameAsync
		private IEnumerator AutosaveCoroutine()
		{
			this.isSaving = true;

#if DEBUG
			// Close ingame menu if open, used for testing
			IngameMenu.main.Close();
#endif

			ErrorMessage.AddWarning("AutosaveStarting".Translate());
			SaveLoadManager saveManager = SaveLoadManager.main;
			string cachedSlot = (string)typeof(SaveLoadManager).GetField("currentSlot", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(saveManager);

#if DEBUG
			Entry.LogMessage("Cached save slot");
#endif

			yield return null;

			string autosaveSlot = this.NextAutosaveSlotName();
			saveManager.SetCurrentSlot(autosaveSlot);

#if DEBUG
			Entry.LogMessage($"Set custom slot as {autosaveSlot}");
#endif

			yield return null;

			IEnumerator saveGameAsync = (IEnumerator)typeof(IngameMenu).GetMethod("SaveGameAsync", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(IngameMenu.main, null);

			yield return saveGameAsync;

#if DEBUG
			Entry.LogMessage("Executed _SaveGameAsync");
#endif

			saveManager.SetCurrentSlot(cachedSlot);
			this.lastUsedAutosaveName = autosaveSlot;
			int autosaveInterval = Entry.GetConfig.SecondsBetweenAutosaves;
			this.nextSaveTriggerTick += autosaveInterval;

#if DEBUG
			Entry.LogMessage("Updated save slot and trigger tick");
#endif

			yield return null;

			ErrorMessage.AddWarning("AutosaveEnding".FormatTranslate(autosaveInterval.ToString()));
			this.isSaving = false;

#if DEBUG
			Entry.LogMessage("Autosave sequence complete");
#endif

			yield break;
		}

		private void Tick()
		{
			this.totalTicks++;

#if DEBUG
			if (this.totalTicks % 30 == 0)
			{
				Entry.LogMessage($"totalTicks reached {this.totalTicks} ticks");
			}
#endif

			if (this.totalTicks == this.nextSaveTriggerTick - PriorWarningTicks)
			{
#if DEBUG
				Entry.LogMessage("Warning ticks reached, should display an ErrorMessage.");
#endif

				ErrorMessage.AddWarning("AutosaveWarning".FormatTranslate(PriorWarningTicks.ToString()));
			}

			else if (this.totalTicks >= this.nextSaveTriggerTick && !this.isSaving)
			{
				if (this.IsSafeToSave())
				{
					this.ExecuteAutosave();
				}

				else
				{
#if DEBUG
					Entry.LogMessage("IsSafeToSave false. Delaying autosave.");
#endif

					this.DelayAutosave();
				}
			}
		}

		// Monobehaviour.Awake(), called before Start()
		private void Awake()
		{
			if (Entry.GetConfig == null)
			{
				Entry.LogMessage("Main config missing. Trying to load config.");

				ConfigHandler.LoadConfig();
			}

			this.nextSaveTriggerTick = Entry.GetConfig.SecondsBetweenAutosaves;

			for (int i = 0; i < Entry.GetConfig.MaxSaveFiles; i++)
			{
				this.allowedAutosaveNames.Add(this.SlotNameFormatted(i));
			}

			this.lastUsedAutosaveName = this.LastUsedAutosaveFromStorage();

#if DEBUG
			Entry.LogMessage($"SecondsBetweenAutosaves == {Entry.GetConfig.SecondsBetweenAutosaves}");
			Entry.LogMessage($"MaxSaveFiles == {Entry.GetConfig.MaxSaveFiles}");
			Entry.LogMessage($"SafePlayerHealthFraction == {Entry.GetConfig.MinimumPlayerHealthPercent}");
			Entry.LogMessage($"lastUsedAutosaveName == {this.lastUsedAutosaveName}");
#endif
		}

		// Monobehaviour.Start
		private void Start()
		{
			this.InvokeRepeating(nameof(AutosaveController.Tick), 1f, 1f);
		}

		public void DelayAutosave()
		{
			this.nextSaveTriggerTick += RetryTicks;
		}

		public void ExecuteAutosave()
		{
			if (!this.isSaving)
			{
				try
				{
					CoroutineHost.StartCoroutine(this.AutosaveCoroutine());
				}

				catch (Exception ex)
				{
					Entry.LogMessage(ex.ToString());
					Entry.LogMessage("Failed to execute save coroutine. Something went wrong.");
				}
			}

			else
			{
				ErrorMessage.AddWarning("AutosaveInProgress".Translate());
			}
		}
	}
}
