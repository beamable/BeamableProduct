// This file generated by a copy-operation from another project. 
// Edits to this file will be overwritten by the build process. 

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
