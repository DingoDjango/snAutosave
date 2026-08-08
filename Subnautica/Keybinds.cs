using Nautilus.Handlers;

namespace SubnauticaAutosave
{
    // The legacy KeyCode + [Keybind] path is BZ-only in this Nautilus version
    // and Subnautica's own keybinding system has regressions, so this uses
    // the Input System path via EnumHandler/GameInputHandler instead.
    internal static class Keybinds
    {
        private const string DefaultBinding = "<Keyboard>/f9";

        internal static GameInput.Button Quicksave { get; private set; }

        internal static void Initialize()
        {
            Quicksave = EnumHandler.AddEntry<GameInput.Button>("SubnauticaAutosaveQuicksave")
                .CreateInput("QuicksaveKey".Translate(), "Tooltip_QuicksaveKey".Translate())
                .WithKeyboardBinding(DefaultBinding)
                // Empty path = bindable in Mod Input tab, unbound default. Vanilla Controller group covers gamepad + mouse.
                .WithBinding(GameInput.Device.Controller, GameInput.BindingSet.Primary, string.Empty)
                .AvoidConflicts(GameInput.Device.Keyboard)
                .WithCategory(ModPlugin.modName);
        }
    }
}
