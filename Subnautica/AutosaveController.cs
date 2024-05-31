namespace SubnauticaAutosave
{
    public class AutosaveController : AutosaveControllerBase
    {
        public override void SetSlot(string newSlot)
        {
            SaveLoadManager.main.SetCurrentSlot(newSlot);
        }
    }
}
