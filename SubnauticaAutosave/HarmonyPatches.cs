using System.Reflection;
using HarmonyLib;

namespace SubnauticaAutosave
{
	public class HarmonyPatches
	{
#if DEBUG
		private static bool TestPatch_SaveGame_Prefix()
		{
			Player.main.GetComponent<AutosaveController>()?.ExecuteAutosave();

			return false;
		}
#endif

		private static void Patch_Player_Awake_Postfix(Player __instance)
		{
			__instance.gameObject.AddComponent<AutosaveController>();
		}

		private static void Patch_Subroot_PlayerEnteredOrExited_Postfix()
		{
#if DEBUG
			Entry.LogMessage("Player entered or exited sub. Delaying autosave.");
#endif

			Player.main.GetComponent<AutosaveController>()?.DelayAutosave();
		}

		//private static void Patch_SetCurrentLanguage_Postfix()
		//{
		//	Translation.ReloadLanguage();
		//}

		internal static void InitializeHarmony()
		{
			var harmony = new Harmony("delrathi.snAutosave");

#if DEBUG
			Harmony.DEBUG = true;


			// Detour menu saves for testing purposes
			MethodInfo ingameMenuSaveGame = AccessTools.Method(typeof(IngameMenu), nameof(IngameMenu.SaveGame));
			harmony.Patch(ingameMenuSaveGame, new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.TestPatch_SaveGame_Prefix)), null, null);
#endif

			MethodInfo playerAwake = AccessTools.Method(typeof(Player), nameof(Player.Awake));
			MethodInfo subRootPlayerEntered = AccessTools.Method(typeof(SubRoot), nameof(SubRoot.OnPlayerEntered));
			MethodInfo subRootPlayerExited = AccessTools.Method(typeof(SubRoot), nameof(SubRoot.OnPlayerExited));
			MethodInfo setLanguage = AccessTools.Method(typeof(Language), nameof(Language.SetCurrentLanguage));

			// Autosave injection
			harmony.Patch(playerAwake,
				prefix: null,
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_Player_Awake_Postfix)),
				transpiler: null);

			// Delay autosave if player has entered or exited a base or vehicle
			HarmonyMethod delayAutosave = new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_Subroot_PlayerEnteredOrExited_Postfix));

			harmony.Patch(subRootPlayerEntered,
				prefix: null,
				postfix: delayAutosave,
				transpiler: null);

			harmony.Patch(subRootPlayerExited,
				prefix: null,
				postfix: delayAutosave,
				transpiler: null);

			// Reset language cache upon language change
			//harmony.Patch(setLanguage,
			//	prefix: null,
			//	postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_SetCurrentLanguage_Postfix)),
			//	transpiler: null);
		}
	}
}
