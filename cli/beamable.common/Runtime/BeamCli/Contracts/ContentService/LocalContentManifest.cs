using System;

namespace Beamable.Common.BeamCli.Contracts
{
	[CliContractType, Serializable]
	public class LocalContentManifest
	{
		public string OwnerCid;
		public string OwnerPid;
		public string ManifestId;

		public LocalContentManifestEntry[] Entries;
	}
}
