using Nautilus.Handlers;

namespace SubnauticaAutosave
{
    internal class Keybinds
    {
    	private const string QuicksaveDefault = "<Keyboard>/f9";
    	private const string DebugAutosaveDefault = "<Keyboard>/leftBracket";

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
