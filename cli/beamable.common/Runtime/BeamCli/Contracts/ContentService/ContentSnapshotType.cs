using System;

namespace Beamable.Common.BeamCli.Contracts
{
	[CliContractType, Serializable]
	public enum ContentSnapshotType
	{
		Local = 0,
		Shared = 1
	}
}
