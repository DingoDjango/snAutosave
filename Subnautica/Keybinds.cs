using Nautilus.Handlers;
using static VFXParticlesPool;

namespace SubnauticaAutosave
{
    // The legacy KeyCode + [Keybind] path is BZ-only in this Nautilus version
    // and Subnautica's own keybinding system has regressions, so this uses
    // the Input System path via EnumHandler/GameInputHandler instead.
    internal static class Keybinds
    {
        private const string DefaultBinding = "<Keyboard>/f9";
        private const string DebugDefaultBinding = "<Keyboard>/leftBracket";

        internal static GameInput.Button Quicksave { get; private set; }

#if DEBUG
        internal static GameInput.Button DebugAutosaveTrigger { get; private set; }
#endif

        internal static void Initialize()
        {
            Quicksave = EnumHandler.AddEntry<GameInput.Button>("SubnauticaAutosaveQuicksave")
                .CreateInput("QuicksaveKey".Translate(), "Tooltip_QuicksaveKey".Translate())
                .WithKeyboardBinding(DefaultBinding)
                // Empty path = bindable in Mod Input tab, unbound default. Vanilla Controller group covers gamepad + mouse.
                .WithBinding(GameInput.Device.Controller, GameInput.BindingSet.Primary, string.Empty)
                .AvoidConflicts(GameInput.Device.Keyboard)
                .WithCategory(ModPlugin.modName);

#if DEBUG
            GameInput.SetBinding(GameInput.Device.Keyboard, GameInput.Button.CycleNext, GameInput.BindingSet.Secondary, string.Empty);
            GameInput.SetBinding(GameInput.Device.Keyboard, GameInput.Button.CyclePrev, GameInput.BindingSet.Secondary, string.Empty);

            DebugAutosaveTrigger = EnumHandler.AddEntry<GameInput.Button>("SubnauticaAutosaveDebugAutosave")
                .CreateInput("Debug: Trigger Autosave", "Forces TryExecuteAutosave() for testing.")
                .WithKeyboardBinding(DebugDefaultBinding)
                .WithBinding(GameInput.Device.Controller, GameInput.BindingSet.Primary, string.Empty)
                .AvoidConflicts(GameInput.Device.Keyboard)
                .WithCategory(ModPlugin.modName);
#endif
        }
    }
}
