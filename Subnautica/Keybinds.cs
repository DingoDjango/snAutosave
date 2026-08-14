using Nautilus.Handlers;

namespace SubnauticaAutosave
{
    internal class Keybinds
    {
    	private const string DefaultBinding = "<Keyboard>/f9";
    	private const string DebugDefaultBinding = "<Keyboard>/leftBracket";

        internal GameInput.Button Quicksave = EnumHandler.AddEntry<GameInput.Button>("SubnauticaAutosaveQuicksave")
                    .CreateInput("QuicksaveKey".Translate(), "Tooltip_QuicksaveKey".Translate())
                    .WithKeyboardBinding(primaryBindingPath: DefaultBinding, secondaryBindingPath: null)
                    .WithControllerBinding(null, null)
                    .AvoidConflicts(GameInput.Device.Keyboard)
                    .WithCategory(ModPlugin.modName);

#if DEBUG
        internal GameInput.Button DebugAutosaveTrigger = EnumHandler.AddEntry<GameInput.Button>("SubnauticaAutosaveDebugAutosave")
                .CreateInput("Debug: Trigger Autosave", "Forces TryExecuteAutosave() for testing.")
                .WithKeyboardBinding(DebugDefaultBinding)
                .WithBinding(GameInput.Device.Controller, GameInput.BindingSet.Primary, string.Empty)
                .AvoidConflicts(GameInput.Device.Keyboard)
                .WithCategory(ModPlugin.modName);
#endif
    }
}
