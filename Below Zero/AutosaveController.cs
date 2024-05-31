namespace SubnauticaAutosave
{
    public class AutosaveController : AutosaveControllerBase
    {
        public override void SetSlot(string newSlot)
        {
            // No idea what StoryVersion.Reboot means but I guess it has to do with the Early Access for BZ?
            SaveLoadManager.main.SetCurrentSlot(newSlot, SaveLoadManager.StoryVersion.Reboot);
        }
    }
}
