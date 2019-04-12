using System;

namespace SubnauticaAutosave
{
	[Serializable]
	public class Config
	{
		public int SecondsBetweenAutosaves = 900;

		public int MaxSaveFiles = 3;

		public float MinimumPlayerHealthPercent = 25;

		public bool HardcoreMode = false;
	}
}
