using System;

namespace Beamable.Player.CloudSaving
{
	[Serializable]
	public class SavingFileEntry
	{
		public string FileName;
		public string ExpectedChecksum;
	}
}
