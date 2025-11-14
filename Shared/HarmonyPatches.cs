using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using HarmonyLib.Tools;

namespace SubnauticaAutosave
{
	public static class HarmonyPatches
	{
		private static bool Patch_PrettifyDate_Prefix(ref string __result, long dateTicks)
		{
			if (ModPlugin.ConfigUseCustomDateFormat.Value)
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
				if (ModPlugin.ConfigDelaySaveOnManual.Value)
				{
					controller.ScheduleAutosave();
				}
			}
		}

		private static IEnumerable<CodeInstruction> Patch_SaveToDeepStorageAsync_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

			try
			{
				FieldInfo configComprehensiveSavesFld = AccessTools.Field(typeof(ModPlugin), nameof(ModPlugin.ConfigComprehensiveSaves));
				MethodInfo configComprehensiveSavesFldValueGetter = AccessTools.PropertyGetter(typeof(ConfigEntry<bool>), "Value");
				FieldInfo lastSaveTimeFld = AccessTools.Field(typeof(SaveLoadManager), "lastSaveTime");
				MethodInfo opGreaterThan = AccessTools.Method(typeof(DateTime), "op_GreaterThan", new[] { typeof(DateTime), typeof(DateTime) });

				for (int i = 0; i < codes.Count; i++)
				{
					if (codes[i].opcode == OpCodes.Ldfld && (FieldInfo)codes[i].operand == lastSaveTimeFld
						&& codes[i + 1].opcode == OpCodes.Call && (MethodInfo)codes[i + 1].operand == opGreaterThan)
					{
						//	codes.Insert(i - 3, new CodeInstruction(OpCodes.Call, compSavesValueGetter));
						codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, configComprehensiveSavesFld));
						codes.Insert(i + 3, new CodeInstruction(OpCodes.Callvirt, configComprehensiveSavesFldValueGetter));
						codes.Insert(i + 4, new CodeInstruction(OpCodes.Or));

//#if DEBUG
//						MethodInfo toStringMethod = AccessTools.Method(typeof(object), "ToString");
//						MethodInfo logMethod = AccessTools.Method(typeof(ModPlugin), nameof(ModPlugin.LogMessage));

//						codes.Insert(i + 5, new CodeInstruction(OpCodes.Dup)); // duplicate result of OR
//						codes.Insert(i + 6, new CodeInstruction(OpCodes.Box, typeof(bool))); // box the bool
//						codes.Insert(i + 7, new CodeInstruction(OpCodes.Callvirt, toStringMethod)); // call ToString()
//						codes.Insert(i + 8, new CodeInstruction(OpCodes.Call, logMethod)); // log it
//#endif
					}
				}
			}
			catch (Exception ex)
			{
				ModPlugin.LogMessage(ex.ToString());
			}

			return codes;
		}

		private static void Patch_UpdateLoadButtonState_Postfix(MainMenuLoadButton lb)
		{
			if (ModPlugin.ConfigShowSaveNames.Value && SaveLoadManager.main.GetGameInfo(lb.saveGame) != null)
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
			if (ModPlugin.ConfigAutosaveOnSleep.Value)
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

		private static IEnumerable<CodeInstruction> Patch_SubnauticaMap_Slot(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo getCurrentSlot = AccessTools.Method(typeof(SaveLoadManager), nameof(SaveLoadManager.GetCurrentSlot));
			MethodInfo getMainSlot = AccessTools.Method(typeof(AutosaveController), nameof(AutosaveController.GetMainSlotDefault));

			return instructions.MethodReplacer(getCurrentSlot, getMainSlot);
		}

		internal static void InitializeHarmony()
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

			/* Save all files option */
#if DEBUG
			HarmonyFileLog.Enabled = true;
			Harmony.DEBUG = true;
#endif
			Type[] nestedTypes = typeof(SaveLoadManager).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			Type saveToDeepStorageStateMachine = nestedTypes.FirstOrDefault(t =>
				t.Name.Contains("SaveToDeepStorageAsync") &&
				t.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance) != null);

			if (saveToDeepStorageStateMachine != null)
			{
				MethodInfo saveToDeepStorageIterator = AccessTools.Method(saveToDeepStorageStateMachine, "MoveNext");

				harmony.Patch(original: saveToDeepStorageIterator,
					transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_SaveToDeepStorageAsync_Transpiler))
#if DEBUG
					{ debug = true }
#endif
					);
			}
		}
	}
}
