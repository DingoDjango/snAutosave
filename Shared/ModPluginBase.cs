using BepInEx;

namespace SubnauticaAutosave
{
    public abstract class ModPluginBase : BaseUnityPlugin
    {
        public const int MaxMinutesBetweenSaves = 600; // 10 hours should be enough
        public const int MaxSaveFiles = 99;

        public static ModPlugin Instance;

        public static ModOptions options;

        public abstract void LogMessage(string message);

        public abstract void LogWarning(string warning);

        public abstract void LogError(string error);
    }
}
