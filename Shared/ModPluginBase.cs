using BepInEx;

namespace SubnauticaAutosave
{
    public abstract class ModPluginBase : BaseUnityPlugin
    {
        public const int MaxMinutesBetweenSaves = 600; // 10 hours should be enough
        public const int MaxSaveFiles = 99;

        public static ModOptions options;
    }
}
