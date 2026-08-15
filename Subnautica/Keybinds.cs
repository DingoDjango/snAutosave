using Nautilus.Handlers;

namespace SubnauticaAutosave
{
    internal class Keybinds
    {
    	private static string QuicksaveDefault => GameInputHandler.Paths.Keyboard.F9;
    	private static string DebugAutosaveDefault => GameInputHandler.Paths.Keyboard.LeftBracket;

        internal GameInput.Button Quicksave = EnumHandler.AddEntry<GameInput.Button>("SubnauticaAutosaveQuicksave")
                    .CreateInput("QuicksaveKey".Translate(), "Tooltip_QuicksaveKey".Translate())
                    .WithKeyboardBinding(QuicksaveDefault)
                    .WithControllerBinding("None")
                    .AvoidConflicts(GameInput.Device.Keyboard)
                    .WithCategory(ModPlugin.modName);

#if DEBUG
        internal GameInput.Button DebugAutosaveTrigger = EnumHandler.AddEntry<GameInput.Button>("SubnauticaAutosaveDebugAutosave")
                .CreateInput("Debug: Trigger Autosave", "Forces TryExecuteAutosave() for testing.")
                .WithKeyboardBinding(DebugAutosaveDefault)
                .WithControllerBinding("None")
                .AvoidConflicts(GameInput.Device.Keyboard)
                .WithCategory(ModPlugin.modName);
#endif
    }
}
