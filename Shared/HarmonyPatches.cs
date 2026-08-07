using System;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;

namespace SubnauticaAutosave
{
	public static class HarmonyPatches
	{
		private static ManualLogSource logSource;

		private static bool Patch_PrettifyDate_Prefix(ref string __result, long dateTicks)
		{
			if (ModPlugin.options.UseCustomDateFormat)
			{
				DateTime date = new DateTime(dateTicks);

				__result = Translation.GetCustomDateFormat(date);

				return false;
			}

			return true;
		}

		private static bool Patch_ManualSaveGame_Prefix()
		{
			Player.main?.GetComponent<AutosaveController>()?.SetMainSlotIfAutosave();

			return true;
		}

		private static void Patch_ManualSaveGame_Postfix()
		{
			AutosaveController controller = Player.main?.GetComponent<AutosaveController>();

			if (controller != null)
			{
				if (ModPlugin.options.DelaySaveOnManual)
				{
					controller.ScheduleAutosave();
				}
			}
		}

		private static void Patch_UpdateLoadButtonState_Postfix(MainMenuLoadButton lb)
		{
			if (ModPlugin.options.ShowSaveNames && SaveLoadManager.main.GetGameInfo(lb.saveGame) != null)
			{
				string slotNamePrefix = string.Empty;

				if (lb.saveGame.Contains("auto"))
				{
					slotNamePrefix = "[Auto] ";
				}

				lb.saveGameLengthText.text += "\n\n" + slotNamePrefix + lb.saveGame;
			}
		}

		private static void Patch_Player_Awake_Postfix(Player __instance)
		{
			__instance.gameObject.AddComponent<AutosaveController>();
		}

		private static void Patch_ReportStageDurations_Postfix()
		{
#if DEBUG
			ModPlugin.LogMessage("Main scene loading, scheduling first autosave.");
#endif

			Player.main?.GetComponent<AutosaveController>()?.ScheduleAutosave(showMessage: false);
		}

		// Untested //
		private static void Patch_Bed_OnHandClick_Postfix()
		{
			if (ModPlugin.options.AutosaveOnSleep)
			{
				Player player = Player.main;

				if (player != null)
				{
					/* [Bed.cs] private float kSleepInterval = 600f, added to timeLastSleep when sleep screen ends
                     * If player did indeed sleep recently, we can trigger a save after the bed click
                     * Could instead transpile into OnHandClick if(isValidHandTarget), but no reason to complicate things... */
					if (player.timeLastSleep + 200f > DayNightCycle.main.timePassedAsFloat)
					{
#if DEBUG
						ModPlugin.LogMessage("Player clicked on bed. Executing save on sleep.");
#endif

						player.GetComponent<AutosaveController>()?.TryExecuteAutosave(); // Might want a scheduled save instead
					}
				}
			}
		}

		private static void Patch_Subroot_PlayerEnteredOrExited_Postfix()
		{
#if DEBUG
			ModPlugin.LogMessage("Player entered or exited sub. Delaying autosave.");
#endif

			Player.main?.GetComponent<AutosaveController>()?.DelayAutosave();
		}

		private static void Patch_CopyFilesToContainerAsyncImpl_Postfix(object owner, object state)
		{
			try
			{
				// CopyFilesToContainerWrapper passed via state (ioThread.Enqueue(delegate, this, wrapper)); operation field on base Wrapper
				UserStorageUtils.AsyncOperation operation = FindWrapperOperation(state) ?? FindWrapperOperation(owner);

				if (operation != null && operation.result != UserStorageUtils.Result.Success)
				{
					if (logSource == null)
					{
						logSource = BepInEx.Logging.Logger.CreateLogSource(ModPlugin.modName);
					}

					logSource.LogError($"CopyFilesToContainerAsyncImpl failed: {operation.result} - {operation.errorMessage}");
				}
			}
			catch
			{
				// Never break vanilla save flow
			}
		}

		private static UserStorageUtils.AsyncOperation FindWrapperOperation(object wrapper)
		{
			if (wrapper == null)
			{
				return null;
			}

			for (Type type = wrapper.GetType(); type != null; type = type.BaseType)
			{
				FieldInfo field = type.GetField("operation", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

				if (field != null && field.FieldType == typeof(UserStorageUtils.AsyncOperation))
				{
					return (UserStorageUtils.AsyncOperation)field.GetValue(wrapper);
				}
			}

			return null;
		}

		internal static void InitializeHarmony()
		{
			try
			{
				Harmony harmony = new Harmony("Dingo.Harmony.SubnauticaAutosave");

				/* In the main menu, show user-defined save slot date format */
				// Patch: Utils.PrettifyDate
				harmony.Patch(original: AccessTools.Method(typeof(Utils), nameof(Utils.PrettifyDate)),
					prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_PrettifyDate_Prefix)));

				/* When saving manually, set to main slot if loaded from autosave */
				// Patch: IngameMenu.SaveGame
				harmony.Patch(original: AccessTools.Method(typeof(IngameMenu), nameof(IngameMenu.SaveGame)),
							  prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_ManualSaveGame_Prefix)),
							  postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_ManualSaveGame_Postfix)));

				/* Show save names in menu panel */
				// Patch: MainMenuLoadPanel.UpdateLoadButtonState(MainMenuLoadButton lb)
				harmony.Patch(original: AccessTools.Method(typeof(MainMenuLoadPanel), "UpdateLoadButtonState", new System.Type[] { typeof(MainMenuLoadButton) }),
							  postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_UpdateLoadButtonState_Postfix)));

				/* Autosave Controller initialization */
				// Patch: Player.Awake
				harmony.Patch(original: AccessTools.Method(typeof(Player), nameof(Player.Awake)),
							  postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_Player_Awake_Postfix)));
				// Patch: WaitScreen.ReportStageDurations, called last when loading saved games
				harmony.Patch(original: AccessTools.Method(typeof(WaitScreen), nameof(WaitScreen.ReportStageDurations)),
							  postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_ReportStageDurations_Postfix)));

				/* Save on player sleep */
				// Patch: Bed.OnHandClick
				harmony.Patch(original: AccessTools.Method(typeof(Bed), nameof(Bed.OnHandClick)),
							  postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_Bed_OnHandClick_Postfix)));

				/* Delay autosave if player has entered or exited a base or vehicle */
				HarmonyMethod delayAutosavePatch = new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_Subroot_PlayerEnteredOrExited_Postfix));
				// Patch: SubRoot.OnPlayerEntered
				harmony.Patch(original: AccessTools.Method(typeof(SubRoot), nameof(SubRoot.OnPlayerEntered)),
							  postfix: delayAutosavePatch);
				// Patch: SubRoot.OnPlayerExited
				harmony.Patch(original: AccessTools.Method(typeof(SubRoot), nameof(SubRoot.OnPlayerExited)),
							  postfix: delayAutosavePatch);

				/* Log vanilla copy failures instead of silent catch */
				// Patch: UserStoragePC.CopyFilesToContainerAsyncImpl (postfix, no IL manipulation)
				harmony.Patch(original: AccessTools.Method(typeof(UserStoragePC), "CopyFilesToContainerAsyncImpl"),
							  postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_CopyFilesToContainerAsyncImpl_Postfix)));

			}
			catch (Exception ex)
			{
				ModPlugin.LogMessage($"Harmony patch initialization FAILED: {ex}");
			}
		}
	}
}
