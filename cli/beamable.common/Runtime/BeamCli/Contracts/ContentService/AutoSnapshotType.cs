using System;

namespace Beamable.Common.BeamCli.Contracts
{
	[CliContractType, Serializable]
	public enum AutoSnapshotType
	{
		None,
		LocalOnly,
		SharedOnly,
		Both
	}
}
