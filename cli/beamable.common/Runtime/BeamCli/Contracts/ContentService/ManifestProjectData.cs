using System;

namespace Beamable.Common.BeamCli.Contracts
{
	[CliContractType, Serializable]
	public struct ManifestProjectData
	{
		public string PID;
		public string RealmName;
	}
}
