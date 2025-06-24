using System;
using System.Collections.Generic;

namespace Beamable.Player.CloudSaving
{
	[Serializable]
	public class CloudSavingManifest
	{
		public List<CloudSaveEntry> manifest = new();
		public List<SavingFileEntry> savingFiles = new();
	}
}
